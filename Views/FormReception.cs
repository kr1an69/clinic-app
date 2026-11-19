using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using ClinicManagement.Controllers;
using FontAwesome.Sharp;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using System.Drawing.Drawing2D;

namespace ClinicManagement.Views
{
    public class FormReception : BaseForm
    {
        private readonly ReceptionController _controller = new();
        private System.Windows.Forms.Timer _refreshTimer = null!;
        
        private TextBox _txtQuickSearch = null!;
        private int? _selectedPatientId = null;
        private TextBox _txtFullName = null!;
        private TextBox _txtPhone = null!;
        private ComboBox _cboGender = null!;
        private NumericUpDown _numBirthYear = null!;
        private DateTimePicker _dtpRegDate = null!;
        private TextBox _txtAddress = null!;

        private Label _lblTicketNumber = null!;
        private Label _lblTicketPatient = null!;
        private Label _lblTicketStatus = null!;

        private DataGridView _dgvTodayQueue = null!;
        
        public FormReception()
        {
            InitializeComponent();
            LoadTodayQueue();
        }

        private void InitializeComponent()
        {
            this.Text = "Tiếp Nhận Bệnh Nhân & Cấp Số Thứ Tự";
            this.Dock = DockStyle.Fill;
            this.Padding = UiHelper.Pad(12);
            
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = UiHelper.Pad(0, 0, 12, 0)
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Cột trái GroupBox thông tin bệnh nhân
            var grpPatientInput = UiHelper.CreateGroupBox(
                title: "1. THÔNG TIN BỆNH NHÂN ĐĂNG KÝ KHÁM",
                titleColor: UiHelper.PrimaryBlue,
                padding: UiHelper.Pad(14, 12, 14, 14));
            grpPatientInput.Dock = DockStyle.Top;
            grpPatientInput.AutoSize = true;
            grpPatientInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 9,
                Font = UiHelper.FontRegular
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 9; i++)
            {
                formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Tra cứu nhanh BN cũ 
            var lblSearch = UiHelper.CreateLabel(
                "Tra cứu nhanh BN cũ (SĐT / Họ tên):",
                font: UiHelper.FontLabelBold,
                margin: UiHelper.Pad(0, 0, 0, 4));

            var pnlSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };
            pnlSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlSearch.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtQuickSearch = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Nhập SĐT hoặc Tên...",
                Font = UiHelper.FontRegular,
                Margin = Padding.Empty
            };
            _txtQuickSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    QuickSearchPatient();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            var btnQuickFind = UiHelper.CreateButton("Tìm", IconChar.MagnifyingGlass, UiHelper.SidebarBtnBg, minWidth: 74, height: 32, baseIconSize: 14);
            btnQuickFind.Click += (s, e) => QuickSearchPatient();

            var btnOpenPatientManager = UiHelper.CreateButton("Chọn hồ sơ", IconChar.AddressBook, UiHelper.PrimaryBlue, minWidth: 125, height: 32, baseIconSize: 14);
            btnOpenPatientManager.Click += BtnOpenPatientManager_Click;

            pnlSearch.Controls.Add(_txtQuickSearch, 0, 0);
            pnlSearch.Controls.Add(btnQuickFind, 1, 0);
            pnlSearch.Controls.Add(btnOpenPatientManager, 2, 0);

