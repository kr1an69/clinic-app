using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClinicManagement.Views
{
    public class FormMain : Form
    {
        private Button btnReception = null!;
        private Button btnDoctor = null!;
        private Button btnPharmacy = null!;
        private Button btnQueueMonitor = null!;
        private Panel panelContent = null!;
        private Label lblHeader = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblStatus = null!;

        public FormMain()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ Thống Quản Lý Phòng Khám & Đơn Thuốc (WinForms - MVC - SQLite)";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);

            // Header
            lblHeader = new Label
            {
                Text = "🏥 HỆ THỐNG QUẢN LÝ PHÒNG KHÁM TƯ NHÂN",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Sidebar Menu
            Panel panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(44, 62, 80)
            };

            btnReception = CreateMenuButton("👤 1. Lễ Tân & Bệnh Nhân", 10);
            btnQueueMonitor = CreateMenuButton("📺 1. Màn Hình Chờ (TV)", 70);
            btnDoctor = CreateMenuButton("🩺 2. Bác Sĩ & Kê Đơn", 130);
            btnPharmacy = CreateMenuButton("💊 3. Kho & Thu Ngân", 190);

            panelMenu.Controls.AddRange(new Control[] { btnReception, btnQueueMonitor, btnDoctor, btnPharmacy });

            // Content Panel
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Status bar
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Hệ thống sẵn sàng. Chọn một phân hệ để bắt đầu.");
            statusStrip.Items.Add(lblStatus);

            this.Controls.Add(panelContent);
            this.Controls.Add(panelMenu);
            this.Controls.Add(lblHeader);
            this.Controls.Add(statusStrip);

            // Events
            btnReception.Click += (s, e) => LoadForm(new FormReception(), "Phân hệ 1: Tiếp Nhận, Xếp Hàng & Thống Kê Bệnh Nhân (Thành viên 1)");
            btnQueueMonitor.Click += (s, e) => new FormQueueMonitor().Show();
            btnDoctor.Click += (s, e) => LoadForm(new FormDoctor(), "Phân hệ 2: Bác Sĩ Khám & Kê Đơn Thuốc (Thành viên 2)");
            btnPharmacy.Click += (s, e) => LoadForm(new FormPharmacy(), "Phân hệ 3: Thu Phí, Xuất Thuốc, In Ấn & Báo Cáo Kho (Thành viên 3)");

            // Mặc định mở tab Lễ tân
            LoadForm(new FormReception(), "Phân hệ 1: Tiếp Nhận, Xếp Hàng & Thống Kê Bệnh Nhân (Thành viên 1)");
        }

        private Button CreateMenuButton(string text, int top)
        {
            return new Button
            {
                Text = text,
                Top = top,
                Left = 10,
                Width = 200,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
        }

        private void LoadForm(Form childForm, string statusText)
        {
            panelContent.Controls.Clear();
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelContent.Controls.Add(childForm);
            childForm.Show();
            lblStatus.Text = statusText;
        }
    }
}
