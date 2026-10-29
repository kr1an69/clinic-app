using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;

namespace ClinicManagement.Views
{
    public class FormReception : Form
    {
        private readonly ReceptionController _controller = new();

        public FormReception()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // TODO (Thành viên 1): Thiết kế giao diện Tiếp nhận bệnh nhân, Cấp số thứ tự khám & Thống kê BN theo ngày
            Label lblPlaceholder = new Label
            {
                Text = "👤 PHÂN HỆ 1: TIẾP NHẬN BỆNH NHÂN & CẤP SỐ THỨ TỰ\n(Thành viên 1 phụ trách phát triển tại FormReception.cs)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            this.Controls.Add(lblPlaceholder);
        }
    }
}
