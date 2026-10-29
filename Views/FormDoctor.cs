using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;

namespace ClinicManagement.Views
{
    public class FormDoctor : Form
    {
        private readonly DoctorController _controller = new();

        public FormDoctor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // TODO (Thành viên 2): Thiết kế giao diện Khám bệnh, nhập triệu chứng/chẩn đoán, kê đơn thuốc AutoComplete & kiểm tra tồn kho
            Label lblPlaceholder = new Label
            {
                Text = "🩺 PHÂN HỆ 2: BÁC SĨ KHÁM BỆNH & KÊ ĐƠN THUỐC\n(Thành viên 2 phụ trách phát triển tại FormDoctor.cs)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185)
            };

            this.Controls.Add(lblPlaceholder);
        }
    }
}
