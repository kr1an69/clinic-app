using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    /// <summary>
    /// QUẢN LÝ & CHI TIẾT ĐƠN THUỐC ĐÃ KÊ (Master-Detail)
    /// Chuẩn hóa theo BaseForm và UiHelper (DPI-aware).
    /// </summary>
    public class FormPrescriptionList : BaseForm
    {
        private readonly DoctorController _controller = new();

        // Các control khu vực tìm kiếm phía trên
        private TextBox _txtSearch = null!;
        private IconButton _btnSearch = null!;
        private IconButton _btnRefresh = null!;
        private CheckBox _chkOnlyWithMedicines = null!;
        private Label _lblPrescriptionCount = null!;

        // Bảng Master: Danh sách đơn thuốc
        private DataGridView _dgvPrescriptions = null!;

        // Bảng Detail: Chi tiết thuốc trong đơn
        private GroupBox _grpDetails = null!;
        private DataGridView _dgvPrescriptionDetails = null!;
        private Label _lblDetailTotalAmount = null!;

        // Tái sử dụng Font in nghiêng cho dòng thông báo không kê thuốc (tránh rò rỉ GDI handle)
        private readonly Font _italicDetailFont = new(UiHelper.FontRegular, FontStyle.Italic);

        public FormPrescriptionList(string initialKeyword = "")
        {
            InitializeComponent();
            RegisterEvents();
            if (!string.IsNullOrEmpty(initialKeyword))
            {
                _txtSearch.Text = initialKeyword;
            }
            LoadPrescriptions(_txtSearch.Text.Trim());
        }

        private void InitializeComponent()
        {
            this.Text = "Quản Lý & Chi Tiết Đơn Thuốc Đã Kê";
            this.ConfigureAsFixedDialog(baseWidth: 1260, baseHeight: 680);
            this.Padding = UiHelper.Pad(12);

            // Bố cục chính chia 2 hàng: Thanh tìm kiếm (AutoSize) và Khung Master-Detail (co giãn 100%)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpSearch = BuildSearchGroup();
            var bodyMasterDetail = BuildMasterDetailLayout();

            mainLayout.Controls.Add(grpSearch, 0, 0);
            mainLayout.Controls.Add(bodyMasterDetail, 0, 1);

            this.Controls.Add(mainLayout);
        }

        // 1. Thanh công cụ tìm kiếm đơn thuốc
        private GroupBox BuildSearchGroup()
        {
            var grpSearch = UiHelper.CreateGroupBox(
                "Tra Cứu Đơn Thuốc Đã Kê",
                titleColor: UiHelper.DarkText,
                padding: UiHelper.Pad(12, 8, 12, 10),
                margin: UiHelper.Pad(0, 0, 0, 10)
            );
            grpSearch.Dock = DockStyle.Top;
            grpSearch.AutoSize = true;

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var searchFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var lblSearch = UiHelper.CreateLabel(
                text: "Tìm theo Tên BN / Chẩn đoán:",
                font: UiHelper.FontRegular,
                margin: UiHelper.Pad(0, 6, 8, 0));
            lblSearch.Anchor = AnchorStyles.Left;

            _txtSearch = new TextBox
            {
                Width = UiHelper.S(300),
                Font = UiHelper.FontRegular,
                PlaceholderText = "Nhập tên bệnh nhân hoặc chẩn đoán...",
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 8, 0)
            };

            _btnSearch = UiHelper.CreateButton("Tìm kiếm đơn thuốc", IconChar.MagnifyingGlass, UiHelper.PrimaryBlue, 175, 34);
            _btnSearch.Margin = UiHelper.Pad(0, 0, 6, 0);

            _btnRefresh = UiHelper.CreateButton("Hiển thị tất cả", IconChar.Rotate, Color.FromArgb(52, 73, 94), 145, 34);
            _btnRefresh.Margin = UiHelper.Pad(0, 0, 6, 0);

            _chkOnlyWithMedicines = new CheckBox
            {
                Text = "Chỉ hiện đơn có thuốc",
                AutoSize = true,
                Font = UiHelper.FontRegular,
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(8, 6, 0, 0),
                Cursor = Cursors.Hand
            };

            searchFlow.Controls.Add(lblSearch);
            searchFlow.Controls.Add(_txtSearch);
            searchFlow.Controls.Add(_btnSearch);
            searchFlow.Controls.Add(_btnRefresh);
            searchFlow.Controls.Add(_chkOnlyWithMedicines);

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };

            _lblPrescriptionCount = UiHelper.CreateLabel(
                text: "Tổng số: 0 đơn thuốc",
                font: UiHelper.FontBold,
                foreColor: UiHelper.PrimaryBlue,
                margin: UiHelper.Pad(0, 8, 4, 0));
            rightFlow.Controls.Add(_lblPrescriptionCount);

            toolbarLayout.Controls.Add(searchFlow, 0, 0);
            toolbarLayout.Controls.Add(rightFlow, 1, 0);
            grpSearch.Controls.Add(toolbarLayout);
            return grpSearch;
        }

        // 2. Bố cục Master-Detail 2 cột: Trái (Danh sách đơn thuốc) - Phải (Chi tiết các thuốc trong đơn)
        private TableLayoutPanel BuildMasterDetailLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpMaster = BuildPrescriptionsMasterGroup();
            _grpDetails = BuildPrescriptionDetailsGroup();

            layout.Controls.Add(grpMaster, 0, 0);
            layout.Controls.Add(_grpDetails, 1, 0);

            return layout;
        }

        // 2.1 Cột Trái (Master): Bảng danh sách đơn thuốc
        private GroupBox BuildPrescriptionsMasterGroup()
        {
            var grpMaster = UiHelper.CreateGroupBox(
                "DANH SÁCH ĐƠN THUỐC (PRESCRIPTIONS)",
                padding: UiHelper.Pad(12, 8, 12, 12),
                margin: UiHelper.Pad(0, 0, 8, 0)
            );

            _dgvPrescriptions = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 44);
            _dgvPrescriptions.ShowCellToolTips = true;
            _dgvPrescriptions.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _dgvPrescriptions.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvPrescriptions.ColumnHeadersDefaultCellStyle.Padding = UiHelper.Pad(4, 2, 4, 2);
            _dgvPrescriptions.DefaultCellStyle.Padding = UiHelper.Pad(4, 2, 4, 2);
            _dgvPrescriptions.DefaultCellStyle.ForeColor = UiHelper.DarkText;

            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "PrescriptionCode",
                HeaderText = "Mã Đơn",
                FillWeight = 13,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colCode.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var colPatient = new DataGridViewTextBoxColumn
            {
                Name = "PatientName",
                HeaderText = "Bệnh Nhân",
                FillWeight = 25,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colDiagnosis = new DataGridViewTextBoxColumn
            {
                Name = "Diagnosis",
                HeaderText = "Chẩn Đoán",
                FillWeight = 23,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colTotal = new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "Tổng Tiền",
                FillWeight = 17,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "DispensedStatus",
                HeaderText = "Trạng Thái",
                FillWeight = 22,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dgvPrescriptions.Columns.AddRange(colCode, colPatient, colDiagnosis, colTotal, colStatus);

            grpMaster.Controls.Add(_dgvPrescriptions);
            return grpMaster;
        }

        // 2.2 Cột Phải (Detail): Bảng chi tiết thuốc trong đơn đang chọn
        private GroupBox BuildPrescriptionDetailsGroup()
        {
            var grpDetail = UiHelper.CreateGroupBox(
                "CHI TIẾT THUỐC TRONG ĐƠN (PRESCRIPTION DETAILS)",
                padding: UiHelper.Pad(12, 8, 12, 12),
                margin: UiHelper.Pad(8, 0, 0, 0)
            );

            var detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(42)));

            _dgvPrescriptionDetails = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 44);
            _dgvPrescriptionDetails.ShowCellToolTips = true;
            _dgvPrescriptionDetails.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _dgvPrescriptionDetails.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvPrescriptionDetails.ColumnHeadersDefaultCellStyle.Padding = UiHelper.Pad(4, 2, 4, 2);
            _dgvPrescriptionDetails.DefaultCellStyle.Padding = UiHelper.Pad(4, 2, 4, 2);
            _dgvPrescriptionDetails.DefaultCellStyle.ForeColor = UiHelper.DarkText;

            var colIndex = new DataGridViewTextBoxColumn
            {
                Name = "Index",
                HeaderText = "STT",
                FillWeight = 7,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colIndex.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var colMedName = new DataGridViewTextBoxColumn
            {
                Name = "MedicineName",
                HeaderText = "Tên Thuốc & Hàm Lượng",
                FillWeight = 32,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                Name = "Quantity",
                HeaderText = "Số Lượng",
                FillWeight = 12,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var colUnitPrice = new DataGridViewTextBoxColumn
            {
                Name = "UnitPrice",
                HeaderText = "Đơn Giá",
                FillWeight = 14,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colUnitPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var colSubTotal = new DataGridViewTextBoxColumn
            {
                Name = "SubTotal",
                HeaderText = "Thành Tiền",
                FillWeight = 15,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colSubTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var colDosage = new DataGridViewTextBoxColumn
            {
                Name = "Dosage",
                HeaderText = "Liều Dùng Chỉ Định",
                FillWeight = 20,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            _dgvPrescriptionDetails.Columns.AddRange(colIndex, colMedName, colQty, colUnitPrice, colSubTotal, colDosage);

            // Thanh tóm tắt tổng tiền thuốc bên dưới
            var summaryBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(235, 245, 251),
                Padding = UiHelper.Pad(12, 6, 12, 6),
                Margin = UiHelper.Pad(0, 6, 0, 0)
            };

            _lblDetailTotalAmount = UiHelper.CreateLabel(
                text: "TỔNG TIỀN THUỐC TRONG ĐƠN: 0 đ",
                font: new Font("Segoe UI", 10F, FontStyle.Bold),
                foreColor: UiHelper.DangerRed);
            _lblDetailTotalAmount.Dock = DockStyle.Right;
            _lblDetailTotalAmount.TextAlign = ContentAlignment.MiddleRight;

            summaryBar.Controls.Add(_lblDetailTotalAmount);

            detailLayout.Controls.Add(_dgvPrescriptionDetails, 0, 0);
            detailLayout.Controls.Add(summaryBar, 0, 1);

            grpDetail.Controls.Add(detailLayout);
            return grpDetail;
        }

        // --- CÁC PHƯƠNG THỨC NGHIỆP VỤ & SỰ KIỆN ---

        private void RegisterEvents()
        {
            _btnSearch.Click += (_, _) => LoadPrescriptions(_txtSearch.Text.Trim());
            _btnRefresh.Click += (_, _) =>
            {
                _txtSearch.Clear();
                _chkOnlyWithMedicines.Checked = false;
                LoadPrescriptions();
            };
            _chkOnlyWithMedicines.CheckedChanged += (_, _) => LoadPrescriptions(_txtSearch.Text.Trim());

            _txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    LoadPrescriptions(_txtSearch.Text.Trim());
                }
            };

            _dgvPrescriptions.SelectionChanged += (_, _) => OnPrescriptionSelectionChanged();

            // Định dạng màu sắc cột Trạng Thái (Chờ thu phí: Cam, Đã xuất/Đã thu: Xanh lá) & Tooltip hiển thị đầy đủ
            _dgvPrescriptions.CellFormatting += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var col = _dgvPrescriptions.Columns[e.ColumnIndex];
                    if (col.Name == "Diagnosis" && e.Value != null)
                    {
                        _dgvPrescriptions.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = e.Value.ToString();
                    }
                    else if (col.Name == "DispensedStatus")
                    {
                        string status = Convert.ToString(e.Value) ?? string.Empty;
                        if (status.Contains("Chờ thu phí"))
                        {
                            e.CellStyle.ForeColor = UiHelper.WarningOrange;
                            e.CellStyle.Font = UiHelper.FontBold;
                        }
                        else if (status.Contains("Đã xuất") || status.Contains("Đã thu phí"))
                        {
                            e.CellStyle.ForeColor = UiHelper.SuccessGreen;
                            e.CellStyle.Font = UiHelper.FontBold;
                        }
                    }
                }
            };

            _dgvPrescriptionDetails.CellFormatting += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var col = _dgvPrescriptionDetails.Columns[e.ColumnIndex];
                    if ((col.Name == "MedicineName" || col.Name == "Dosage") && e.Value != null)
                    {
                        _dgvPrescriptionDetails.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = e.Value.ToString();
                    }
                }
            };
        }

        // Tải danh sách đơn thuốc từ CSDL theo từ khóa tìm kiếm
        public void LoadPrescriptions(string keyword = "")
        {
            try
            {
                var prescriptions = _controller.GetAllPrescriptions(keyword);
                if (_chkOnlyWithMedicines != null && _chkOnlyWithMedicines.Checked)
                {
                    prescriptions = prescriptions.Where(p => p.TotalAmount > 0).ToList();
                }
                _dgvPrescriptions.Rows.Clear();

                foreach (var item in prescriptions)
                {
                    string code = !string.IsNullOrEmpty(item.PrescriptionCode) ? item.PrescriptionCode : $"DT-{item.Id:D3}";
                    string patient = item.PatientName;
                    string diagnosis = item.Diagnosis ?? string.Empty;
                    double total = item.TotalAmount;
                    bool isNoMedicine = total <= 0;
                    string totalDisplay = isNoMedicine ? "0 đ (Không thuốc)" : $"{total:N0} đ";
                    string status = isNoMedicine
                        ? (item.IsDispensed == 1 ? "Đã thu phí" : "Chờ thu phí")
                        : (!string.IsNullOrEmpty(item.DispensedStatus) ? item.DispensedStatus : (item.IsDispensed == 1 ? "Đã xuất thuốc" : "Chờ thu phí"));

                    int rowIdx = _dgvPrescriptions.Rows.Add(
                        code,
                        patient,
                        diagnosis,
                        totalDisplay,
                        status
                    );
                    _dgvPrescriptions.Rows[rowIdx].Tag = item;
                    if (isNoMedicine)
                    {
                        _dgvPrescriptions.Rows[rowIdx].Cells["TotalAmount"].ToolTipText = "Ca khám chỉ thu tiền công khám lâm sàng, bác sĩ không kê đơn thuốc.";
                    }
                }

                _lblPrescriptionCount.Text = $"Tổng số: {prescriptions.Count} đơn thuốc";

                if (_dgvPrescriptions.Rows.Count > 0)
                {
                    _dgvPrescriptions.Rows[0].Selected = true;
                    OnPrescriptionSelectionChanged();
                }
                else
                {
                    ResetDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn thuốc: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hiển thị chi tiết thuốc trong đơn khi người dùng click chọn một đơn thuốc bên bảng Master
        private void OnPrescriptionSelectionChanged()
        {
            if (_dgvPrescriptions.SelectedRows.Count == 0)
            {
                ResetDetails();
                return;
            }

            var row = _dgvPrescriptions.SelectedRows[0];
            if (row.Tag is not PrescriptionListItem item)
            {
                ResetDetails();
                return;
            }

            int prescriptionId = item.Id;
            string code = !string.IsNullOrEmpty(item.PrescriptionCode) ? item.PrescriptionCode : $"DT-{prescriptionId:D3}";
            string patientName = item.PatientName;
            double totalAmount = item.TotalAmount;
            bool isNoMedicine = totalAmount <= 0;

            _grpDetails.Text = isNoMedicine
                ? $"CHI TIẾT THUỐC TRONG ĐƠN #{code} - BN: {patientName} (KHÔNG KÊ THUỐC)"
                : $"CHI TIẾT THUỐC TRONG ĐƠN #{code} - BN: {patientName}";

            _lblDetailTotalAmount.Text = isNoMedicine
                ? "TỔNG TIỀN THUỐC TRONG ĐƠN: 0 đ (Ca chỉ khám lâm sàng, không kê đơn thuốc)"
                : $"TỔNG TIỀN THUỐC TRONG ĐƠN: {totalAmount:N0} đ";

            try
            {
                var details = _controller.GetPrescriptionDetailsById(prescriptionId);
                _dgvPrescriptionDetails.Rows.Clear();

                if (details.Count == 0)
                {
                    int emptyRow = _dgvPrescriptionDetails.Rows.Add(
                        "---",
                        "Ca khám không kê thuốc (chỉ thu tiền công khám)",
                        "---",
                        "0 đ",
                        "0 đ",
                        "Bác sĩ tư vấn chế độ sinh hoạt & nghỉ ngơi"
                    );
                    _dgvPrescriptionDetails.Rows[emptyRow].DefaultCellStyle.ForeColor = UiHelper.SecondaryGray;
                    _dgvPrescriptionDetails.Rows[emptyRow].DefaultCellStyle.Font = _italicDetailFont;
                }
                else
                {
                    for (int i = 0; i < details.Count; i++)
                    {
                        var d = details[i];
                        string unit = !string.IsNullOrEmpty(d.Unit) ? $" {d.Unit}" : "";
                        _dgvPrescriptionDetails.Rows.Add(
                            $"{i + 1:D2}",
                            d.MedicineName ?? string.Empty,
                            $"{d.Quantity}{unit}",
                            $"{d.UnitPrice:N0} đ",
                            $"{d.SubTotal:N0} đ",
                            d.Dosage ?? string.Empty
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp chi tiết đơn thuốc #{code}: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetDetails()
        {
            _grpDetails.Text = "CHI TIẾT THUỐC TRONG ĐƠN (PRESCRIPTION DETAILS)";
            _dgvPrescriptionDetails.Rows.Clear();
            _lblDetailTotalAmount.Text = "TỔNG TIỀN THUỐC TRONG ĐƠN: 0 đ";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _italicDetailFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
