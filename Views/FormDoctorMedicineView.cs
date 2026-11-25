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
    /// <summary>
    /// TRA CỨU DANH MỤC THUỐC KHẢ DỤNG & TỒN KHO CHO BÁC SĨ
    /// Chuẩn hóa theo BaseForm và UiHelper (DPI-aware).
    /// </summary>
    public class FormDoctorMedicineView : BaseForm
    {
        private const string AllUnitsOption = "Tất cả đơn vị";

        private readonly DoctorController _controller = new();
        private List<Medicine> _allAvailableMedicines = new();

        // Các control khu vực tìm kiếm & lọc theo đơn vị
        private TextBox _txtSearch = null!;
        private ComboBox _cboUnitFilter = null!;
        private IconButton _btnFilter = null!;
        private Label _lblMedicineCount = null!;

        // Bảng danh mục thuốc khả dụng (StockQuantity > 0)
        private DataGridView _dgvDoctorMedicines = null!;

        public FormDoctorMedicineView()
        {
            InitializeComponent();
            RegisterEvents();
            LoadMedicines();
        }

        private void InitializeComponent()
        {
            this.Text = "Tra Cứu Danh Mục Thuốc & Tồn Kho (Bác Sĩ)";
            this.ConfigureAsFixedDialog(baseWidth: 1120, baseHeight: 650);
            this.Padding = UiHelper.Pad(14);

            // Bố cục chính 2 hàng: Bộ lọc phía trên (AutoSize) và Bảng danh mục thuốc (co giãn 100%)
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

            var grpFilter = BuildFilterGroup();
            var grpMedicineGrid = BuildMedicineGridGroup();

            mainLayout.Controls.Add(grpFilter, 0, 0);
            mainLayout.Controls.Add(grpMedicineGrid, 0, 1);

            this.Controls.Add(mainLayout);
        }

        // 1. Xây dựng thanh tìm kiếm nhanh & lọc theo đơn vị tính
        private GroupBox BuildFilterGroup()
        {
            var grpFilter = UiHelper.CreateGroupBox(
                "Tìm Kiếm & Lọc Danh Mục Thuốc Khả Dụng",
                titleColor: UiHelper.DarkText,
                padding: UiHelper.Pad(12, 8, 12, 10),
                margin: UiHelper.Pad(0, 0, 0, 10)
            );
            grpFilter.Dock = DockStyle.Top;
            grpFilter.AutoSize = true;

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var lblSearch = UiHelper.CreateLabel(
                text: "Tìm tên/mã thuốc:",
                font: UiHelper.FontRegular,
                margin: UiHelper.Pad(0, 0, 6, 0));
            lblSearch.Anchor = AnchorStyles.Left;

            _txtSearch = new TextBox
            {
                Width = UiHelper.S(210),
                Font = UiHelper.FontRegular,
                PlaceholderText = "Nhập tên hoặc mã thuốc...",
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 0, 12, 0)
            };

            var lblUnit = UiHelper.CreateLabel(
                text: "Đơn vị:",
                font: UiHelper.FontRegular,
                margin: UiHelper.Pad(0, 0, 6, 0));
            lblUnit.Anchor = AnchorStyles.Left;

            _cboUnitFilter = new ComboBox
            {
                Width = UiHelper.S(125),
                Font = UiHelper.FontRegular,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 0, 12, 0)
            };
            _cboUnitFilter.Items.AddRange(new object[] { AllUnitsOption, "Viên", "Vỉ", "Hộp", "Tuýp", "Lọ" });
            _cboUnitFilter.SelectedIndex = 0;

            _btnFilter = UiHelper.CreateButton("Lọc danh sách", IconChar.Filter, UiHelper.PrimaryBlue, 138, 34);
            _btnFilter.Anchor = AnchorStyles.Left;
            _btnFilter.Margin = Padding.Empty;

            filterFlow.Controls.Add(lblSearch);
            filterFlow.Controls.Add(_txtSearch);
            filterFlow.Controls.Add(lblUnit);
            filterFlow.Controls.Add(_cboUnitFilter);
            filterFlow.Controls.Add(_btnFilter);

            _lblMedicineCount = UiHelper.CreateLabel(
                text: "Tổng số: 0 loại thuốc",
                font: UiHelper.FontBold,
                foreColor: UiHelper.PrimaryBlue,
                margin: UiHelper.Pad(8, 0, 4, 0));
            _lblMedicineCount.Anchor = AnchorStyles.Right;

            toolbarLayout.Controls.Add(filterFlow, 0, 0);
            toolbarLayout.Controls.Add(_lblMedicineCount, 1, 0);
            grpFilter.Controls.Add(toolbarLayout);
            return grpFilter;
        }

        // 2. Xây dựng bảng danh mục thuốc còn tồn kho (ReadOnly)
        private GroupBox BuildMedicineGridGroup()
        {
            var grpGrid = UiHelper.CreateGroupBox(
                "DANH MỤC THUỐC CÒN TỒN KHO ĐỂ BÁC SĨ KÊ ĐƠN (STOCK > 0)",
                padding: UiHelper.Pad(12, 8, 12, 12),
                margin: Padding.Empty
            );

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

            // Thanh chú thích tình trạng tồn kho & hạn sử dụng
            var legendFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };

            var lblLegendOk = CreateLegendBadge(
                "Đủ thuốc kê đơn",
                Color.FromArgb(232, 248, 245),
                Color.FromArgb(20, 143, 119));
            var lblLegendLowStock = CreateLegendBadge(
                "Sắp hết hàng (Tồn <= Mức tối thiểu)",
                Color.FromArgb(253, 235, 208),
                Color.FromArgb(147, 81, 22));
            var lblLegendExpiry = CreateLegendBadge(
                "Cận hạn / Quá hạn sử dụng (<= 60 ngày)",
                Color.FromArgb(250, 219, 216),
                Color.FromArgb(146, 43, 33));

            legendFlow.Controls.Add(lblLegendOk);
            legendFlow.Controls.Add(lblLegendLowStock);
            legendFlow.Controls.Add(lblLegendExpiry);

            // Bảng danh mục thuốc chuẩn DPI
            _dgvDoctorMedicines = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 40);
            _dgvDoctorMedicines.DefaultCellStyle.ForeColor = UiHelper.DarkText;
            _dgvDoctorMedicines.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Code",
                HeaderText = "Mã Thuốc",
                FillWeight = 13
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Tên Thuốc & Hàm Lượng",
                FillWeight = 28
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Unit",
                HeaderText = "Đơn Vị Tính",
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UnitPrice",
                HeaderText = "Đơn Giá Bán",
                FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StockQuantity",
                HeaderText = "Tồn Khả Dụng",
                FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExpiryDate",
                HeaderText = "Hạn Sử Dụng",
                FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _dgvDoctorMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StatusNote",
                HeaderText = "Tình Trạng Kho",
                FillWeight = 22
            });

            gridContainer.Controls.Add(legendFlow, 0, 0);
            gridContainer.Controls.Add(_dgvDoctorMedicines, 0, 1);
            grpGrid.Controls.Add(gridContainer);
            return grpGrid;
        }

        private static Label CreateLegendBadge(string text, Color backColor, Color foreColor)
        {
            var lbl = UiHelper.CreateLabel(
                text: text,
                font: UiHelper.FontSmallBold,
                foreColor: foreColor,
                margin: UiHelper.Pad(0, 0, 8, 2));
            lbl.BackColor = backColor;
            lbl.Padding = UiHelper.Pad(8, 4, 8, 4);
            return lbl;
        }

        // --- CÁC PHƯƠNG THỨC NGHIỆP VỤ & SỰ KIỆN ---

        private void RegisterEvents()
        {
            _btnFilter.Click += (_, _) => ApplyFilter();
            _txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplyFilter();
                }
            };

            _dgvDoctorMedicines.CellFormatting += DgvDoctorMedicines_CellFormatting;
        }

        // Tải danh sách thuốc còn tồn kho (> 0) từ DoctorController
        public void LoadMedicines()
        {
            try
            {
                _allAvailableMedicines = _controller.GetAllMedicinesForPrescription();
                SyncUnitFilterOptions();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh mục thuốc khả dụng: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Đồng bộ danh sách đơn vị tính trong ComboBox (giữ mặc định + bổ sung đơn vị phát sinh trong CSDL nếu có)
        private void SyncUnitFilterOptions()
        {
            string? previousSelection = _cboUnitFilter.SelectedItem?.ToString();

            var defaultUnits = new[] { "Viên", "Vỉ", "Hộp", "Tuýp", "Lọ" };
            var dbUnits = _allAvailableMedicines
                .Select(m => m.Unit?.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .Cast<string>();

            var mergedUnits = defaultUnits
                .Concat(dbUnits)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _cboUnitFilter.BeginUpdate();
            _cboUnitFilter.Items.Clear();
            _cboUnitFilter.Items.Add(AllUnitsOption);
            foreach (var unit in mergedUnits)
            {
                _cboUnitFilter.Items.Add(unit);
            }

            int idx = !string.IsNullOrEmpty(previousSelection)
                ? _cboUnitFilter.Items.IndexOf(previousSelection)
                : 0;
            _cboUnitFilter.SelectedIndex = idx >= 0 ? idx : 0;
            _cboUnitFilter.EndUpdate();
        }

        // Lọc danh sách theo từ khóa tên/mã thuốc và đơn vị tính (chỉ kích hoạt khi bấm nút Lọc danh sách hoặc nhấn Enter)
        public void ApplyFilter()
        {
            try
            {
                _allAvailableMedicines = _controller.GetAllMedicinesForPrescription();
            }
            catch
            {
                // Giữ lại danh sách hiện tại nếu kết nối tạm bận
            }

            string keyword = _txtSearch.Text.Trim();
            string selectedUnit = _cboUnitFilter.SelectedItem?.ToString() ?? AllUnitsOption;
            bool filterByUnit = !string.Equals(selectedUnit, AllUnitsOption, StringComparison.Ordinal);

            var filtered = _allAvailableMedicines.Where(med =>
            {
                bool matchesKeyword = string.IsNullOrEmpty(keyword)
                    || med.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || med.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);

                bool matchesUnit = !filterByUnit
                    || string.Equals(med.Unit?.Trim(), selectedUnit, StringComparison.OrdinalIgnoreCase);

                return matchesKeyword && matchesUnit;
            }).ToList();

            _dgvDoctorMedicines.Rows.Clear();
            foreach (var med in filtered)
            {
                var (statusText, _, _) = GetMedicineStatusInfo(med);
                int rowIdx = _dgvDoctorMedicines.Rows.Add(
                    med.Code,
                    med.Name,
                    med.Unit,
                    $"{med.UnitPrice:N0} đ",
                    $"{med.StockQuantity:N0}",
                    FormatExpiryDate(med.ExpiryDate),
                    statusText
                );
                _dgvDoctorMedicines.Rows[rowIdx].Tag = med;
            }

            _lblMedicineCount.Text = $"Tổng số: {filtered.Count} loại thuốc";
        }

        private void DgvDoctorMedicines_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgvDoctorMedicines.Rows.Count)
            {
                return;
            }

            var row = _dgvDoctorMedicines.Rows[e.RowIndex];
            if (row.Tag is not Medicine med)
            {
                return;
            }

            var (_, statusForeColor, statusBackColor) = GetMedicineStatusInfo(med);
            string colName = _dgvDoctorMedicines.Columns[e.ColumnIndex].Name;

            if (colName == "Code" || colName == "StockQuantity")
            {
                e.CellStyle!.Font = UiHelper.FontBold;
            }

            if (colName == "StatusNote")
            {
                e.CellStyle!.Font = UiHelper.FontBold;
                e.CellStyle.ForeColor = statusForeColor;
                e.CellStyle.BackColor = statusBackColor;
            }
            else if (colName == "StockQuantity" && med.StockQuantity <= med.MinStockAlert)
            {
                e.CellStyle!.ForeColor = UiHelper.WarningOrange;
            }
        }

        private static (string Text, Color ForeColor, Color BackColor) GetMedicineStatusInfo(Medicine med)
        {
            if (IsNearOrPastExpiry(med.ExpiryDate, out int daysLeft))
            {
                string text = daysLeft < 0
                    ? "Đã quá hạn sử dụng"
                    : $"Sắp hết hạn (còn {daysLeft} ngày)";
                return (text, Color.FromArgb(146, 43, 33), Color.FromArgb(250, 219, 216));
            }

            if (med.StockQuantity <= med.MinStockAlert)
            {
                return (
                    $"Sắp hết hàng (tồn <= {med.MinStockAlert})",
                    Color.FromArgb(147, 81, 22),
                    Color.FromArgb(253, 235, 208)
                );
            }

            return (
                "Khả dụng (Đủ thuốc)",
                Color.FromArgb(20, 143, 119),
                Color.FromArgb(232, 248, 245)
            );
        }

        private static bool IsNearOrPastExpiry(string? expiryDateStr, out int daysLeft)
        {
            daysLeft = int.MaxValue;
            if (string.IsNullOrWhiteSpace(expiryDateStr))
            {
                return false;
            }

            if (DateTime.TryParseExact(expiryDateStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate)
                || DateTime.TryParse(expiryDateStr, out expiryDate))
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

            if (DateTime.TryParseExact(expiryDateStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate)
                || DateTime.TryParse(expiryDateStr, out expiryDate))
            {
                return expiryDate.ToString("dd/MM/yyyy");
            }

            return expiryDateStr;
        }
    }
}
