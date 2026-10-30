using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormMain : Form
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
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "Hệ Thống Quản Lý Phòng Khám & Đơn Thuốc";
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1060, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // 1. Header Panel: Dùng IconPictureBox (Only Icon) + Label tiêu đề chuẩn theo yêu cầu
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(41, 128, 185),
                Padding = new Padding(16, 0, 16, 0)
            };

            var headerFrameLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerFrameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerFrameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headerPart = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            headerPart.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPart.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPart.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _iconHeader = new IconPictureBox
            {
                IconChar = IconChar.Hospital,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                IconSize = 48,
                Size = new Size(48, 48),
                Anchor = AnchorStyles.Left,
                // Bù 5px phía trên để tâm thị giác của Icon khớp với thân chữ in hoa có dấu tiếng Việt
                Margin = new Padding(0, 5, 12, 0),
                BackColor = Color.Transparent
            };

            _lblHeader = new Label
            {
                Text = "HỆ THỐNG QUẢN LÝ PHÒNG KHÁM && ĐƠN THUỐC",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };

            headerPart.Controls.Add(_iconHeader, 0, 0);
            headerPart.Controls.Add(_lblHeader, 1, 0);
            headerFrameLayout.Controls.Add(headerPart, 0, 0);
            _panelHeader.Controls.Add(headerFrameLayout);

            // 2. Sidebar Menu: Dùng trực tiếp IconButton, tự động đảm bảo độ rộng đủ lớn ở mọi mức DPI (100% - 175%)
            _panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                BackColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(12, 16, 12, 16)
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

            _btnReception = CreateMenuButton("1. Lễ Tân && Bệnh Nhân", IconChar.UserInjured);
            _btnQueueMonitor = CreateMenuButton("1. Màn Hình Chờ (TV)", IconChar.Tv);
            _btnDoctor = CreateMenuButton("2. Bác Sĩ && Kê Đơn", IconChar.UserMd);
            _btnPharmacy = CreateMenuButton("3. Kho && Thu Ngân", IconChar.Pills);

            _menuLayout.Controls.Add(_btnReception, 0, 0);
            _menuLayout.Controls.Add(_btnQueueMonitor, 0, 1);
            _menuLayout.Controls.Add(_btnDoctor, 0, 2);
            _menuLayout.Controls.Add(_btnPharmacy, 0, 3);
            _panelMenu.Controls.Add(_menuLayout);

            // 3. Content Panel (Nơi nhúng các Form phân hệ)
            _panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12)
            };

            // 4. Status Bar
            _statusStrip = new StatusStrip
            {
                BackColor = Color.White,
                SizingGrip = true
            };
            _lblStatus = new ToolStripStatusLabel("Hệ thống sẵn sàng. Chọn một phân hệ để bắt đầu.")
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(44, 62, 80)
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

        private static IconButton CreateMenuButton(string text, IconChar icon)
        {
            var btn = new IconButton
            {
                Text = "  " + text,
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                IconSize = 26,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Height = 54,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 73, 94),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoEllipsis = false,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);

            // Căn chỉnh lại tâm thực tế của Bitmap Icon (do FontAwesome.Sharp vẽ lệch lên đỉnh ảnh 4px)
            AlignIconBitmapVertically(btn, 4);

            return btn;
        }

        /// <summary>
        /// Dịch nhẹ phần hình vẽ trong Bitmap của IconButton xuống một khoảng offsetY (px)
        /// để bù độ lệch giữa Bitmap của FontAwesome.Sharp (vẽ sát mép trên Y=0) và baseline của font Segoe UI tiếng Việt.
        /// </summary>
        private static void AlignIconBitmapVertically(IconButton btn, int offsetY)
        {
            if (btn.Image is not Bitmap originalBmp)
            {
                return;
            }

            var alignedBmp = new Bitmap(originalBmp.Width, originalBmp.Height + offsetY);
            using (var g = Graphics.FromImage(alignedBmp))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(originalBmp, 0, offsetY, originalBmp.Width, originalBmp.Height);
            }

            btn.Image = alignedBmp;
        }

        private void SetActiveMenuButton(IconButton selectedButton)
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = Color.FromArgb(52, 73, 94);
                _activeButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }

            _activeButton = selectedButton;
            _activeButton.BackColor = Color.FromArgb(41, 128, 185);
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
