using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using ClinicManagement.Controllers;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Views
{
    public class FormLogin : BaseForm
    {
        private readonly ReceptionController _controller = new();

        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private CheckBox _chkShowPassword = null!;
        private IconButton _btnLogin = null!;
        private Label _lblError = null!;

        public User? LoggedInUser { get; private set; }

        public FormLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng Nhập & Phân Quyền";
            this.ConfigureAsFixedDialog(baseWidth: 500, baseHeight: 640);
            this.BackColor = Color.FromArgb(238, 242, 246);
            
            // Thẻ trung tâm
            var cardBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = UiHelper.Pad(28, 20, 28, 20)
            };

            cardBox.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, cardBox.ClientRectangle,
                    Color.FromArgb(209, 213, 219), ButtonBorderStyle.Solid);
            };
            
            // Bố cục
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 11,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            for (int i = 0; i < 11; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            
            // Logo và tiêu đề
            var iconBox = new IconPictureBox
            {
                IconChar = IconChar.HospitalUser,
                IconFont = IconFont.Auto,
                IconColor = UiHelper.PrimaryBlue,
                IconSize = UiHelper.S(50),
                Size = UiHelper.SSize(50, 50),
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = UiHelper.Pad(0, 0, 0, 4)
            };

            var lblTitle = UiHelper.CreateLabel(
                text: "ĐĂNG NHẬP PHÒNG KHÁM",
                font: new Font("Segoe UI", 14F, FontStyle.Bold),
                foreColor: UiHelper.PrimaryBlue,
                margin: UiHelper.Pad(0, 0, 0, 2)
            );
            lblTitle.Anchor = AnchorStyles.None;

            var lblSubTitle = UiHelper.CreateLabel(
                text: "Hệ thống quản lý phòng khám và đơn thuốc",
                font: UiHelper.FontRegular,
                foreColor: UiHelper.SecondaryGray,
                margin: UiHelper.Pad(0, 0, 0, 16)
            );
            lblSubTitle.Anchor = AnchorStyles.None;
            
            // Tài khoản
            var lblUser = UiHelper.CreateLabel(
                text: "Tên đăng nhập:",
                font: UiHelper.FontBold,
                margin: UiHelper.Pad(0, 0, 0, 3)
            );
            _txtUsername = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10.5F),
                Margin = UiHelper.Pad(0, 0, 0, 10),
                Text = "admin"
            };

            var lblPass = UiHelper.CreateLabel(
                text: "Mật khẩu:",
                font: UiHelper.FontBold,
                margin: UiHelper.Pad(0, 0, 0, 3)
            );
            _txtPassword = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10.5F),
                UseSystemPasswordChar = true,
                Margin = UiHelper.Pad(0, 0, 0, 4),
                Text = "admin123"
            };

            _chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = UiHelper.FontSmall,
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };
            _chkShowPassword.CheckedChanged += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;
            };
            
            // Thông báo lỗi và nút đăng nhập
            _lblError = UiHelper.CreateLabel(
                text: string.Empty,
                font: new Font("Segoe UI", 9F, FontStyle.Italic),
                foreColor: UiHelper.DangerRed,
                margin: UiHelper.Pad(0, 0, 0, 6)
            );
            _lblError.Anchor = AnchorStyles.None;

            _btnLogin = UiHelper.CreateButton(
                text: "ĐĂNG NHẬP HỆ THỐNG",
                IconChar.RightToBracket,
                backColor: UiHelper.PrimaryBlue,
                minWidth: 200,
                height: 42
            );
            _btnLogin.Dock = DockStyle.Fill;
            _btnLogin.Margin = Padding.Empty;
            _btnLogin.Click += (s, e) => PerformLogin();
            this.AcceptButton = _btnLogin;
            
            // Chọn tài khoản
            var grpAccounts = UiHelper.CreateGroupBox(
                title: "Chọn tài khoản:",
                titleColor: UiHelper.SecondaryGray,
                margin: UiHelper.Pad(0, 14, 0, 0)
            );

            var accountGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
            };
            accountGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            accountGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            accountGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            accountGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            
            accountGrid.Controls.Add(CreateAccountBtn("Quản trị viên", "admin", "admin123", UiHelper.DarkText), 0, 0);
            accountGrid.Controls.Add(CreateAccountBtn("Lễ tân", "letan", "123", UiHelper.SecondaryGray), 1, 0);
            accountGrid.Controls.Add(CreateAccountBtn("Bác sĩ", "bacsi", "123", UiHelper.WarningOrange), 0, 1);
            accountGrid.Controls.Add(CreateAccountBtn("Dược sĩ", "duocsi", "123", UiHelper.SuccessGreen), 1, 1);
            grpAccounts.Controls.Add(accountGrid);
            
            // Thêm vào bảng
            layout.Controls.Add(iconBox, 0, 0);
            layout.Controls.Add(lblTitle, 0, 1);
            layout.Controls.Add(lblSubTitle, 0, 2);
            layout.Controls.Add(lblUser, 0, 3);
            layout.Controls.Add(_txtUsername, 0, 4);
            layout.Controls.Add(lblPass, 0, 5);
            layout.Controls.Add(_txtPassword, 0, 6);
            layout.Controls.Add(_chkShowPassword, 0, 7);
            layout.Controls.Add(_lblError, 0, 8);
            layout.Controls.Add(_btnLogin, 0, 9);
            layout.Controls.Add(grpAccounts, 0, 10);

            cardBox.Controls.Add(layout);
            this.Controls.Add(cardBox);
        }

        private Button CreateAccountBtn(string text, string u, string p, Color c)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = UiHelper.Pad(2),
                FlatStyle = FlatStyle.Flat,
                BackColor = c,
                ForeColor = Color.White,
                Font = UiHelper.FontSmallBold,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                _txtUsername.Text = u;
                _txtPassword.Text = p;
                _lblError.Text = string.Empty;
            };
            return btn;
        }

        private void PerformLogin()
        {
            string u = _txtUsername.Text.Trim();
            string p = _txtPassword.Text;

            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                _lblError.Text = "Vui lòng nhập đầy đủ tài khoản và mật khẩu!";
                return;
            }

            try
            {
                var user = _controller.Login(u, p);
                if (user == null)
                {
                    _lblError.Text = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    _txtPassword.Focus();
                    return;
                }

                AppSession.SignIn(user);
                LoggedInUser = user;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = "Lỗi kết nối CSDL" + ex.Message;
            }
        }
    }
}




