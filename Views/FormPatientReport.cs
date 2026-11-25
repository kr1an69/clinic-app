using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormPatientReport : BaseForm
    {
        private readonly ReceptionController _controller = new();

        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;

        private Label _lblKpiTotal = null!;
        private Label _lblKpiCompleted = null!;
        private Label _lblKpiWaiting = null!;
        private Label _lblKpiAvgPerDay = null!;

        private DataGridView _dgvReport = null!;

        public FormPatientReport()
        {
            InitializeComponent();
            LoadReportData();
        }

        private void InitializeComponent()
        {
            this.Text = "Báo Cáo & Thống Kê Bệnh Nhân Theo Ngày";
            this.Dock = DockStyle.Fill;
            this.Padding = UiHelper.Pad(12);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Thanh lọc thời gian
            var grpFilter = UiHelper.CreateGroupBox(
                title: "Thời Gian Thống Kê Tiếp Nhận",
                padding: UiHelper.Pad(12, 8, 12, 10),
                margin: UiHelper.Pad(0, 0, 0, 10));
            grpFilter.AutoSize = true;
            grpFilter.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1
            };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var leftFilterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Width = UiHelper.S(130),
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 12, 0)
            };

            _dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = UiHelper.S(130),
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 12, 0)
            };

            var btnFilter = UiHelper.CreateButton("Lọc thống kê", IconChar.Filter, UiHelper.PrimaryBlue, minWidth: 115, height: 34);
            btnFilter.Click += (s, e) => LoadReportData();

            leftFilterFlow.Controls.Add(UiHelper.CreateLabel("Từ ngày:", font: UiHelper.FontRegular, margin: UiHelper.Pad(0, 6, 6, 0)));
            leftFilterFlow.Controls.Add(_dtpFrom);
            leftFilterFlow.Controls.Add(UiHelper.CreateLabel("Đến ngày:", font: UiHelper.FontRegular, margin: UiHelper.Pad(0, 6, 6, 0)));
            leftFilterFlow.Controls.Add(_dtpTo);
            leftFilterFlow.Controls.Add(btnFilter);

            var rightFilterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var btnRefresh = UiHelper.CreateButton("Làm mới dữ liệu", IconChar.ArrowsRotate, UiHelper.SuccessGreen, minWidth: 145, height: 34);
            btnRefresh.Click += (s, e) =>
            {
                _dtpFrom.Value = DateTime.Today.AddDays(-30);
                _dtpTo.Value = DateTime.Today;
                LoadReportData();
            };
            rightFilterFlow.Controls.Add(btnRefresh);

            filterLayout.Controls.Add(leftFilterFlow, 0, 0);
            filterLayout.Controls.Add(rightFilterFlow, 1, 0);
            grpFilter.Controls.Add(filterLayout);

            // 4 thẻ KPI
            var kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                Height = UiHelper.S(96),
                Margin = UiHelper.Pad(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            kpiTable.Controls.Add(CreateKpiCard("TỔNG LƯỢT ĐĂNG KÝ", IconChar.Users, UiHelper.PrimaryBlue, out _lblKpiTotal), 0, 0);
            kpiTable.Controls.Add(CreateKpiCard("ĐÃ KHÁM HOÀN TẤT", IconChar.CircleCheck, UiHelper.SuccessGreen, out _lblKpiCompleted), 1, 0);
            kpiTable.Controls.Add(CreateKpiCard("ĐANG CHỜ / ĐANG KHÁM", IconChar.UserClock, UiHelper.WarningOrange, out _lblKpiWaiting), 2, 0);
            kpiTable.Controls.Add(CreateKpiCard("TRUNG BÌNH MỖI NGÀY", IconChar.ChartLine, UiHelper.DangerRed, out _lblKpiAvgPerDay), 3, 0);

            // Bảng tổng kết chi tiết
            var grpGrid = UiHelper.CreateGroupBox("Bảng Thống Kê Chi Tiết Số Lượng Bệnh Nhân Theo Từng Ngày", padding: UiHelper.Pad(10));
            _dgvReport = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 36);

            _dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExamDate", HeaderText = "Ngày Khám", FillWeight = 110 });
            _dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPatients", HeaderText = "Tổng Số Bệnh Nhân", FillWeight = 90 });
            _dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompletedCount", HeaderText = "Số Ca Đã Hoàn Tất", FillWeight = 90 });
            _dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "WaitingCount", HeaderText = "Số Ca Đang Chờ / Khám", FillWeight = 90 });
            _dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompletionRate", HeaderText = "Tỷ Lệ Hoàn Thành", FillWeight = 95 });

            _dgvReport.Columns["TotalPatients"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvReport.Columns["CompletedCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvReport.Columns["WaitingCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvReport.Columns["CompletionRate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            _dgvReport.DefaultCellStyle.ForeColor = Color.Black;
            _dgvReport.RowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvReport.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvReport.DefaultCellStyle.SelectionForeColor = Color.Black;

            grpGrid.Controls.Add(_dgvReport);

            rootLayout.Controls.Add(grpFilter, 0, 0);
            rootLayout.Controls.Add(kpiTable, 0, 1);
            rootLayout.Controls.Add(grpGrid, 0, 2);
            this.Controls.Add(rootLayout);
        }

        private static Panel CreateKpiCard(string title, IconChar icon, Color accentColor, out Label lblValue)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = UiHelper.Pad(12, 8, 12, 8),
                Margin = UiHelper.Pad(3)
            };
            pnl.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, UiHelper.S(4), pnl.Height);
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            var cardLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var textFlow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            textFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblValue = UiHelper.CreateLabel("0", font: new Font("Segoe UI", 15F, FontStyle.Bold), foreColor: UiHelper.DarkText);

            textFlow.Controls.Add(UiHelper.CreateLabel(title, font: UiHelper.FontSmallBold, foreColor: UiHelper.SecondaryGray));
            textFlow.Controls.Add(lblValue);

            var iconBox = new IconPictureBox
            {
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = accentColor,
                IconSize = UiHelper.S(30),
                Size = UiHelper.SSize(30, 30),
                Anchor = AnchorStyles.Right,
                BackColor = Color.Transparent
            };

            cardLayout.Controls.Add(textFlow, 0, 0);
            cardLayout.Controls.Add(iconBox, 1, 0);
            pnl.Controls.Add(cardLayout);

            return pnl;
        }

        public void LoadReportData()
        {
            DateTime fromDate = _dtpFrom.Value.Date;
            DateTime toDate = _dtpTo.Value.Date;

            if (fromDate > toDate)
            {
                MessageBox.Show("Ngày bắt đầu ('Từ ngày') không được lớn hơn ngày kết thúc ('Đến ngày')!",
                    "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var stats = _controller.GetDailyPatientStatistics(fromDate, toDate);

            int totalPatients = stats.Sum(s => s.TotalPatients);
            int totalCompleted = stats.Sum(s => s.CompletedCount);
            int totalWaiting = stats.Sum(s => s.WaitingCount);
            double avgPerDay = stats.Count > 0 ? (double)totalPatients / stats.Count : 0;

            _lblKpiTotal.Text = $"{totalPatients} Lượt";
            _lblKpiCompleted.Text = $"{totalCompleted} Lượt";
            _lblKpiWaiting.Text = $"{totalWaiting} Lượt";
            _lblKpiAvgPerDay.Text = $"{avgPerDay:0.0} BN/ngày";

            _dgvReport.Rows.Clear();
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");

            foreach (var item in stats)
            {
                string rawDate = item.ExamDate?.ToString() ?? string.Empty;
                string cleanDate = DateTime.TryParse(rawDate, out DateTime parsedDate)
                    ? parsedDate.ToString("yyyy-MM-dd")
                    : (rawDate.Length >= 10 ? rawDate[..10] : rawDate);

                string rateStr;
                if (item.TotalPatients == 0)
                {
                    rateStr = "0%";
                }
                else
                {
                    double rate = (double)item.CompletedCount / item.TotalPatients * 100;
                    rateStr = (cleanDate == todayStr && item.WaitingCount > 0)
                        ? $"Đang diễn ra ({rate:0.#}%)"
                        : $"{rate:0.#}%";
                }

                _dgvReport.Rows.Add(
                    cleanDate == todayStr ? $"{cleanDate} (Hôm nay)" : cleanDate,
                    item.TotalPatients,
                    item.CompletedCount,
                    item.WaitingCount,
                    rateStr
                );
            }
        }
    }
}