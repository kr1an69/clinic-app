using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;

namespace ClinicManagement.Views
{
    public class FormQueueMonitor : Form
    {
        private readonly ReceptionController _controller = new();

        public FormQueueMonitor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Màn Hình Phòng Chờ Khám Bệnh";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 11F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // TODO (Thành viên 1): Thiết kế Màn hình TV phòng chờ hiển thị danh sách bệnh nhân đang đợi theo STT (dùng Timer tự động cập nhật)
            Label lblPlaceholder = new Label
            {
                Text = "📺 MÀN HÌNH PHÒNG CHỜ (QUEUE MONITOR)\n(Thành viên 1 phụ trách phát triển tại FormQueueMonitor.cs)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60)
            };

            this.Controls.Add(lblPlaceholder);
        }
    }
}
