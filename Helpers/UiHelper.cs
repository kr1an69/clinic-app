using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement.Helpers
{
    public static class UiHelper
    {
        // Bảng màu chuẩn dùng chung toàn hệ thống
        public static readonly Color PrimaryBlue = Color.FromArgb(41, 128, 185);
        public static readonly Color SuccessGreen = Color.FromArgb(46, 204, 113);
        public static readonly Color WarningOrange = Color.FromArgb(230, 126, 34);
        public static readonly Color DangerRed = Color.FromArgb(231, 76, 60);
        public static readonly Color SecondaryGray = Color.FromArgb(127, 140, 141);
        public static readonly Color DarkText = Color.FromArgb(44, 62, 80);

        // Tạo nút bấm có Icon (mặc định Icon bên trái) và tự động phóng Icon theo DPI màn hình
        public static IconButton CreateButton(
            string text,
            IconChar icon,
            Color backColor,
            int minWidth = 100,
            int height = 36,
            int baseIconSize = 20,
            TextImageRelation relation = TextImageRelation.ImageBeforeText)
        {
            var btn = new IconButton
            {
                Text = text,
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                TextImageRelation = relation,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(minWidth, height),
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            if (icon != IconChar.None)
            {
                void ScaleIcon(object? s, EventArgs e) =>
                    btn.IconSize = (int)Math.Round(baseIconSize * btn.DeviceDpi / 96f);

                btn.HandleCreated += ScaleIcon;
                btn.DpiChangedAfterParent += ScaleIcon;
            }

            return btn;
        }

        // Tạo nút bấm chỉ có chữ (No Icon) đồng bộ kiểu dáng và chiều cao
        public static IconButton CreateTextButton(
            string text,
            Color backColor,
            int minWidth = 90,
            int height = 36)
        {
            return CreateButton(text, IconChar.None, backColor, minWidth, height, 0, TextImageRelation.Overlay);
        }
    }
}
