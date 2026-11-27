using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormMedicineInventory : BaseForm
    {
        private readonly PharmacyController _controller = new();
        private List<Medicine> _allMedicines = new();

        private TextBox _txtSearchMed = null!;
        private IconButton _btnSearch = null!;
        private IconButton _btnOpenEditor = null!;
        private IconButton _btnOpenAlertReport = null!;
        private DataGridView _dgvMedicines = null!;

        public FormMedicineInventory()
        {
            InitializeComponent();
            LoadMedicines();
        }

        private void InitializeComponent()
        {
            this.Text = "Quản Lý Danh Mục Kho Thuốc";
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);

            // Bố cục chính gồm Header Banner và vùng nội dung 2 hàng
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
                "QUẢN LÝ DANH MỤC KHO THUỐC",
                "Tra cứu tồn kho, theo dõi cảnh báo cận hạn dùng / sắp hết hàng và nhập bổ sung thuốc");

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = UiHelper.Pad(14),
                Margin = Padding.Empty
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 1. Khu vực thanh tìm kiếm và nút chức năng
            var grpToolbar = UiHelper.CreateGroupBox(
                "Tra cứu & Thao tác Kho Dược",
                UiHelper.DarkText,
                UiHelper.Pad(12, 8, 12, 10),
                UiHelper.Pad(0, 0, 0, 10));
            grpToolbar.Dock = DockStyle.Top;
            grpToolbar.AutoSize = true;
            grpToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Cụm tìm kiếm bên trái
            var searchFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var lblSearch = UiHelper.CreateLabel("Tìm Mã / Tên thuốc:", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(0, 7, 8, 0));

            _txtSearchMed = new TextBox
            {
                Width = UiHelper.S(240),
                Font = UiHelper.FontRegular,
                PlaceholderText = "Gõ mã hoặc tên thuốc...",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 8, 0)
            };
            _txtSearchMed.TextChanged += (_, _) => ApplyFilter();

            _btnSearch = UiHelper.CreateButton("Tìm", IconChar.MagnifyingGlass, UiHelper.PrimaryBlue, 90);
            _btnSearch.Click += (_, _) => LoadMedicines();

            searchFlow.Controls.Add(lblSearch);
            searchFlow.Controls.Add(_txtSearchMed);
            searchFlow.Controls.Add(_btnSearch);

            // Cụm nút mở màn hình #14 và #15 bên phải
            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };

            _btnOpenAlertReport = UiHelper.CreateButton("Xem Báo Cáo Cảnh Báo", IconChar.TriangleExclamation, UiHelper.WarningOrange, 230);
            _btnOpenAlertReport.Click += BtnOpenAlertReport_Click;

            _btnOpenEditor = UiHelper.CreateButton("Thêm Thuốc Mới / Nhập Kho", IconChar.Plus, UiHelper.SuccessGreen, 255);
            _btnOpenEditor.Click += BtnOpenEditor_Click;

            actionFlow.Controls.Add(_btnOpenAlertReport);
            actionFlow.Controls.Add(_btnOpenEditor);

            toolbarLayout.Controls.Add(searchFlow, 0, 0);
            toolbarLayout.Controls.Add(actionFlow, 1, 0);
            grpToolbar.Controls.Add(toolbarLayout);

            // 2. Khu vực bảng danh mục thuốc
            var grpGrid = UiHelper.CreateGroupBox(
                "Toàn Bộ Danh Mục Thuốc Trong Kho",
                UiHelper.DarkText,
                UiHelper.Pad(12, 8, 12, 12),
                Padding.Empty);

            var gridContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            gridContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gridContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Dòng chú thích màu sắc cảnh báo
            var legendFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = UiHelper.Pad(0, 0, 0, 6)
            };

            var lblLegendRed = new Label
            {
                Text = "Màu Đỏ: Cận hạn dùng (<= 60 ngày)",
                AutoSize = true,
                UseMnemonic = false,
                BackColor = Color.FromArgb(250, 219, 216),
                ForeColor = Color.FromArgb(146, 43, 33),
                Font = UiHelper.FontSmall,
                Padding = UiHelper.Pad(6, 3, 6, 3),
                Margin = UiHelper.Pad(6, 0, 0, 0)
            };

            var lblLegendOrange = new Label
            {
                Text = "Màu Cam: Tồn kho <= Mức tối thiểu",
                AutoSize = true,
                UseMnemonic = false,
                BackColor = Color.FromArgb(253, 235, 208),
                ForeColor = Color.FromArgb(147, 81, 22),
                Font = UiHelper.FontSmall,
                Padding = UiHelper.Pad(6, 3, 6, 3),
                Margin = Padding.Empty
            };

            legendFlow.Controls.Add(lblLegendRed);
            legendFlow.Controls.Add(lblLegendOrange);

            // Khởi tạo DataGridView hiển thị danh mục thuốc
            _dgvMedicines = UiHelper.CreateStandardGrid();

            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 45 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Mã Thuốc", FillWeight = 85 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên Thuốc", FillWeight = 160 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "Đơn Vị", FillWeight = 65 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn Giá", FillWeight = 90 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQuantity", HeaderText = "Tồn Kho", FillWeight = 70 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStockAlert", HeaderText = "Cảnh Báo Tối Thiểu", FillWeight = 85 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "Hạn Dùng", FillWeight = 95 });
            _dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusText", HeaderText = "Trạng Thái Kho", FillWeight = 135 });

            _dgvMedicines.Columns["UnitPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvMedicines.Columns["StockQuantity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvMedicines.Columns["MinStockAlert"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvMedicines.Columns["ExpiryDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvMedicines.CellFormatting += DgvMedicines_CellFormatting;
            _dgvMedicines.CellDoubleClick += BtnOpenEditor_Click;

            gridContainer.Controls.Add(legendFlow, 0, 0);
            gridContainer.Controls.Add(_dgvMedicines, 0, 1);
            grpGrid.Controls.Add(gridContainer);

            mainLayout.Controls.Add(grpToolbar, 0, 0);
            mainLayout.Controls.Add(grpGrid, 0, 1);

            rootLayout.Controls.Add(headerBanner, 0, 0);
            rootLayout.Controls.Add(mainLayout, 0, 1);
            this.Controls.Add(rootLayout);
        }

        // Tải toàn bộ danh sách thuốc từ CSDL và hiển thị lên lưới
        public void LoadMedicines()
        {
            _allMedicines = _controller.GetAllMedicines();
            ApplyFilter();
        }

        // Lọc danh sách thuốc theo Mã hoặc Tên thuốc người dùng nhập
        private void ApplyFilter()
        {
            string keyword = _txtSearchMed.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(keyword)
                ? _allMedicines
                : _allMedicines.Where(m =>
                    m.Code.ToLowerInvariant().Contains(keyword) ||
                    m.Name.ToLowerInvariant().Contains(keyword)).ToList();

            _dgvMedicines.Rows.Clear();
            foreach (var med in filtered)
            {
                string statusText = GetMedicineStatusText(med);
                int rowIndex = _dgvMedicines.Rows.Add(
                    med.Id,
                    med.Code,
                    med.Name,
                    med.Unit,
                    string.Format("{0:N0} đ", med.UnitPrice),
                    med.StockQuantity,
                    med.MinStockAlert,
                    FormatExpiryDate(med.ExpiryDate),
                    statusText
                );
                _dgvMedicines.Rows[rowIndex].Tag = med;
            }
        }

        // Tính chuỗi trạng thái hiển thị cho từng loại thuốc
        private static string GetMedicineStatusText(Medicine med)
        {
            if (IsNearExpiry(med.ExpiryDate, out int daysLeft))
            {
                return daysLeft < 0
                    ? "Đã quá hạn sử dụng"
                    : $"Sắp hết hạn (Còn {daysLeft} ngày)";
            }

            if (med.StockQuantity <= med.MinStockAlert)
            {
                return $"Sắp hết hàng ({med.StockQuantity} <= {med.MinStockAlert})";
            }

            return "Bình thường";
        }

        // Kiểm tra hạn dùng có nằm trong diện cảnh báo (<= 60 ngày) hay không
        private static bool IsNearExpiry(string? expiryDateStr, out int daysLeft)
        {
            daysLeft = int.MaxValue;
            if (string.IsNullOrWhiteSpace(expiryDateStr))
            {
                return false;
            }

            if (DateTime.TryParseExact(expiryDateStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate) ||
                DateTime.TryParse(expiryDateStr, out expiryDate))
            {
                daysLeft = (int)(expiryDate.Date - DateTime.Today).TotalDays;
                return daysLeft <= 60;
            }

            return false;
        }

        private static string FormatExpiryDate(string? expiryDateStr)
        {
            if (string.IsNullOrWhiteSpace(expiryDateStr))
            {
                return "---";
            }

            if (DateTime.TryParseExact(expiryDateStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate) ||
                DateTime.TryParse(expiryDateStr, out expiryDate))
            {
                return expiryDate.ToString("dd/MM/yyyy");
            }

            return expiryDateStr;
        }

        // Tô màu dòng tự động: Đỏ nhạt nếu cận hạn <= 60 ngày, Cam nhạt nếu tồn kho <= mức tối thiểu
        private void DgvMedicines_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgvMedicines.Rows.Count)
            {
                return;
            }

            var row = _dgvMedicines.Rows[e.RowIndex];
            if (row.Tag is not Medicine med)
            {
                return;
            }

            if (IsNearExpiry(med.ExpiryDate, out _))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(250, 219, 216);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(146, 43, 33);
                row.DefaultCellStyle.SelectionBackColor = UiHelper.DangerRed;
                row.DefaultCellStyle.SelectionForeColor = Color.White;
            }
            else if (med.StockQuantity <= med.MinStockAlert)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(253, 235, 208);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(147, 81, 22);
                row.DefaultCellStyle.SelectionBackColor = UiHelper.WarningOrange;
                row.DefaultCellStyle.SelectionForeColor = Color.White;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = UiHelper.DarkText;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
                row.DefaultCellStyle.SelectionForeColor = UiHelper.DarkText;
            }
        }

        // Mở màn hình thêm thuốc mới & nhập bổ sung tồn kho (#14)
        private void BtnOpenEditor_Click(object? sender, EventArgs e)
        {
            int? selectedMedicineId = null;
            if (_dgvMedicines.SelectedRows.Count > 0)
            {
                var row = _dgvMedicines.SelectedRows[0];
                if (row.Tag is Medicine med)
                {
                    selectedMedicineId = med.Id;
                }
                else if (row.Cells["Id"].Value is int id)
                {
                    selectedMedicineId = id;
                }
            }

            using var editor = new FormMedicineEditor(selectedMedicineId);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                LoadMedicines();
            }
        }

        // Mở màn hình báo cáo cảnh báo thuốc (#15)
        private void BtnOpenAlertReport_Click(object? sender, EventArgs e)
        {
            using var reportForm = new FormStockAlertReport();
            reportForm.ShowDialog(this);
            LoadMedicines();
        }
    }
}
