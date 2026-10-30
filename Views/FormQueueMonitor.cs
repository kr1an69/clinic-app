using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormQueueMonitor : Form
    {
        private readonly ReceptionController _controller = new();
        private TableLayoutPanel _rootLayout = null!;
        private IconPictureBox _iconBanner = null!;
        private Label _lblTitle = null!;
        private Label _lblDescription = null!;

        public FormQueueMonitor()
        {
            _ = _controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "Màn Hình Phòng Chờ Khám Bệnh";
            this.Size = new Size(900, 620);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // TODO (Thành viên 1): Thiết kế Màn hình TV phòng chờ hiển thị danh sách bệnh nhân đang đợi theo STT (dùng Timer tự động cập nhật)
            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var cardLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Anchor = AnchorStyles.None,
                Padding = new Padding(20)
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _iconBanner = new IconPictureBox
            {
                IconChar = IconChar.Tv,
                IconFont = IconFont.Auto,
                IconColor = Color.FromArgb(231, 76, 60),
                IconSize = 56,
                Size = new Size(56, 56),
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 12)
            };

            _lblTitle = new Label
            {
                Text = "MÀN HÌNH PHÒNG CHỜ (QUEUE MONITOR)",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60),
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblDescription = new Label
            {
                Text = "(Thành viên 1 phụ trách phát triển tại FormQueueMonitor.cs)",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(127, 140, 141),
                Margin = new Padding(0)
            };

            cardLayout.Controls.Add(_iconBanner, 0, 0);
            cardLayout.Controls.Add(_lblTitle, 0, 1);
            cardLayout.Controls.Add(_lblDescription, 0, 2);

            _rootLayout.Controls.Add(cardLayout, 1, 1);
            this.Controls.Add(_rootLayout);
        }
    }
}
