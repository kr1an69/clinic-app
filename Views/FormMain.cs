using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Helpers;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormMain : BaseForm
    {
        private Panel _panelHeader = null!;
        private IconPictureBox _iconHeader = null!;
        private Label _lblHeader = null!;
        private Panel _panelMenu = null!;
        private TableLayoutPanel _menuLayout = null!;
        private IconButton _btnReception = null!;
        private IconButton _btnQueueMonitor = null!;
        private IconButton _btnDoctor = null!;
        private IconButton _btnPharmacy = null!;
        private Panel _panelContent = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _lblStatus = null!;
        private IconButton? _activeButton;

        public FormMain()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ Thống Quản Lý Phòng Khám & Đơn Thuốc";
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.BackColor = UiHelper.BgMain;

            // 1. Header Panel: Dùng IconPictureBox (Only Icon) + Label tiêu đề chuẩn theo yêu cầu
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = UiHelper.S(64),
                BackColor = UiHelper.PrimaryBlue,
                Padding = UiHelper.Pad(16, 0, 16, 0)
            };

            var headerFrameLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerFrameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerFrameLayout.RowStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var headerPart = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            headerPart.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPart.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPart.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _iconHeader = new IconPictureBox
            {
                IconChar = IconChar.Hospital,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                IconSize = UiHelper.S(40),
                Size = UiHelper.SSize(40, 40),
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 12, 0),
                BackColor = Color.Transparent
            };

            _lblHeader = new Label
            {
                Text = "HỆ THỐNG QUẢN LÝ PHÒNG KHÁM & ĐƠN THUỐC",
                AutoSize = true,
                UseMnemonic = false,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left,
                Margin = Padding.Empty
            };

            headerPart.Controls.Add(_iconHeader, 0, 0);
            headerPart.Controls.Add(_lblHeader, 1, 0);
            headerFrameLayout.Controls.Add(headerPart, 0, 0);
            _panelHeader.Controls.Add(headerFrameLayout);

            // 2. Sidebar Menu: Độ rộng, chiều cao nút và Icon đều tự động nhân theo tỷ lệ DPI
            _panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = UiHelper.S(255),
                BackColor = UiHelper.SidebarBg,
                Padding = UiHelper.Pad(12, 16, 12, 16)
            };

            _menuLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            _menuLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++)
            {
                _menuLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            _btnReception = UiHelper.CreateSidebarButton("1. Lễ Tân & Bệnh Nhân", IconChar.UserInjured);
            _btnQueueMonitor = UiHelper.CreateSidebarButton("1. Màn Hình Chờ (TV)", IconChar.Tv);
            _btnDoctor = UiHelper.CreateSidebarButton("2. Bác Sĩ & Kê Đơn", IconChar.UserMd);
            _btnPharmacy = UiHelper.CreateSidebarButton("3. Kho & Thu Ngân", IconChar.Pills);

            _menuLayout.Controls.Add(_btnReception, 0, 0);
            _menuLayout.Controls.Add(_btnQueueMonitor, 0, 1);
            _menuLayout.Controls.Add(_btnDoctor, 0, 2);
            _menuLayout.Controls.Add(_btnPharmacy, 0, 3);
            _panelMenu.Controls.Add(_menuLayout);

            // 3. Content Panel (Nơi nhúng các Form phân hệ)
            _panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiHelper.BgMain,
                Padding = UiHelper.Pad(10)
            };

            // 4. Status Bar
            _statusStrip = new StatusStrip
            {
                BackColor = Color.White,
                SizingGrip = true,
                Padding = UiHelper.Pad(6, 2, 6, 2)
            };
            _lblStatus = new ToolStripStatusLabel("Hệ thống sẵn sàng. Chọn một phân hệ để bắt đầu.")
            {
                Font = UiHelper.FontRegular,
                ForeColor = UiHelper.DarkText
            };
            _statusStrip.Items.Add(_lblStatus);

            // Thứ tự Add Controls chuẩn trong WinForms
            this.Controls.Add(_panelContent);
            this.Controls.Add(_panelMenu);
            this.Controls.Add(_panelHeader);
            this.Controls.Add(_statusStrip);

            // Events điều hướng
            _btnReception.Click += (_, _) =>
            {
                SetActiveMenuButton(_btnReception);
                LoadForm(new FormReception(), "Phân hệ 1: Tiếp Nhận, Xếp Hàng && Thống Kê Bệnh Nhân (Thành viên 1)");
            };

            _btnQueueMonitor.Click += (_, _) =>
            {
                var monitorForm = new FormQueueMonitor();
                monitorForm.Show();
            };

            _btnDoctor.Click += (_, _) =>
            {
                SetActiveMenuButton(_btnDoctor);
                LoadForm(new FormDoctor(), "Phân hệ 2: Bác Sĩ Khám && Kê Đơn Thuốc (Thành viên 2)");
            };

            _btnPharmacy.Click += (_, _) =>
            {
                SetActiveMenuButton(_btnPharmacy);
                LoadForm(new FormPharmacy(), "Phân hệ 3: Thu Phí, Xuất Thuốc, In Ấn && Báo Cáo Kho (Thành viên 3)");
            };

            // Mặc định mở tab Lễ tân khi khởi động
            SetActiveMenuButton(_btnReception);
            LoadForm(new FormReception(), "Phân hệ 1: Tiếp Nhận, Xếp Hàng && Thống Kê Bệnh Nhân (Thành viên 1)");
        }

        private void SetActiveMenuButton(IconButton selectedButton)
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = UiHelper.SidebarBtnBg;
                _activeButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }

            _activeButton = selectedButton;
            _activeButton.BackColor = UiHelper.PrimaryBlue;
            _activeButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        private void LoadForm(Form childForm, string statusText)
        {
            foreach (Control ctrl in _panelContent.Controls)
            {
                ctrl.Dispose();
            }
            _panelContent.Controls.Clear();

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            _panelContent.Controls.Add(childForm);
            childForm.Show();
            _lblStatus.Text = statusText;
        }
    }
}
