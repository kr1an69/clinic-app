using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;

namespace ClinicManagement.Views
{
    public class FormPharmacy : Form
    {
        private readonly PharmacyController _controller = new();

        public FormPharmacy()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // TODO (Thành viên 3): Thiết kế giao diện Thu phí, Xuất thuốc tự động trừ kho, In đơn thuốc/hóa đơn & Cảnh báo thuốc hết hạn/hết hàng
            Label lblPlaceholder = new Label
            {
                Text = "💊 PHÂN HỆ 3: THU PHÍ, XUẤT THUỐC, IN ẤN & QUẢN LÝ KHO\n(Thành viên 3 phụ trách phát triển tại FormPharmacy.cs)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96)
            };

            this.Controls.Add(lblPlaceholder);
        }
    }
}