            // Họ và tên
            var lblName = UiHelper.CreateLabel(
                "Họ và tên bệnh nhân (*):",
                font: UiHelper.FontLabelBold,
                margin: UiHelper.Pad(0, 0, 0, 4));
            _txtFullName = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };

            // SĐT & Giới tính 
            var pnlPhoneGender = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };
            pnlPhoneGender.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            pnlPhoneGender.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            pnlPhoneGender.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlPhoneGender.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtPhone = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 0, 10, 0)
            };
            _cboGender = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiHelper.FontRegular,
                Margin = Padding.Empty
            };
            _cboGender.Items.AddRange(new object[] { "Nam", "Nữ" });
            _cboGender.SelectedIndex = -1; 

            pnlPhoneGender.Controls.Add(UiHelper.CreateLabel("Số điện thoại (*):", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 4)), 0, 0);
            pnlPhoneGender.Controls.Add(UiHelper.CreateLabel("Giới tính:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 4)), 1, 0);
            pnlPhoneGender.Controls.Add(_txtPhone, 0, 1);
            pnlPhoneGender.Controls.Add(_cboGender, 1, 1);

            // Năm sinh & Ngày tiếp nhận
            var pnlYearDate = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };
            pnlYearDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlYearDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlYearDate.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlYearDate.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _numBirthYear = new NumericUpDown
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 1900,
                Maximum = DateTime.Now.Year,
                Value = 1990,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 0, 10, 0)
            };
            _numBirthYear.Text = string.Empty; 

            _dtpRegDate = new DateTimePicker
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Now,
                Enabled = false,
                Font = UiHelper.FontRegular,
                Margin = Padding.Empty
            };

            pnlYearDate.Controls.Add(UiHelper.CreateLabel("Năm sinh:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 4)), 0, 0);
            pnlYearDate.Controls.Add(UiHelper.CreateLabel("Ngày tiếp nhận:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 4)), 1, 0);
            pnlYearDate.Controls.Add(_numBirthYear, 0, 1);
            pnlYearDate.Controls.Add(_dtpRegDate, 1, 1);

            // Địa chỉ
            var lblAddress = UiHelper.CreateLabel(
                text:"Địa chỉ liên hệ:",
                font: UiHelper.FontLabelBold,
                margin: UiHelper.Pad(0, 0, 0, 4));
            _txtAddress = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 0, 0, 14)
            };

            // Hàng nút bấm
            var pnlButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = UiHelper.Pad(0, 2, 0, 0)
            };
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            pnlButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var btnIssue = UiHelper.CreateButton("LƯU & CẤP SỐ THỨ TỰ KHÁM", IconChar.Ticket, UiHelper.SuccessGreen, minWidth: 180, height: 40, baseIconSize: 18);
            btnIssue.Dock = DockStyle.Fill;
            btnIssue.Margin = UiHelper.Pad(0, 0, 8, 0);
            btnIssue.Click += BtnIssue_Click;

            var btnReset = UiHelper.CreateButton("Làm mới", IconChar.RotateLeft, UiHelper.SecondaryGray, minWidth: 95, height: 40, baseIconSize: 16);
            btnReset.Dock = DockStyle.Fill;
            btnReset.Margin = Padding.Empty;
            btnReset.Click += (s, e) => ResetForm();

            pnlButtons.Controls.Add(btnIssue, 0, 0);
            pnlButtons.Controls.Add(btnReset, 1, 0);

            formLayout.Controls.Add(lblSearch, 0, 0);
            formLayout.Controls.Add(pnlSearch, 0, 1);
            formLayout.Controls.Add(lblName, 0, 2);
            formLayout.Controls.Add(_txtFullName, 0, 3);
            formLayout.Controls.Add(pnlPhoneGender, 0, 4);
            formLayout.Controls.Add(pnlYearDate, 0, 5);
            formLayout.Controls.Add(lblAddress, 0, 6);
            formLayout.Controls.Add(_txtAddress, 0, 7);
            formLayout.Controls.Add(pnlButtons, 0, 8);

            grpPatientInput.Controls.Add(formLayout);

            // Thẻ phiếu số thứ tự vừa cấp 
            var pnlTicket = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(235, 245, 251),
                Padding = UiHelper.Pad(14),
                Margin = UiHelper.Pad(0, 12, 0, 0)
            };
            pnlTicket.Paint += (s, e) =>
            {
                using var pen = new Pen(UiHelper.PrimaryBlue, Math.Max(1, UiHelper.S(2))) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(pen, 1, 1, pnlTicket.Width - 3, pnlTicket.Height - 3);
            };

            var ticketOuter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            ticketOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ticketOuter.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var ticketLayout = new TableLayoutPanel
            {
                Anchor = AnchorStyles.None,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            ticketLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 4; i++)
            {
                ticketLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var lblTicketTitle = UiHelper.CreateLabel(
                "PHIẾU SỐ THỨ TỰ VỪA CẤP TRONG NGÀY",
                font: UiHelper.FontBold,
                foreColor: Color.FromArgb(27, 79, 114),
                margin: UiHelper.Pad(0, 0, 0, 6));
            lblTicketTitle.Anchor = AnchorStyles.None;

            _lblTicketNumber = UiHelper.CreateLabel(
                "STT: --",
                font: new Font("Segoe UI", 32F, FontStyle.Bold),
                foreColor: UiHelper.DangerRed,
                margin: UiHelper.Pad(0, 2, 0, 6));
            _lblTicketNumber.Anchor = AnchorStyles.None;

            _lblTicketPatient = UiHelper.CreateLabel(
                "Bệnh nhân: (Chưa có lượt cấp)",
                font: UiHelper.FontSection,
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 4));
            _lblTicketPatient.Anchor = AnchorStyles.None;

            _lblTicketStatus = UiHelper.CreateLabel(
                "Trạng thái: Sẵn sàng phục vụ",
                font: new Font("Segoe UI", 9F, FontStyle.Italic),
                foreColor: UiHelper.SecondaryGray,
                margin: Padding.Empty);
            _lblTicketStatus.Anchor = AnchorStyles.None;

            ticketLayout.Controls.Add(lblTicketTitle, 0, 0);
            ticketLayout.Controls.Add(_lblTicketNumber, 0, 1);
            ticketLayout.Controls.Add(_lblTicketPatient, 0, 2);
            ticketLayout.Controls.Add(_lblTicketStatus, 0, 3);

            ticketOuter.Controls.Add(ticketLayout, 0, 0);
            pnlTicket.Controls.Add(ticketOuter);

            leftLayout.Controls.Add(grpPatientInput, 0, 0);
            leftLayout.Controls.Add(pnlTicket, 0, 1);

            // Cột phải danh sách hàng đợi
            var grpQueue = UiHelper.CreateGroupBox(
                title: "2. DANH SÁCH HÀNG ĐỢI KHÁM HÔM NAY",
                titleColor: UiHelper.PrimaryBlue,
                padding: UiHelper.Pad(12));

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var toolFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };

            var btnOpenTv = UiHelper.CreateButton(
                text: "Mở TV Phòng Chờ",
                IconChar.Tv,
                backColor: UiHelper.WarningOrange,
                minWidth: 190,
                height: 36,
                baseIconSize: 17
            );
            btnOpenTv.Margin = UiHelper.Pad(0, 0, 8, 4);
            btnOpenTv.Click += BtnOpenTv_Click;

            var btnOpenReport = UiHelper.CreateButton(
                text: "Thống Kê BN",
                IconChar.ChartColumn,
                backColor: UiHelper.PrimaryBlue,
                minWidth: 170,
                height: 36,
                baseIconSize: 16
            );
            btnOpenReport.Margin = UiHelper.Pad(0, 0, 8, 4);
            btnOpenReport.Click += BtnOpenReport_Click;
            

            toolFlow.Controls.Add(btnOpenTv);
            toolFlow.Controls.Add(btnOpenReport);
            
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _refreshTimer.Tick += (s, e) =>
            {
                if (this.Visible) LoadTodayQueue();
            };
            _refreshTimer.Start();
            this.Disposed += (s, e) =>
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            };
            
            _dgvTodayQueue = UiHelper.CreateStandardGrid(baseRowHeight: 42, baseHeaderHeight: 38);
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueId", Visible = false });
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "STT", FillWeight = 45 });
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Họ và Tên Bệnh Nhân", FillWeight = 140 });
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Số Điện Thoại", FillWeight = 90 });
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Giờ Tiếp Nhận", FillWeight = 85 });
            _dgvTodayQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", FillWeight = 120 });

            _dgvTodayQueue.Columns["QueueNumber"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvTodayQueue.Columns["QueueNumber"]!.DefaultCellStyle.Font = UiHelper.FontBold;
            _dgvTodayQueue.Columns["Time"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvTodayQueue.Columns["Status"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            _dgvTodayQueue.DefaultCellStyle.ForeColor = Color.Black;
            _dgvTodayQueue.RowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvTodayQueue.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvTodayQueue.DefaultCellStyle.SelectionForeColor = Color.Black;

            // Tô màu trạng thái
            _dgvTodayQueue.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (_dgvTodayQueue.Columns[e.ColumnIndex].Name != "Status") return;

                e.PaintBackground(e.CellBounds, true);

                string statusText = e.FormattedValue?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(statusText))
                {
                    Color badgeBg, badgeBorder, badgeText;
                    if (statusText.Contains("Đang khám") || statusText.Contains("Examining"))
                    {
                        badgeBg = Color.FromArgb(235, 245, 251);     
                        badgeBorder = Color.FromArgb(93, 173, 226);  
                        badgeText = Color.FromArgb(27, 79, 114);     
                    }
                    else if (statusText.Contains("Đang chờ") || statusText.Contains("Waiting"))
                    {
                        badgeBg = Color.FromArgb(254, 249, 231);     
                        badgeBorder = Color.FromArgb(244, 208, 63);  
                        badgeText = Color.FromArgb(183, 149, 11);    
                    }
                    else if (statusText.Contains("Chờ thu phí") || statusText.Contains("PendingPayment"))
                    {
                        badgeBg = Color.FromArgb(253, 242, 233);     
                        badgeBorder = Color.FromArgb(245, 176, 65);  
                        badgeText = Color.FromArgb(186, 74, 0);      
                    }
                    else if (statusText.Contains("Đã hoàn tất") || statusText.Contains("Completed"))
                    {
                        badgeBg = Color.FromArgb(234, 250, 241);     
                        badgeBorder = Color.FromArgb(88, 214, 141);  
                        badgeText = Color.FromArgb(30, 132, 73);     
                    }
                    else
                    {
                        badgeBg = Color.FromArgb(242, 244, 244);     
                        badgeBorder = Color.FromArgb(204, 209, 209); 
                        badgeText = Color.FromArgb(97, 106, 107);    
                    }

                    int padX = UiHelper.S(8);
                    int padY = UiHelper.S(5);
                    var badgeRect = new Rectangle(
                        e.CellBounds.X + padX,
                        e.CellBounds.Y + padY,
                        Math.Max(10, e.CellBounds.Width - padX * 2 - 1),
                        Math.Max(10, e.CellBounds.Height - padY * 2 - 1));

                    var oldSmoothing = e.Graphics!.SmoothingMode;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var path = CreateRoundedRect(badgeRect, UiHelper.S(4)))
                    using (var brush = new SolidBrush(badgeBg))
                    using (var pen = new Pen(badgeBorder, 1F))
                    {
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }

                    e.Graphics.SmoothingMode = oldSmoothing;

                    TextRenderer.DrawText(
                        e.Graphics,
                        statusText,
                        UiHelper.FontSmallBold,
                        badgeRect,
                        badgeText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                }

                e.Handled = true;
            };

            rightLayout.Controls.Add(toolFlow, 0, 0);
            rightLayout.Controls.Add(_dgvTodayQueue, 0, 1);
            grpQueue.Controls.Add(rightLayout);

            mainLayout.Controls.Add(leftLayout, 0, 0);
            mainLayout.Controls.Add(grpQueue, 1, 0);
            this.Controls.Add(mainLayout);
        }
        
        public void LoadTodayQueue()
        {
            try
            {
                var list = _controller.GetTodayAllQueues();
                _dgvTodayQueue.Rows.Clear();

                foreach (var q in list)
                {
                    string statusText = q.Status switch
                    {
                        "Examining" => "Đang khám (Examining)",
                        "Waiting" => "Đang chờ (Waiting)",
                        "PendingPayment" => "Chờ thu phí (PendingPayment)",
                        "Completed" => "Đã hoàn tất (Completed)",
                        "Cancelled" => "Đã hủy (Cancelled)",
                        _ => q.Status
                    };

                    _dgvTodayQueue.Rows.Add(
                        q.Id,
                        $"{q.QueueNumber:D2}",
                        q.PatientName,
                        q.Phone,
                        q.CreatedAt.ToString("HH:mm:ss"),
                        statusText
                    );
                }
                _dgvTodayQueue.ClearSelection();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách hàng đợi hôm nay: " + ex.Message,
                    "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTvMonitors()
        {
            foreach (var tvForm in Application.OpenForms.OfType<FormQueueMonitor>())
            {
                tvForm.LoadQueueData();
            }
        }

        private void BtnOpenTv_Click(object? sender, EventArgs e)
        {
            var existingTv = Application.OpenForms.OfType<FormQueueMonitor>().FirstOrDefault();
            if (existingTv != null)
            {
                existingTv.LoadQueueData();
                if (existingTv.WindowState == FormWindowState.Minimized)
                {
                    existingTv.WindowState = FormWindowState.Maximized;
                }
                existingTv.BringToFront();
            }
            else
            {
                var f = new FormQueueMonitor();
                f.Show();
            }
        }

        private void BtnOpenReport_Click(object? sender, EventArgs e)
        {
            using var reportForm = new FormPatientReport
            {
                Dock = DockStyle.None,
                FormBorderStyle = FormBorderStyle.Sizable,
                StartPosition = FormStartPosition.CenterParent,
                Size = UiHelper.SSize(1180, 680),
                MinimumSize = UiHelper.SSize(980, 580),
                WindowState = FormWindowState.Maximized
            };
            reportForm.ShowDialog(this);
        }

        private void BtnOpenPatientManager_Click(object? sender, EventArgs e)
        {
            using var dlg = new FormPatientManager();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedPatient != null)
            {
                FillPatient(dlg.SelectedPatient);
            }
        }

        private void BtnIssue_Click(object? sender, EventArgs e)
        {
            string name = _txtFullName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Vui lòng nhập họ và tên bệnh nhân!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFullName.Focus();
                return;
            }

            int birthYear = 0;
            if (!string.IsNullOrWhiteSpace(_numBirthYear.Text))
            {
                if (!int.TryParse(_numBirthYear.Text.Trim(), out birthYear) || birthYear < 1900 || birthYear > DateTime.Now.Year)
                {
                    MessageBox.Show($"Năm sinh không hợp lệ (phải từ 1900 đến {DateTime.Now.Year})!",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _numBirthYear.Focus();
                    return;
                }
            }

            string? gender = _cboGender.SelectedIndex >= 0 ? _cboGender.SelectedItem?.ToString() : null;

            try
            {
                // Thêm bệnh nhân mới qua Controller
                int pId = _selectedPatientId ?? _controller.AddPatient(new Patient
                {
                    FullName = name,
                    Phone = _txtPhone.Text.Trim(),
                    Gender = gender,
                    BirthYear = birthYear,
                    Address = _txtAddress.Text.Trim()
                });

                // Cấp số thứ tự khám trong ngày qua Controller
                int nextNumber = _controller.GenerateQueueNumber(pId);

                // Hiển thị thông tin lên thẻ phiếu STT
                _lblTicketNumber.Text = $"STT: {nextNumber:D2}";
                _lblTicketPatient.Text = $"Bệnh nhân: {name}";
                _lblTicketStatus.Text = "Trạng thái: Đang chờ (Waiting) - Đã đẩy lên TV Phòng chờ";

                // Cập nhật bảng hàng đợi và đẩy ngay lên màn hình TV phòng chờ
                LoadTodayQueue();
                RefreshTvMonitors();

                MessageBox.Show($"Cấp STT {nextNumber:D2} cho bệnh nhân [{name}] thành công!",
                    "Cấp số thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi cấp số", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void QuickSearchPatient()
        {
            string kw = _txtQuickSearch.Text.Trim();
            if (string.IsNullOrEmpty(kw))
            {
                MessageBox.Show("Vui lòng nhập tên hoặc số điện thoại để tìm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = _controller.SearchPatients(kw);
            if (results.Count > 0)
            {
                var patient = results.First();
                FillPatient(patient);
                MessageBox.Show($"Đã tìm thấy hồ sơ của bệnh nhân: {patient.FullName}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Không tìm thấy bệnh nhân nào có thông tin '{kw}'. Bạn có thể nhập mới thông tin!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FillPatient(Patient p)
        {
            _selectedPatientId = p.Id;
            _txtFullName.Text = p.FullName;
            _txtPhone.Text = p.Phone ?? string.Empty;

            if (p.Gender == "Nam" || p.Gender == "Nữ")
            {
                _cboGender.SelectedItem = p.Gender;
            }
            else
            {
                _cboGender.SelectedIndex = -1;
            }

            if (p.BirthYear >= 1900 && p.BirthYear <= DateTime.Now.Year)
            {
                _numBirthYear.Value = p.BirthYear;
                _numBirthYear.Text = p.BirthYear.ToString();
            }
            else
            {
                _numBirthYear.Text = string.Empty;
            }

            _txtAddress.Text = p.Address ?? string.Empty;
        }

        private void ResetForm()
        {
            _selectedPatientId = null;
            _txtQuickSearch.Clear();
            _txtFullName.Clear();
            _txtPhone.Clear();
            _cboGender.SelectedIndex = -1;
            _numBirthYear.Text = string.Empty;
            _txtAddress.Clear();
            _txtFullName.Focus();
        }
        
        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}