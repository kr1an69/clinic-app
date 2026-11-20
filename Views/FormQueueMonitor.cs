using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using ClinicManagement.Controllers;
using FontAwesome.Sharp;
using ClinicManagement.Helpers;

namespace ClinicManagement.Views
{
    public class FormQueueMonitor : BaseForm
    {
        private readonly ReceptionController _controller = new();
        private System.Windows.Forms.Timer _timer = null!;

        private Label _lblClock = null!;
        private Label _lblExamTitle = null!;
        private Label _lblExaminingStt = null!;
        private Label _lblExaminingName = null!;
        private Label _lblExaminingInfo = null!;
        private Label _lblWaitingCount = null!;
        private Panel _pnlExaminingCard = null!;
        private DataGridView _dgvWaiting = null!;

        public FormQueueMonitor()
        {
            InitializeComponent();
            InitTimer();
            LoadQueueData();
        }

        private void InitializeComponent()
        {
            this.Text = "Màn Hình TV Phòng Chờ";
            this.ConfigureAsFullScreen(minBaseWidth: 1050, minBaseHeight: 640);
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Padding = UiHelper.Pad(16);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = UiHelper.Pad(0, 0, 0, 10),
                Margin = Padding.Empty
            };
            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(51, 65, 85), Math.Max(1, UiHelper.S(1)));
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var iconTv = new IconPictureBox
            {
                IconChar = IconChar.Tv,
                IconFont = IconFont.Auto,
                IconColor = Color.FromArgb(56, 189, 248),
                IconSize = UiHelper.S(32),
                Size = UiHelper.SSize(32, 32),
                Anchor = AnchorStyles.Left,
                BackColor = Color.Transparent,
                Margin = UiHelper.Pad(0, 2, 10, 0)
            };

            var lblHeaderTitle = UiHelper.CreateLabel(
                text: "MÀN HÌNH HIỂN THỊ HÀNG ĐỢI PHÒNG CHỜ KHÁM BỆNH",
                font: new Font("Segoe UI", 14F, FontStyle.Bold),
                foreColor: Color.FromArgb(56, 189, 248),
                margin: UiHelper.Pad(0, 3, 0, 0));
            lblHeaderTitle.Anchor = AnchorStyles.Left;

            titleFlow.Controls.Add(iconTv);
            titleFlow.Controls.Add(lblHeaderTitle);

            
            _lblClock = UiHelper.CreateLabel(
                text: DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"),
                font: UiHelper.FontBold,
                foreColor: Color.FromArgb(167, 243, 208),
                margin: Padding.Empty);
            _lblClock.Anchor = AnchorStyles.Right;
            _lblClock.BackColor = Color.FromArgb(6, 95, 70);
            _lblClock.Padding = UiHelper.Pad(12, 6, 12, 6);

            

            headerLayout.Controls.Add(titleFlow, 0, 0);
            headerLayout.Controls.Add(_lblClock, 1, 0);
            headerPanel.Controls.Add(headerLayout);

