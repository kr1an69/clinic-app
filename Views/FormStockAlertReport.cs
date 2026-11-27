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
    public class FormStockAlertReport : BaseForm
    {
        private readonly PharmacyController _controller = new();
        private List<Medicine> _alertMedicines = new();
        private List<InvoiceHistoryItem> _invoices = new();

        private Label _lblLowStockCount = null!;
        private Label _lblNearExpiryCount = null!;
        private Label _lblTotalMedicinesCount = null!;
        private Label _lblTotalRevenue = null!;

        private DataGridView _dgvAlerts = null!;
        private DataGridView _dgvInvoices = null!;
        private IconButton _btnRestockSelected = null!;
        private IconButton _btnRefresh = null!;
        private IconButton _btnReprintInvoice = null!;
        private IconButton _btnClose = null!;

        public FormStockAlertReport()
        {
            InitializeComponent();
            LoadReportData();
        }

        private void InitializeComponent()
        {
            this.Text = "Báo Cáo Cảnh Báo Kho Thuốc & Nhật Ký Hóa Đơn";
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UiHelper.BgMain,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headerBanner = UiHelper.CreateHeaderBanner(
                "BÁO CÁO CẢNH BÁO KHO THUỐC & SỔ HÓA ĐƠN",
                "Theo dõi thuốc sắp hết hàng, thuốc cận hạn sử dụng (<= 60 ngày) và nhật ký hóa đơn đã thanh toán");

            // Hàng 1: 4 thẻ KPI tổng hợp
            var kpiGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1,
                Padding = UiHelper.Pad(14, 12, 14, 4),
                Margin = Padding.Empty
            };
            for (int i = 0; i < 4; i++)
            {
                kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }
            kpiGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            kpiGrid.Controls.Add(CreateKpiCard("THUỐC SẮP HẾT HÀNG", UiHelper.WarningOrange, IconChar.BoxesStacked, out _lblLowStockCount, UiHelper.Pad(0, 0, 8, 0)), 0, 0);
            kpiGrid.Controls.Add(CreateKpiCard("THUỐC CẬN HẠN (<= 60 NGÀY)", UiHelper.DangerRed, IconChar.TriangleExclamation, out _lblNearExpiryCount, UiHelper.Pad(4, 0, 4, 0)), 1, 0);
            kpiGrid.Controls.Add(CreateKpiCard("TỔNG MẶT HÀNG THUỐC", UiHelper.PrimaryBlue, IconChar.Pills, out _lblTotalMedicinesCount, UiHelper.Pad(4, 0, 4, 0)), 2, 0);
            kpiGrid.Controls.Add(CreateKpiCard("TỔNG DOANH THU ĐÃ THU", UiHelper.SuccessGreen, IconChar.CashRegister, out _lblTotalRevenue, UiHelper.Pad(8, 0, 0, 0)), 3, 0);

            // Hàng 2: 2 bảng chia đôi trái (Cảnh báo thuốc) & phải (Nhật ký hóa đơn)
            var bodyColumns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = UiHelper.Pad(14, 6, 14, 14),
                Margin = Padding.Empty
            };
            bodyColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bodyColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bodyColumns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            bodyColumns.Controls.Add(BuildAlertPanel(), 0, 0);
            bodyColumns.Controls.Add(BuildInvoicePanel(), 1, 0);

            rootLayout.Controls.Add(headerBanner, 0, 0);
            rootLayout.Controls.Add(kpiGrid, 0, 1);
            rootLayout.Controls.Add(bodyColumns, 0, 2);

            this.Controls.Add(rootLayout);
        }

        // Tạo thẻ KPI đồng bộ bằng TableLayoutPanel AutoSize
        private static Panel CreateKpiCard(string title, Color accentColor, IconChar icon, out Label valueLabel, Padding margin)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = UiHelper.Pad(14, 12, 14, 12),
                Margin = margin
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = UiHelper.CreateLabel(title, UiHelper.FontSmallBold, UiHelper.SecondaryGray, UiHelper.Pad(0, 0, 6, 4));

            valueLabel = UiHelper.CreateLabel("0", new Font("Segoe UI", 15F, FontStyle.Bold), accentColor, Padding.Empty);

            var iconBox = new IconPictureBox
            {
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = accentColor,
                IconSize = UiHelper.S(28),
                Size = UiHelper.SSize(32, 32),
                Anchor = AnchorStyles.Right,
                Margin = UiHelper.Pad(8, 2, 0, 0)
            };

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(valueLabel, 0, 1);
            layout.Controls.Add(iconBox, 1, 0);
            layout.SetRowSpan(iconBox, 2);

            card.Controls.Add(layout);
            return card;
        }

        // Dựng bảng trái: Danh sách thuốc cảnh báo sắp hết hàng / cận date
        private GroupBox BuildAlertPanel()
        {
            var grpAlert = UiHelper.CreateGroupBox(
                "1. DANH SÁCH THUỐC CẢNH BÁO TỒN KHO & HẠN DÙNG",
                UiHelper.WarningOrange,
                UiHelper.Pad(12),
                UiHelper.Pad(0, 0, 8, 0));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _dgvAlerts = UiHelper.CreateStandardGrid();
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Mã", Width = UiHelper.S(85), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên thuốc" });
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQuantity", HeaderText = "Tồn", Width = UiHelper.S(60), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStockAlert", HeaderText = "Định mức", Width = UiHelper.S(78), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "Hạn dùng", Width = UiHelper.S(95), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "Cảnh báo", Width = UiHelper.S(130), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

            _dgvAlerts.Columns["StockQuantity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvAlerts.Columns["MinStockAlert"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvAlerts.Columns["ExpiryDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvAlerts.CellFormatting += DgvAlerts_CellFormatting;

            var bottomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = UiHelper.Pad(0, 10, 0, 0)
            };

            _btnRestockSelected = UiHelper.CreateButton("Nhập Bổ Sung Thuốc", IconChar.Plus, UiHelper.SuccessGreen, 200, 36);
            _btnRestockSelected.Click += BtnRestockSelected_Click;

            _btnRefresh = UiHelper.CreateButton("Làm mới", IconChar.RotateRight, UiHelper.PrimaryBlue, 115, 36);
            _btnRefresh.Click += (_, _) => LoadReportData();

            bottomFlow.Controls.Add(_btnRestockSelected);
            bottomFlow.Controls.Add(_btnRefresh);

            layout.Controls.Add(_dgvAlerts, 0, 0);
            layout.Controls.Add(bottomFlow, 0, 1);

            grpAlert.Controls.Add(layout);
            return grpAlert;
        }

        // Dựng bảng phải: Nhật ký hóa đơn đã thanh toán
        private GroupBox BuildInvoicePanel()
        {
            var grpInvoice = UiHelper.CreateGroupBox(
                "2. NHẬT KÝ HÓA ĐƠN ĐÃ THU TIỀN",
                UiHelper.PrimaryBlue,
                UiHelper.Pad(12),
                UiHelper.Pad(8, 0, 0, 0));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _dgvInvoices = UiHelper.CreateStandardGrid();
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvoiceId", HeaderText = "Mã HĐ", Width = UiHelper.S(70), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Bệnh nhân" });
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExamFee", HeaderText = "Tiền khám", Width = UiHelper.S(95), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedicineFee", HeaderText = "Tiền thuốc", Width = UiHelper.S(95), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "Tổng thu", Width = UiHelper.S(105), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaidAt", HeaderText = "Thời gian thu", Width = UiHelper.S(130), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

            _dgvInvoices.Columns["ExamFee"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvInvoices.Columns["MedicineFee"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvInvoices.Columns["TotalAmount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvInvoices.Columns["PaidAt"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvInvoices.CellDoubleClick += (_, _) => OpenSelectedInvoicePreview();

            var bottomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                Margin = UiHelper.Pad(0, 10, 0, 0)
            };

            _btnClose = UiHelper.CreateTextButton("Đóng", UiHelper.SecondaryGray, 95, 36);
            _btnClose.Click += (_, _) => this.Close();

            _btnReprintInvoice = UiHelper.CreateButton("Xem & In Lại Hóa Đơn", IconChar.Print, UiHelper.PrimaryBlue, 210, 36);
            _btnReprintInvoice.Click += (_, _) => OpenSelectedInvoicePreview();

            bottomFlow.Controls.Add(_btnClose);
            bottomFlow.Controls.Add(_btnReprintInvoice);

            layout.Controls.Add(_dgvInvoices, 0, 0);
            layout.Controls.Add(bottomFlow, 0, 1);

            grpInvoice.Controls.Add(layout);
            return grpInvoice;
        }

        // Tải toàn bộ số liệu KPI, thuốc cảnh báo và sổ hóa đơn từ CSDL
        private void LoadReportData()
        {
            var allMedicines = _controller.GetAllMedicines();
            _alertMedicines = _controller.GetAlertMedicines();
            _invoices = _controller.GetInvoicesHistory(200);
            var totalRevenue = _controller.GetTotalRevenue();

            int lowStockCount = allMedicines.Count(m => m.StockQuantity <= m.MinStockAlert);
            int nearExpiryCount = allMedicines.Count(IsNearExpiry);

            _lblLowStockCount.Text = lowStockCount.ToString();
            _lblNearExpiryCount.Text = nearExpiryCount.ToString();
            _lblTotalMedicinesCount.Text = allMedicines.Count.ToString();
            _lblTotalRevenue.Text = $"{totalRevenue:N0} đ";

            _dgvAlerts.Rows.Clear();
            foreach (var med in _alertMedicines)
            {
                string reason = BuildAlertReason(med);
                int rowIndex = _dgvAlerts.Rows.Add(
                    med.Code,
                    med.Name,
                    med.StockQuantity,
                    med.MinStockAlert,
                    FormatExpiryDate(med.ExpiryDate),
                    reason);
                _dgvAlerts.Rows[rowIndex].Tag = med;
            }

            _dgvInvoices.Rows.Clear();
            foreach (var inv in _invoices)
            {
                int rowIndex = _dgvInvoices.Rows.Add(
                    $"#{inv.InvoiceId}",
                    inv.PatientName,
                    $"{inv.ExamFee:N0} đ",
                    $"{inv.MedicineFee:N0} đ",
                    $"{inv.TotalAmount:N0} đ",
                    inv.PaidAt.ToString("dd/MM/yyyy HH:mm"));
                _dgvInvoices.Rows[rowIndex].Tag = inv;
            }
        }

        private void DgvAlerts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgvAlerts.Rows.Count)
            {
                return;
            }

            if (_dgvAlerts.Rows[e.RowIndex].Tag is not Medicine med)
            {
                return;
            }

            var row = _dgvAlerts.Rows[e.RowIndex];
            if (IsNearExpiry(med))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(253, 237, 236);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(169, 50, 38);
            }
            else if (med.StockQuantity <= med.MinStockAlert)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(253, 235, 208);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(147, 81, 22);
            }
        }

        private void BtnRestockSelected_Click(object? sender, EventArgs e)
        {
            int? selectedMedicineId = null;
            if (_dgvAlerts.SelectedRows.Count > 0 && _dgvAlerts.SelectedRows[0].Tag is Medicine med)
            {
                selectedMedicineId = med.Id;
            }

            using var editor = new FormMedicineEditor(selectedMedicineId);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                LoadReportData();
            }
        }

        private void OpenSelectedInvoicePreview()
        {
            if (_dgvInvoices.SelectedRows.Count == 0 || _dgvInvoices.SelectedRows[0].Tag is not InvoiceHistoryItem inv)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn trong nhật ký để xem và in lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var printForm = new FormPrintInvoice(inv.InvoiceId);
            printForm.ShowDialog(this);
        }

        private static bool IsNearExpiry(Medicine med)
        {
            if (string.IsNullOrWhiteSpace(med.ExpiryDate))
            {
                return false;
            }

            if (DateTime.TryParseExact(med.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
            {
                return expiry.Date <= DateTime.Today.AddDays(60);
            }

            return DateTime.TryParse(med.ExpiryDate, out expiry) && expiry.Date <= DateTime.Today.AddDays(60);
        }

        private static string FormatExpiryDate(string? expiryDateStr)
        {
            if (string.IsNullOrWhiteSpace(expiryDateStr))
            {
                return "---";
            }

            if (DateTime.TryParseExact(expiryDateStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry) ||
                DateTime.TryParse(expiryDateStr, out expiry))
            {
                return expiry.ToString("dd/MM/yyyy");
            }

            return expiryDateStr;
        }

        private static string BuildAlertReason(Medicine med)
        {
            bool lowStock = med.StockQuantity <= med.MinStockAlert;
            bool nearExpiry = IsNearExpiry(med);

            if (lowStock && nearExpiry)
            {
                return "Sắp hết & Cận date";
            }

            if (nearExpiry)
            {
                return "Cận hạn sử dụng";
            }

            return "Sắp hết hàng";
        }
    }
}
