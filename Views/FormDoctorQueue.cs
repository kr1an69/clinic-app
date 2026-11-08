using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    /// <summary>
    /// SCREEN #06: DANH SÁCH CHỜ KHÁM & ĐIỀU PHỐI GỌI BỆNH NHÂN (Thành viên 2 - Đức)
    /// </summary>
    public class FormDoctorQueue : Form
    {
        private readonly DoctorController _controller = new();
        private readonly Action<QueueItem>? _onOpenExamScreen;
        private readonly Action<int>? _onOpenHistoryScreen;

        // Các thành phần giao diện
        private TableLayoutPanel _mainLayout = null!;
        private GroupBox _grpWaitingList = null!;
        private GroupBox _grpCallAction = null!;
        private DataGridView _dgvDoctorWaiting = null!;
        private IconButton _btnRefresh = null!;
        private IconButton _btnCallPatient = null!;
        private IconButton _btnViewHistory = null!;
        private IconButton _btnCancelQueue = null!;

        // Thẻ hiển thị xem trước thông tin bệnh nhân
        private Panel _patientCard = null!;
        private Label _lblQueueNumberBig = null!;
        private Label _lblPatientName = null!;
        private Label _lblPatientMeta = null!;

        public FormDoctorQueue(Action<QueueItem>? onOpenExamScreen = null, Action<int>? onOpenHistoryScreen = null)
        {
            _onOpenExamScreen = onOpenExamScreen;
            _onOpenHistoryScreen = onOpenHistoryScreen;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Hàng Đợi Khám Bệnh - Bác Sĩ";
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Dock = DockStyle.Fill;

            // 1. Khởi tạo Banner tiêu đề
            var bannerPanel = BuildHeaderBanner();

            // 2. Khởi tạo Layout chính 2 cột (60% danh sách chờ, 40% điều phối)
            _mainLayout = BuildMainLayout();

            // 3. Khởi tạo cụm bảng danh sách bên trái
            _grpWaitingList = BuildWaitingListGroup();

            // 4. Khởi tạo cụm thẻ gọi khám bên phải
            _grpCallAction = BuildCallActionGroup();

            _mainLayout.Controls.Add(_grpWaitingList, 0, 0);
            _mainLayout.Controls.Add(_grpCallAction, 1, 0);

            this.Controls.Add(_mainLayout);
            this.Controls.Add(bannerPanel);
        }

        private Panel BuildHeaderBanner()
        {
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(234, 242, 248),
                Padding = new Padding(16, 8, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "SCREEN #06: DANH SÁCH CHỜ KHÁM & ĐIỀU PHỐI GỌI BỆNH NHÂN (BS)",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Bác sĩ chọn số thứ tự -> Bấm 'Gọi Vào Khám' để chuyển trạng thái sang Examining và mở Screen #07",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.SecondaryGray,
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            var borderBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(169, 204, 227)
            };

            banner.Controls.Add(lblSubtitle);
            banner.Controls.Add(lblTitle);
            banner.Controls.Add(borderBottom);
            return banner;
        }

        private TableLayoutPanel BuildMainLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private GroupBox BuildWaitingListGroup()
        {
            var group = new GroupBox
            {
                Text = " DANH SÁCH BỆNH NHÂN ĐANG CHỜ KHÁM HÔM NAY ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 2, 0, 4)
            };

            _btnRefresh = UiHelper.CreateButton(" Làm mới", IconChar.Rotate, Color.FromArgb(52, 73, 94), minWidth: 110, height: 34);
            topPanel.Controls.Add(_btnRefresh);

            _dgvDoctorWaiting = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = UiHelper.DarkText
            };

            _dgvDoctorWaiting.EnableHeadersVisualStyles = false;
            _dgvDoctorWaiting.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(223, 230, 233);
            _dgvDoctorWaiting.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(45, 52, 54);
            _dgvDoctorWaiting.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _dgvDoctorWaiting.ColumnHeadersHeight = 38;
            _dgvDoctorWaiting.RowTemplate.Height = 36;
            _dgvDoctorWaiting.DefaultCellStyle.SelectionBackColor = Color.FromArgb(174, 214, 241);
            _dgvDoctorWaiting.DefaultCellStyle.SelectionForeColor = Color.FromArgb(27, 79, 114);

            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "STT", FillWeight = 18 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Họ và Tên Bệnh Nhân", FillWeight = 42 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Số Điện Thoại", FillWeight = 25 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", FillWeight = 25 });

            layout.Controls.Add(topPanel, 0, 0);
            layout.Controls.Add(_dgvDoctorWaiting, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildCallActionGroup()
        {
            var group = new GroupBox
            {
                Text = " ĐIỀU PHỐI GỌI KHÁM ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Padding = new Padding(12)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            _patientCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(235, 245, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var lblCardHeader = new Label
            {
                Text = "BỆNH NHÂN ĐANG CHỌN",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 116, 166),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20
            };

            _lblQueueNumberBig = new Label
            {
                Text = "STT: --",
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 52
            };

            _lblPatientName = new Label
            {
                Text = "Chưa chọn bệnh nhân",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 28
            };

            _lblPatientMeta = new Label
            {
                Text = "Chọn một bệnh nhân trong danh sách bên trái",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.SecondaryGray,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 22
            };

            _patientCard.Controls.Add(_lblPatientMeta);
            _patientCard.Controls.Add(_lblPatientName);
            _patientCard.Controls.Add(_lblQueueNumberBig);
            _patientCard.Controls.Add(lblCardHeader);

            _btnCallPatient = UiHelper.CreateButton("  GỌI VÀO KHÁM & MỞ PHIẾU KHÁM (#07)", IconChar.Stethoscope, UiHelper.SuccessGreen, height: 44);
            _btnCallPatient.Dock = DockStyle.Fill;
            _btnCallPatient.TextAlign = ContentAlignment.MiddleLeft;
            _btnCallPatient.Padding = new Padding(12, 0, 0, 0);
            _btnCallPatient.Margin = new Padding(0, 0, 0, 8);

            _btnViewHistory = UiHelper.CreateButton("  Xem tiền sử khám bệnh của BN này (#08)", IconChar.ClockRotateLeft, UiHelper.PrimaryBlue, height: 40);
            _btnViewHistory.Dock = DockStyle.Fill;
            _btnViewHistory.TextAlign = ContentAlignment.MiddleLeft;
            _btnViewHistory.Padding = new Padding(12, 0, 0, 0);
            _btnViewHistory.Margin = new Padding(0, 0, 0, 8);

            _btnCancelQueue = UiHelper.CreateButton("  Đánh dấu Vắng mặt / Hủy lượt (Cancelled)", IconChar.UserSlash, UiHelper.DangerRed, height: 40);
            _btnCancelQueue.Dock = DockStyle.Fill;
            _btnCancelQueue.TextAlign = ContentAlignment.MiddleLeft;
            _btnCancelQueue.Padding = new Padding(12, 0, 0, 0);
            _btnCancelQueue.Margin = new Padding(0, 0, 0, 8);

            var lblTip = new Label
            {
                Text = "💡 Mẹo: Bấm 'Gọi Vào Khám' sẽ phát tín hiệu âm thanh và chuyển trạng thái đồng bộ sang TV phòng chờ.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = UiHelper.SecondaryGray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(_patientCard, 0, 0);
            layout.Controls.Add(_btnCallPatient, 0, 1);
            layout.Controls.Add(_btnViewHistory, 0, 2);
            layout.Controls.Add(_btnCancelQueue, 0, 3);
            layout.Controls.Add(new Panel(), 0, 4);
            layout.Controls.Add(lblTip, 0, 5);

            group.Controls.Add(layout);
            return group;
        }
    }
}