            // Bố cục chính
            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = UiHelper.Pad(0, 12, 0, 0)
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Cột trái màn hình BN được gọi
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = UiHelper.Pad(18),
                Margin = UiHelper.Pad(0, 0, 8, 0)
            };

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));   
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); 
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));       


            _lblExamTitle = UiHelper.CreateLabel(
                text: "MỜI BỆNH NHÂN VÀO KHÁM",
                font: new Font("Segoe UI", 12F, FontStyle.Bold),
                foreColor: Color.FromArgb(74, 222, 128),
                margin: UiHelper.Pad(0, 0, 0, 12));

            // Thẻ bệnh nhân 
            _pnlExaminingCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = UiHelper.Pad(18, 16, 18, 16),
                Margin = Padding.Empty
            };
            _pnlExaminingCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(56, 189, 248), Math.Max(1, UiHelper.S(2)));
                e.Graphics.DrawRectangle(pen, 1, 1, _pnlExaminingCard.Width - 3, _pnlExaminingCard.Height - 3);
            };

            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var infoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent,
                Padding = UiHelper.Pad(0, 0, 14, 0),
                Margin = Padding.Empty
            };
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var lblClinic = new Label
            {
                Text = "PHÒNG KHÁM NỘI TỔNG QUÁT 01 - BS.CKI NGUYỄN VĂN AN",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Margin = UiHelper.Pad(0, 0, 0, 14),
                UseMnemonic = false
            };

            _lblExaminingName = new Label
            {
                Text = "CHƯA CÓ BỆNH NHÂN",
                Font = new Font("Segoe UI", 23F, FontStyle.Bold),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Margin = UiHelper.Pad(0, 0, 0, 14),
                UseMnemonic = false
            };

            _lblExaminingInfo = new Label
            {
                Text = "Phòng khám đang sẵn sàng tiếp nhận bệnh nhân...",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(56, 189, 248),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Margin = Padding.Empty,
                UseMnemonic = false
            };

            infoLayout.Controls.Add(lblClinic, 0, 1);
            infoLayout.Controls.Add(_lblExaminingName, 0, 2);
            infoLayout.Controls.Add(_lblExaminingInfo, 0, 3);

            // Khung ô số thứ tự 
            var pnlSttBadge = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = UiHelper.Pad(8),
                Margin = UiHelper.Pad(4, 4, 0, 4)
            };
            pnlSttBadge.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(250, 204, 21), Math.Max(1, UiHelper.S(2)));
                e.Graphics.DrawRectangle(pen, 1, 1, pnlSttBadge.Width - 3, pnlSttBadge.Height - 3);
            };

            _lblExaminingStt = new Label
            {
                Text = "--",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };
            pnlSttBadge.Controls.Add(_lblExaminingStt);

            cardLayout.Controls.Add(infoLayout, 0, 0);
            cardLayout.Controls.Add(pnlSttBadge, 1, 0);
            _pnlExaminingCard.Controls.Add(cardLayout);

            // Khung thông báo lưu ý 
            var pnlNotice = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = UiHelper.Pad(14, 12, 14, 12),
                Margin = Padding.Empty
            };
            pnlNotice.Paint += (s, e) =>
            {
                using var b = new SolidBrush(Color.FromArgb(250, 204, 21));
                e.Graphics.FillRectangle(b, 0, 0, UiHelper.S(4), pnlNotice.Height);
            };

            var noticeLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            noticeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            noticeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            noticeLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var iconNotice = new IconPictureBox
            {
                IconChar = IconChar.CircleInfo,
                IconFont = IconFont.Auto,
                IconColor = Color.FromArgb(250, 204, 21),
                IconSize = UiHelper.S(20),
                Size = UiHelper.SSize(20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = Color.Transparent,
                Margin = UiHelper.Pad(0, 2, 10, 0)
            };

            var lblNotice = new Label
            {
                Text = "Quý bệnh nhân vui lòng chuẩn bị sẵn thẻ BHYT/CCCD và chú ý lắng nghe gọi số thứ tự.",
                Font = UiHelper.FontRegular,
                ForeColor = Color.FromArgb(226, 232, 240),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = Padding.Empty,
                UseMnemonic = false
            };

            noticeLayout.Controls.Add(iconNotice, 0, 0);
            noticeLayout.Controls.Add(lblNotice, 1, 0);
            pnlNotice.Controls.Add(noticeLayout);

            leftLayout.Controls.Add(_lblExamTitle, 0, 0);
            leftLayout.Controls.Add(_pnlExaminingCard, 0, 1);
            leftLayout.Controls.Add(pnlNotice, 0, 3);
            leftPanel.Controls.Add(leftLayout);

            // Cột phải danh sách BN đang đợi
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = UiHelper.Pad(18),
                Margin = UiHelper.Pad(8, 0, 0, 0)
            };

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var rightHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = UiHelper.Pad(0, 0, 0, 12)
            };
            rightHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            rightHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            rightHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblWaitTitle = UiHelper.CreateLabel(
                text: "DANH SÁCH BỆNH NHÂN ĐANG ĐỢI",
                font: new Font("Segoe UI", 12F, FontStyle.Bold),
                foreColor: Color.FromArgb(250, 204, 21),
                margin: Padding.Empty);
            lblWaitTitle.Anchor = AnchorStyles.Left;

            _lblWaitingCount = UiHelper.CreateLabel(
                text: "Đang chờ: 0 BN",
                font: UiHelper.FontBold,
                foreColor: Color.FromArgb(226, 232, 240),
                margin: Padding.Empty);
            _lblWaitingCount.Anchor = AnchorStyles.Right;

            rightHeader.Controls.Add(lblWaitTitle, 0, 0);
            rightHeader.Controls.Add(_lblWaitingCount, 1, 0);

            _dgvWaiting = UiHelper.CreateStandardGrid(baseRowHeight: 42, baseHeaderHeight: 40);
            _dgvWaiting.BackgroundColor = Color.FromArgb(15, 23, 42);
            _dgvWaiting.BorderStyle = BorderStyle.None;
            _dgvWaiting.GridColor = Color.FromArgb(51, 65, 85);
            _dgvWaiting.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51, 65, 85);
            _dgvWaiting.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(250, 204, 21);
            _dgvWaiting.ColumnHeadersDefaultCellStyle.Font = UiHelper.FontBold;
            _dgvWaiting.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dgvWaiting.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            _dgvWaiting.DefaultCellStyle.ForeColor = Color.White;
            _dgvWaiting.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            _dgvWaiting.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            _dgvWaiting.DefaultCellStyle.SelectionForeColor = Color.White;

            _dgvWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "SỐ THỨ TỰ", FillWeight = 75 });
            _dgvWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "HỌ VÀ TÊN BỆNH NHÂN", FillWeight = 165 });
            _dgvWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "TRẠNG THÁI", FillWeight = 105 });

            _dgvWaiting.Columns["QueueNumber"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvWaiting.Columns["QueueNumber"]!.DefaultCellStyle.ForeColor = Color.FromArgb(250, 204, 21);
            _dgvWaiting.Columns["Status"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dgvWaiting.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _dgvWaiting.Rows.Count) return;
                var row = _dgvWaiting.Rows[e.RowIndex];
                if (e.RowIndex == 0)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
                    if (_dgvWaiting.Columns[e.ColumnIndex].Name == "Status")
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(74, 222, 128);
                    }
                }
                else if (_dgvWaiting.Columns[e.ColumnIndex].Name == "Status")
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(148, 163, 184);
                }
            };

            rightLayout.Controls.Add(rightHeader, 0, 0);
            rightLayout.Controls.Add(_dgvWaiting, 0, 1);
            rightPanel.Controls.Add(rightLayout);

            bodyLayout.Controls.Add(leftPanel, 0, 0);
            bodyLayout.Controls.Add(rightPanel, 1, 0);

            rootLayout.Controls.Add(headerPanel, 0, 0);
            rootLayout.Controls.Add(bodyLayout, 0, 1);
            this.Controls.Add(rootLayout);

            this.FormClosing += (s, e) =>
            {
                _timer.Stop();
                _timer.Dispose();
            };
        }

        private void InitTimer()
        {
            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += (s, e) =>
            {
                _lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss");
                LoadQueueData();
            };
            _timer.Start();
        }

        public void LoadQueueData()
        {
            try
            {
                var queues = _controller.GetCurrentWaitingQueues();

                // Ưu tiên hiển thị người đang khám, nếu chưa có ai đang khám thì hiển thị người có STT kế (Waiting)
                var currentPatient = queues.FirstOrDefault(q => q.Status == "Examining")
                                  ?? queues.FirstOrDefault(q => q.Status == "Waiting");

                if (currentPatient != null)
                {
                    bool isExamining = currentPatient.Status == "Examining";
                    _lblExamTitle.Text = isExamining
                        ? "MỜI BỆNH NHÂN ĐANG KHÁM "
                        : "MỜI BỆNH NHÂN VÀO KHÁM";

                    _lblExaminingStt.Text = $"STT {currentPatient.QueueNumber:D2}";
                    _lblExaminingName.Text = (currentPatient.PatientName ?? "BỆNH NHÂN").ToUpper();

                    string p = currentPatient.Phone ?? string.Empty;
                    string maskPhone = p.Length >= 7 ? (p[..4] + "***" + p[^3..]) : p;
                    string statusLabel = isExamining ? "Đang khám bệnh" : "Mời vào phòng khám";

                    _lblExaminingInfo.ForeColor = isExamining
                        ? Color.FromArgb(74, 222, 128)
                        : Color.FromArgb(56, 189, 248);

                    _lblExaminingInfo.Text = string.IsNullOrEmpty(maskPhone)
                        ? $"Trạng thái: {statusLabel}"
                        : $"SĐT: {maskPhone}  |  Trạng thái: {statusLabel}";
                }
                else
                {
                    _lblExamTitle.Text = "MỜI BỆNH NHÂN VÀO KHÁM";
                    _lblExaminingStt.Text = "--";
                    _lblExaminingName.Text = "CHƯA CÓ BỆNH NHÂN";
                    _lblExaminingInfo.ForeColor = Color.FromArgb(56, 189, 248);
                    _lblExaminingInfo.Text = "Phòng khám đang sẵn sàng tiếp nhận bệnh nhân...";
                }

                // Danh sách hiển thị những bệnh nhân đang đợi kế tiếp
                var waitingList = queues
                    .Where(q => currentPatient == null || q.Id != currentPatient.Id)
                    .ToList();

                _lblWaitingCount.Text = $"Đang chờ: {waitingList.Count} BN";

                _dgvWaiting.Rows.Clear();
                for (int i = 0; i < waitingList.Count; i++)
                {
                    var item = waitingList[i];
                    string statusDisplay = (i == 0) ? "Chuẩn bị vào khám" : "Đang chờ";
                    _dgvWaiting.Rows.Add(
                        $" {item.QueueNumber:D2}",
                        (item.PatientName ?? string.Empty).ToUpper(),
                        statusDisplay);
                }
            }
            catch
            {
                // Bỏ qua lỗi đọc tạm thời khi Timer polling
            }
        }
    }
}