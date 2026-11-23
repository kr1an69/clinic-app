using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormPharmacy : BaseForm
    {
        private readonly PharmacyController _controller = new();
        private List<PendingPrescriptionItem> _pendingItems = new();
        private List<PrescriptionDetail> _currentDetails = new();
        private PendingPrescriptionItem? _selectedPrescription;

        private TextBox _txtSearch = null!;
        private IconButton _btnRefresh = null!;
        private IconButton _btnOpenInventory = null!;
        private DataGridView _dgvPending = null!;
        private DataGridView _dgvDetails = null!;

        private Label _lblPatientInfo = null!;
        private Label _lblDiagnosisInfo = null!;
        private Label _lblExamFee = null!;
        private Label _lblMedicineFee = null!;
        private Label _lblTotalAmount = null!;
        private IconButton _btnConfirmCheckout = null!;

        public FormPharmacy()
        {
            InitializeComponent();
            LoadPendingPrescriptions();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiHelper.BgMain,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headerBanner = UiHelper.CreateHeaderBanner(
                "QUẦY THU NGÂN & XUẤT THUỐC",
                "Thu phí khám bệnh, xuất thuốc trừ tồn kho tự động và quản lý kho dược");

            var mainColumns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = UiHelper.Pad(14),
                Margin = Padding.Empty
            };
            mainColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            mainColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            mainColumns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var leftPanel = BuildLeftPanel();
            var rightPanel = BuildRightPanel();

            mainColumns.Controls.Add(leftPanel, 0, 0);
            mainColumns.Controls.Add(rightPanel, 1, 0);

            rootLayout.Controls.Add(headerBanner, 0, 0);
            rootLayout.Controls.Add(mainColumns, 0, 1);
            this.Controls.Add(rootLayout);
        }

        // Dựng cột trái: Tìm kiếm và danh sách đơn chờ thu phí
        private GroupBox BuildLeftPanel()
        {
            var grpLeft = UiHelper.CreateGroupBox(
                "1. DANH SÁCH ĐƠN THUỐC CHỜ THANH TOÁN",
                UiHelper.PrimaryBlue,
                UiHelper.Pad(12),
                UiHelper.Pad(0, 0, 8, 0));

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var searchBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };
            searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                PlaceholderText = "Tìm theo tên bệnh nhân hoặc SĐT...",
                Margin = UiHelper.Pad(0, 4, 8, 4)
            };
            _txtSearch.TextChanged += (_, _) => LoadPendingPrescriptions();

            _btnRefresh = UiHelper.CreateButton("Làm mới", IconChar.RotateRight, UiHelper.PrimaryBlue, 110, 34);
            _btnRefresh.Click += (_, _) => LoadPendingPrescriptions();

            searchBar.Controls.Add(_txtSearch, 0, 0);
            searchBar.Controls.Add(_btnRefresh, 1, 0);

            _dgvPending = UiHelper.CreateStandardGrid();
            _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "STT", Width = UiHelper.S(55), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrescriptionId", HeaderText = "Mã đơn", Width = UiHelper.S(70), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Bệnh nhân" });
            _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "SĐT", Width = UiHelper.S(105), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "Tổng tiền", Width = UiHelper.S(110), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvPending.Columns["TotalAmount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvPending.SelectionChanged += DgvPending_SelectionChanged;

            var bottomBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = UiHelper.Pad(0, 10, 0, 0)
            };

            _btnOpenInventory = UiHelper.CreateButton("Quản lý Kho Thuốc", IconChar.BoxesStacked, UiHelper.SecondaryGray, 210, 36);
            _btnOpenInventory.Click += (_, _) =>
            {
                using var frmInventory = new FormMedicineInventory();
                frmInventory.ShowDialog(this);
                RefreshSelectedDetails();
            };
            bottomBar.Controls.Add(_btnOpenInventory);

            leftLayout.Controls.Add(searchBar, 0, 0);
            leftLayout.Controls.Add(_dgvPending, 0, 1);
            leftLayout.Controls.Add(bottomBar, 0, 2);

            grpLeft.Controls.Add(leftLayout);
            return grpLeft;
        }

        // Dựng cột phải: Chi tiết đơn thuốc, tính tiền và xác nhận xuất kho
        private GroupBox BuildRightPanel()
        {
            var grpRight = UiHelper.CreateGroupBox(
                "2. CHI TIẾT ĐƠN THUỐC & THANH TOÁN",
                UiHelper.PrimaryBlue,
                UiHelper.Pad(12),
                UiHelper.Pad(8, 0, 0, 0));

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var infoBox = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiHelper.MutedInputBg,
                Padding = UiHelper.Pad(10, 8, 10, 8),
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };
            infoBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            infoBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblPatientInfo = UiHelper.CreateLabel("Bệnh nhân: Chưa chọn đơn thuốc", UiHelper.FontBold, UiHelper.DarkText, UiHelper.Pad(0, 0, 0, 3));
            _lblDiagnosisInfo = UiHelper.CreateLabel("Chẩn đoán: ---", UiHelper.FontRegular, UiHelper.SecondaryGray, Padding.Empty);
            infoBox.Controls.Add(_lblPatientInfo, 0, 0);
            infoBox.Controls.Add(_lblDiagnosisInfo, 0, 1);

            _dgvDetails = UiHelper.CreateStandardGrid();
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedicineName", HeaderText = "Tên thuốc" });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", Width = UiHelper.S(60), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "SL kê", Width = UiHelper.S(65), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQuantity", HeaderText = "Tồn kho", Width = UiHelper.S(75), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingStock", HeaderText = "Sau xuất", Width = UiHelper.S(80), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn giá", Width = UiHelper.S(95), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "Thành tiền", Width = UiHelper.S(105), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dosage", HeaderText = "Liều dùng" });

            _dgvDetails.Columns["Quantity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvDetails.Columns["StockQuantity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvDetails.Columns["RemainingStock"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvDetails.Columns["UnitPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvDetails.Columns["SubTotal"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvDetails.CellFormatting += DgvDetails_CellFormatting;

            var summaryGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = UiHelper.MutedInputBg,
                Padding = UiHelper.Pad(12, 8, 12, 8),
                Margin = UiHelper.Pad(0, 10, 0, 10)
            };
            summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblExamFee = UiHelper.CreateLabel("0 đ", UiHelper.FontBold, UiHelper.DarkText, UiHelper.Pad(0, 0, 0, 4));
            _lblExamFee.Anchor = AnchorStyles.Right;
            _lblMedicineFee = UiHelper.CreateLabel("0 đ", UiHelper.FontBold, UiHelper.DarkText, UiHelper.Pad(0, 0, 0, 4));
            _lblMedicineFee.Anchor = AnchorStyles.Right;
            _lblTotalAmount = UiHelper.CreateLabel("0 đ", new Font("Segoe UI", 12F, FontStyle.Bold), UiHelper.DangerRed, Padding.Empty);
            _lblTotalAmount.Anchor = AnchorStyles.Right;

            summaryGrid.Controls.Add(UiHelper.CreateLabel("Tiền khám bệnh:", UiHelper.FontRegular, UiHelper.DarkText), 0, 0);
            summaryGrid.Controls.Add(_lblExamFee, 1, 0);
            summaryGrid.Controls.Add(UiHelper.CreateLabel("Tiền thuốc theo đơn:", UiHelper.FontRegular, UiHelper.DarkText), 0, 1);
            summaryGrid.Controls.Add(_lblMedicineFee, 1, 1);
            summaryGrid.Controls.Add(UiHelper.CreateLabel("TỔNG THANH TOÁN:", UiHelper.FontSection, UiHelper.DarkText, Padding.Empty), 0, 2);
            summaryGrid.Controls.Add(_lblTotalAmount, 1, 2);

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = Padding.Empty
            };

            _btnConfirmCheckout = UiHelper.CreateButton("Xác nhận Thu phí & Xuất thuốc", IconChar.CashRegister, UiHelper.SuccessGreen, 250, 40);
            _btnConfirmCheckout.Click += BtnConfirmCheckout_Click;
            actionBar.Controls.Add(_btnConfirmCheckout);

            rightLayout.Controls.Add(infoBox, 0, 0);
            rightLayout.Controls.Add(_dgvDetails, 0, 1);
            rightLayout.Controls.Add(summaryGrid, 0, 2);
            rightLayout.Controls.Add(actionBar, 0, 3);

            grpRight.Controls.Add(rightLayout);
            return grpRight;
        }

        // Tải danh sách đơn thuốc chờ thanh toán lên lưới trái
        private void LoadPendingPrescriptions()
        {
            _pendingItems = _controller.GetPendingPrescriptions(_txtSearch.Text);
            _dgvPending.Rows.Clear();

            foreach (var item in _pendingItems)
            {
                var queueDisplay = item.QueueNumber.HasValue ? item.QueueNumber.Value.ToString() : "-";
                var rowIndex = _dgvPending.Rows.Add(
                    queueDisplay,
                    $"#{item.PrescriptionId}",
                    item.PatientName,
                    item.Phone ?? "---",
                    $"{item.TotalAmount:N0} đ");
                _dgvPending.Rows[rowIndex].Tag = item;
            }

            if (_dgvPending.Rows.Count > 0)
            {
                _dgvPending.Rows[0].Selected = true;
                UpdateSelectedPrescription(_pendingItems[0]);
            }
            else
            {
                ClearCheckoutPanel();
            }
        }

        private void DgvPending_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvPending.SelectedRows.Count == 0)
            {
                return;
            }

            if (_dgvPending.SelectedRows[0].Tag is PendingPrescriptionItem item)
            {
                UpdateSelectedPrescription(item);
            }
        }

        private void UpdateSelectedPrescription(PendingPrescriptionItem item)
        {
            _selectedPrescription = item;
            _lblPatientInfo.Text = $"Bệnh nhân: {item.PatientName}  |  SĐT: {item.Phone ?? "Không có"}  |  Đơn #{item.PrescriptionId}";
            _lblDiagnosisInfo.Text = $"Chẩn đoán: {(string.IsNullOrWhiteSpace(item.Diagnosis) ? "Chưa ghi nhận" : item.Diagnosis)}";

            RefreshSelectedDetails();
        }

        private void RefreshSelectedDetails()
        {
            if (_selectedPrescription == null)
            {
                return;
            }

            _currentDetails = _controller.GetPrescriptionDetails(_selectedPrescription.PrescriptionId);
            _dgvDetails.Rows.Clear();

            foreach (var d in _currentDetails)
            {
                var rowIndex = _dgvDetails.Rows.Add(
                    d.MedicineName ?? "---",
                    d.Unit ?? "Viên",
                    d.Quantity,
                    d.StockQuantity,
                    d.RemainingStock,
                    $"{d.UnitPrice:N0} đ",
                    $"{d.SubTotal:N0} đ",
                    d.Dosage ?? string.Empty);
                _dgvDetails.Rows[rowIndex].Tag = d;
            }

            var medicineFee = _currentDetails.Sum(d => d.SubTotal);
            var totalAmount = _selectedPrescription.ExamFee + medicineFee;

            _lblExamFee.Text = $"{_selectedPrescription.ExamFee:N0} đ";
            _lblMedicineFee.Text = $"{medicineFee:N0} đ";
            _lblTotalAmount.Text = $"{totalAmount:N0} đ";
        }

        private void ClearCheckoutPanel()
        {
            _selectedPrescription = null;
            _currentDetails.Clear();
            _dgvDetails.Rows.Clear();
            _lblPatientInfo.Text = "Bệnh nhân: Không có đơn thuốc chờ thanh toán";
            _lblDiagnosisInfo.Text = "Chẩn đoán: ---";
            _lblExamFee.Text = "0 đ";
            _lblMedicineFee.Text = "0 đ";
            _lblTotalAmount.Text = "0 đ";
        }

        // Tô cảnh báo đỏ nhạt nếu thuốc không đủ tồn kho để xuất
        private void DgvDetails_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgvDetails.Rows.Count)
            {
                return;
            }

            if (_dgvDetails.Rows[e.RowIndex].Tag is PrescriptionDetail detail && detail.RemainingStock < 0)
            {
                _dgvDetails.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(253, 237, 236);
                _dgvDetails.Rows[e.RowIndex].DefaultCellStyle.ForeColor = UiHelper.DangerRed;
            }
        }

        private void BtnConfirmCheckout_Click(object? sender, EventArgs e)
        {
            if (_selectedPrescription == null)
            {
                MessageBox.Show("Vui lòng chọn một đơn thuốc chờ thanh toán.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var totalText = _lblTotalAmount.Text;
            var confirm = MessageBox.Show(
                $"Xác nhận thu {totalText} và xuất kho cho đơn #{_selectedPrescription.PrescriptionId} của bệnh nhân {_selectedPrescription.PatientName}?",
                "Xác nhận thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var invoiceId = _controller.ProcessPaymentAndDispense(
                    _selectedPrescription.PrescriptionId,
                    AppSession.CurrentUser?.Id);

                MessageBox.Show(
                    $"Thanh toán và xuất thuốc thành công!\nMã hóa đơn: #{invoiceId}",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadPendingPrescriptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hoàn tất thanh toán:\n{ex.Message}", "Lỗi xuất kho", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshSelectedDetails();
            }
        }
    }
}
