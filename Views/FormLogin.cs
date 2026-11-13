using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using ClinicManagement.Controllers;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Views
{
    public partial class FormLogin : Form
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
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.ClientSize = new Size(520, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(480, 560);
            this.BackColor = Color.FromArgb(238, 242, 246);
            
            // Khung nền và thẻ
            var rootPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28),
                BackColor = Color.FromArgb(238, 242, 246)
            };

            var cardBox = new Panel
            {
                Dock = DockStyle.None,
                Size = new Size(520, 700),
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20),
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
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            // Logo và tiêu đề
            var iconBox = new IconPictureBox
            {
                IconChar = IconChar.HospitalUser,
                IconFont = IconFont.Auto,
                IconColor = UiHelper.PrimaryBlue,
                IconSize = 52,
                Size = new Size(52, 52),
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6)
            };

            var lblTitle = new Label
            {
                Text = "ĐĂNG NHẬP PHÒNG KHÁM",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubTitle = new Label
            {
                Text = "Hệ thống quản lý phòng khám và đơn thuốc",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = UiHelper.SecondaryGray,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 18)
            };
            
            // Tài khoản
            _txtUsername = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                Margin = new Padding(0, 0, 0, 10),
                Text = "admin"
            };

            _txtPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 0, 0, 4),
                Text = "admin123"
            };

            _chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 14),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            };

            _chkShowPassword.CheckedChanged += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;
            };
            
            // Thông báo lỗi và nút đăng nhập
            _lblError = new Label
            {
                Text = string.Empty,
                ForeColor = UiHelper.DangerRed,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnLogin = UiHelper.CreateButton("ĐĂNG NHẬP HỆ THỐNG", IconChar.RightToBracket, UiHelper.PrimaryBlue, 200, 44);
            _btnLogin.Dock = DockStyle.Fill;
            _btnLogin.TextAlign = ContentAlignment.MiddleCenter;
            _btnLogin.AutoSize = false;
            _btnLogin.Margin = new Padding(0);
            _btnLogin.Click += (s, e) => PerformLogin();
            this.AcceptButton = _btnLogin;
            
            
            // Chọn tài khoản
            var pnlAccounts = new GroupBox()
            {
                Text = "Chọn tài khoản:",
                Dock = DockStyle.Fill,
                ForeColor = UiHelper.SecondaryGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(8, 6, 8, 8),
                Margin = new Padding(0)
            };

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
            pnlAccounts.Controls.Add(accountGrid);
            
            // Thêm vào bảng
            layout.Controls.Add(iconBox);
            layout.Controls.Add(lblTitle);
            layout.Controls.Add(lblSubTitle);

            layout.Controls.Add(new Label
            {
                Text = "Tên đăng nhập",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            });
            layout.Controls.Add(_txtUsername);

            layout.Controls.Add(new Label
            {
                Text = "Mật khẩu",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            });
            layout.Controls.Add(_txtPassword);
            layout.Controls.Add(_chkShowPassword);

            _lblError.Height = 34;
            layout.Controls.Add(_lblError);
            
            _btnLogin.Height = 48;
            _btnLogin.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(_btnLogin);
            
            layout.Controls.Add(new Panel
                {
                    Height = 14,
                    Dock = DockStyle.Fill
                });

            pnlAccounts.Height = 120;  
            layout.Controls.Add(pnlAccounts);

            cardBox.Controls.Add(layout);
            rootPanel.Controls.Add(cardBox);

            void CenterCard()
            {
                cardBox.Left = (rootPanel.ClientSize.Width - cardBox.Width) / 2;
                cardBox.Top = (rootPanel.ClientSize.Height - cardBox.Height) / 2;
            }
            rootPanel.Resize += (s, e) => CenterCard();
            CenterCard();
            
            this.Controls.Add(rootPanel);
        }

        private static void AddRow(TableLayoutPanel table, Control control, float absoluteHeight = 0)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(absoluteHeight > 0 ? new RowStyle(SizeType.Absolute, absoluteHeight) : new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, row);
        }

        private static Label MakeFieldLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = UiHelper.DarkText,
            Margin = new Padding(0, 0, 0, 4)
        };

        private Button CreateAccountBtn(string text, string u, string p, Color c)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                FlatStyle = FlatStyle.Flat,
                BackColor = c,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
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




