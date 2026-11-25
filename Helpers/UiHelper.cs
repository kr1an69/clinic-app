using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement.Helpers
{
    /// <summary>
    /// Lớp cơ sở cho toàn bộ 16 Forms trong hệ thống:
    /// - Tắt AutoScale ngầm của WinForms (AutoScaleMode.None) để tránh nhân đôi kích thước (Double Scaling).
    /// - Cung cấp 2 chế độ cửa sổ chuẩn hóa:
    ///   1. ConfigureAsFullScreen(): Mở Maximized mặc định + khóa MinimumSize chống cắt chữ (dành cho màn hình chính/danh sách).
    ///   2. ConfigureAsFixedDialog(): Khóa cứng kích thước tối ưu, không cho user resize (dành cho Popup nhỏ 2 cột).
    /// </summary>
    public class BaseForm : Form
    {
        public BaseForm()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Font = UiHelper.FontRegular;
            this.BackColor = UiHelper.BgMain;
        }

        /// <summary>
        /// Nhóm 1: Cấu hình cho các màn hình chính nhiều thành phần -> Mở Full Screen (Maximized) mặc định,
        /// đồng thời giữ MinimumSize đủ lớn để nếu user thu nhỏ cửa sổ thì các thành phần vẫn không bị cắt.
        /// </summary>
        protected void ConfigureAsFullScreen(int minBaseWidth = 1180, int minBaseHeight = 680)
        {
            UiHelper.ConfigureFullScreenForm(this, minBaseWidth, minBaseHeight);
        }

        /// <summary>
        /// Nhóm 2: Cấu hình cho các màn hình Popup/Dialog ít thành phần -> Khóa cứng kích thước (FixedDialog),
        /// tắt nút Maximize và không cho phép user kéo giãn cửa sổ.
        /// </summary>
        protected void ConfigureAsFixedDialog(int baseWidth = 1000, int baseHeight = 580)
        {
            UiHelper.ConfigureFixedDialog(this, baseWidth, baseHeight);
        }

        /// <summary>
        /// Tương thích ngược cho các lời gọi cấu hình kích thước tuỳ chỉnh.
        /// </summary>
        protected void ConfigureWindowBounds(int baseWidth, int baseHeight, int minWidth = 0, int minHeight = 0)
        {
            UiHelper.ConfigureForm(this, baseWidth, baseHeight, minWidth, minHeight);
        }
    }

    public static class UiHelper
    {
        // =====================================================================
        // 1. BẢNG MÀU & FONT HỆ THỐNG - Design Token
        // =====================================================================
        public static readonly Color PrimaryBlue = Color.FromArgb(41, 128, 185);
        public static readonly Color SuccessGreen = Color.FromArgb(46, 204, 113);
        public static readonly Color WarningOrange = Color.FromArgb(230, 126, 34);
        public static readonly Color DangerRed = Color.FromArgb(231, 76, 60);
        public static readonly Color SecondaryGray = Color.FromArgb(127, 140, 141);
        public static readonly Color DarkText = Color.FromArgb(44, 62, 80);

        public static readonly Color BgMain = Color.FromArgb(245, 247, 250);
        public static readonly Color SidebarBg = Color.FromArgb(44, 62, 80);
        public static readonly Color SidebarBtnBg = Color.FromArgb(52, 73, 94);
        public static readonly Color MutedInputBg = Color.FromArgb(237, 242, 247);

        // Font đơn vị Point (pt) được GDI+ tự động scale theo System DPI, không nhân DpiScale vào cỡ chữ pt
        public static Font FontRegular => new("Segoe UI", 9.5F, FontStyle.Regular);
        public static Font FontBold => new("Segoe UI", 9.5F, FontStyle.Bold);
        public static Font FontSmall => new("Segoe UI", 8.5F, FontStyle.Regular);
        public static Font FontSmallBold => new("Segoe UI", 8.5F, FontStyle.Bold);
        public static Font FontLabelBold => new("Segoe UI", 9F, FontStyle.Bold);
        public static Font FontSection => new("Segoe UI", 10F, FontStyle.Bold);

        // =====================================================================
        // 2. THUẬT TOÁN QUY ĐỔI KÍCH THƯỚC THEO DPI & ĐỘ PHÂN GIẢI MÀN HÌNH
        // =====================================================================
        private static float? _systemDpiScale;

        /// <summary>
        /// Hệ số tỷ lệ DPI của hệ thống so với chuẩn thiết kế 100% (96 DPI).
        /// Ví dụ: 100% -> 1.0f | 125% -> 1.25f | 150% -> 1.5f | 175% -> 1.75f
        /// </summary>
        public static float DpiScale
        {
            get
            {
                if (_systemDpiScale == null)
                {
                    using var g = Graphics.FromHwnd(IntPtr.Zero);
                    _systemDpiScale = g.DpiX > 0 ? g.DpiX / 96f : 1f;
                }
                return _systemDpiScale.Value;
            }
        }

        public static int S(int basePx) => (int)Math.Round(basePx * DpiScale);

        public static float SF(float basePx) => (float)Math.Round(basePx * DpiScale);

        public static Size SSize(int baseWidth, int baseHeight) => new(S(baseWidth), S(baseHeight));

        public static Padding Pad(int all) => new(S(all));

        public static Padding Pad(int horizontal, int vertical) =>
            new(S(horizontal), S(vertical), S(horizontal), S(vertical));

        public static Padding Pad(int left, int top, int right, int bottom) =>
            new(S(left), S(top), S(right), S(bottom));

        /// <summary>
        /// Chuyển đổi ký tự '&' đơn thành '&&' để tránh lỗi WinForms hiểu nhầm là phím tắt (hiển thị dấu '_').
        /// </summary>
        public static string EscapeMnemonic(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return text.Replace("&&", "&").Replace("&", "&&");
        }

        /// <summary>
        /// Nhóm 1: Cấu hình màn hình chính / danh sách mở Full Screen (Maximized) mặc định,
        /// kèm MinimumSize lớn (kẹp theo WorkingArea) để khi thu nhỏ không bị cắt giao diện.
        /// </summary>
        public static void ConfigureFullScreenForm(Form form, int minBaseWidth = 1180, int minBaseHeight = 680)
        {
            form.AutoScaleMode = AutoScaleMode.None;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimizeBox = true;
            form.StartPosition = FormStartPosition.CenterScreen;

            Rectangle workArea = Screen.FromControl(form).WorkingArea;
            int minW = Math.Min(S(minBaseWidth), (int)(workArea.Width * 0.94));
            int minH = Math.Min(S(minBaseHeight), (int)(workArea.Height * 0.90));

            form.MinimumSize = new Size(minW, minH);
            form.Size = new Size(minW, minH);
            form.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Nhóm 2: Cấu hình Popup / Dialog nhỏ gọn -> Khóa cứng kích thước tối ưu (FixedDialog), không cho user resize.
        /// </summary>
        public static void ConfigureFixedDialog(Form form, int baseWidth = 1000, int baseHeight = 580)
        {
            form.AutoScaleMode = AutoScaleMode.None;
            form.WindowState = FormWindowState.Normal;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterParent;

            Rectangle workArea = Screen.FromControl(form).WorkingArea;
            int fixedW = Math.Min(S(baseWidth), (int)(workArea.Width * 0.95));
            int fixedH = Math.Min(S(baseHeight), (int)(workArea.Height * 0.92));

            var lockedSize = new Size(fixedW, fixedH);
            form.MinimumSize = lockedSize;
            form.MaximumSize = lockedSize;
            form.Size = lockedSize;
        }

        public static void ConfigureForm(Form form, int baseWidth, int baseHeight, int minWidth = 0, int minHeight = 0)
        {
            form.AutoScaleMode = AutoScaleMode.None;

            Rectangle workArea = Screen.FromControl(form).WorkingArea;
            int maxW = (int)(workArea.Width * 0.96);
            int maxH = (int)(workArea.Height * 0.94);

            int targetW = Math.Min(S(baseWidth), maxW);
            int targetH = Math.Min(S(baseHeight), maxH);

            if (minWidth > 0 && minHeight > 0)
            {
                int targetMinW = Math.Min(S(minWidth), targetW);
                int targetMinH = Math.Min(S(minHeight), targetH);
                form.MinimumSize = new Size(targetMinW, targetMinH);
            }

            form.Size = new Size(targetW, targetH);
        }

        // =====================================================================
        // 3. NHÓM HÀM KHỞI TẠO CONTROL & KHỐI BỐ CỤC ĐỒNG BỘ (UI FACTORY METHODS)
        // =====================================================================

        /// <summary>
        /// Tạo nút bấm có Icon (mặc định Icon bên trái), tự động scale kích thước nút, Padding, Margin và Icon theo DPI.
        /// </summary>
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
                Text = EscapeMnemonic(text),
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                IconSize = icon != IconChar.None ? S(baseIconSize) : 16,
                TextImageRelation = relation,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = SSize(minWidth, height),
                Padding = Pad(12, 2, 12, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = FontBold,
                Cursor = Cursors.Hand,
                Margin = Pad(6, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            if (icon != IconChar.None)
            {
                void ScaleIcon(object? s, EventArgs e)
                {
                    float scale = btn.DeviceDpi > 0 ? btn.DeviceDpi / 96f : DpiScale;
                    btn.IconSize = (int)Math.Round(baseIconSize * scale);
                }

                btn.HandleCreated += ScaleIcon;
                btn.DpiChangedAfterParent += ScaleIcon;
            }

            return btn;
        }

        /// <summary>
        /// Tạo nút bấm chỉ có chữ (No Icon) đồng bộ kiểu dáng và chiều cao theo DPI.
        /// </summary>
        public static IconButton CreateTextButton(
            string text,
            Color backColor,
            int minWidth = 90,
            int height = 36)
        {
            return CreateButton(text, IconChar.None, backColor, minWidth, height, 0, TextImageRelation.Overlay);
        }

        /// <summary>
        /// Tạo nút điều hướng trên Sidebar của FormMain, tự động scale chiều cao, Icon và căn chỉnh tâm Icon theo DPI.
        /// </summary>
        public static IconButton CreateSidebarButton(string text, IconChar icon, int baseHeight = 52, int baseIconSize = 24)
        {
            var btn = new IconButton
            {
                Text = "  " + EscapeMnemonic(text),
                IconChar = icon,
                IconFont = IconFont.Auto,
                IconColor = Color.White,
                IconSize = S(baseIconSize),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Height = S(baseHeight),
                Margin = Pad(0, 0, 0, 10),
                Padding = Pad(12, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = SidebarBtnBg,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoEllipsis = false,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = PrimaryBlue;

            AlignIconBitmapVertically(btn, S(3));
            return btn;
        }

        public static void AlignIconBitmapVertically(IconButton btn, int scaledOffsetY)
        {
            if (scaledOffsetY <= 0 || btn.Image is not Bitmap originalBmp)
            {
                return;
            }

            var alignedBmp = new Bitmap(originalBmp.Width, originalBmp.Height + scaledOffsetY);
            using (var g = Graphics.FromImage(alignedBmp))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(originalBmp, 0, scaledOffsetY, originalBmp.Width, originalBmp.Height);
            }

            btn.Image = alignedBmp;
        }

        /// <summary>
        /// Tạo thanh Header Banner (Tiêu đề lớn + Mô tả phụ) dùng TableLayoutPanel AutoSize để không bao giờ đè/cắt chữ ở 150% DPI.
        /// </summary>
        public static Panel CreateHeaderBanner(string title, string subtitle, Color? backColor = null)
        {
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = backColor ?? Color.FromArgb(234, 242, 248),
                Margin = Padding.Empty
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = Pad(16, 10, 16, 10),
                Margin = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = CreateLabel(
                text: title,
                font: new Font("Segoe UI", 11.5F, FontStyle.Bold),
                foreColor: DarkText,
                margin: Pad(0, 0, 0, 3));

            var lblSubtitle = CreateLabel(
                text: subtitle,
                font: new Font("Segoe UI", 9F, FontStyle.Regular),
                foreColor: SecondaryGray,
                margin: Padding.Empty);

            grid.Controls.Add(lblTitle, 0, 0);
            grid.Controls.Add(lblSubtitle, 0, 1);

            var borderBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = Math.Max(1, S(1)),
                BackColor = Color.FromArgb(169, 204, 227)
            };

            banner.Controls.Add(grid);
            banner.Controls.Add(borderBottom);
            return banner;
        }

        /// <summary>
        /// Tạo bảng Form nhập liệu 2 cột chuẩn (Cột trái: Nhãn cố định theo DPI, Cột phải: Ô nhập 100%)
        /// giúp các thành viên không phải tự cấu hình RowStyle.Absolute dễ gây lỗi.
        /// </summary>
        public static TableLayoutPanel CreateFormGrid(int labelColumnBaseWidth = 145)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 0,
                Font = FontRegular,
                Margin = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SF(labelColumnBaseWidth)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return grid;
        }

        /// <summary>
        /// Thêm một dòng (Label + Input Control) vào bảng Form 2 cột với RowStyle.AutoSize và tự động căn giữa dọc.
        /// </summary>
        public static void AddFormRow(TableLayoutPanel grid, string labelText, Control inputControl, int verticalGap = 5)
        {
            var lbl = CreateLabel(
                text: labelText,
                font: FontRegular,
                foreColor: DarkText,
                margin: Pad(0, verticalGap, 8, verticalGap));
            lbl.Anchor = AnchorStyles.Left;

            AddFormRow(grid, lbl, inputControl, verticalGap);
        }

        /// <summary>
        /// Thêm một dòng (Control cột trái như CheckBox/Label + Input Control cột phải) vào bảng Form 2 cột.
        /// </summary>
        public static void AddFormRow(TableLayoutPanel grid, Control leftControl, Control rightControl, int verticalGap = 5)
        {
            int rowIndex = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            leftControl.Anchor = AnchorStyles.Left;
            leftControl.Margin = Pad(0, verticalGap, 8, verticalGap);

            // Căn giữa dọc (Left | Right) cho các ô nhập đơn dòng, tránh Dock.Fill ép dính sát mép trên
            rightControl.Dock = DockStyle.None;
            rightControl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            rightControl.Margin = Pad(0, verticalGap, 0, verticalGap);

            grid.Controls.Add(leftControl, 0, rowIndex);
            grid.Controls.Add(rightControl, 1, rowIndex);
        }

        /// <summary>
        /// Tạo GroupBox chuẩn của hệ thống: tự động escape dấu '&', gán Font tiêu đề và Padding theo DPI.
        /// </summary>
        public static GroupBox CreateGroupBox(
            string title,
            Color? titleColor = null,
            Padding? padding = null,
            Padding? margin = null)
        {
            return new GroupBox
            {
                Text = EscapeMnemonic(title),
                Dock = DockStyle.Fill,
                Font = FontSection,
                ForeColor = titleColor ?? PrimaryBlue,
                BackColor = Color.White,
                Padding = padding ?? Pad(12, 8, 12, 12),
                Margin = margin ?? Padding.Empty
            };
        }

        /// <summary>
        /// Tạo Label chuẩn với AutoSize = true, UseMnemonic = false (hiển thị nguyên vẹn dấu '&') và Margin theo DPI.
        /// </summary>
        public static Label CreateLabel(
            string text,
            Font? font = null,
            Color? foreColor = null,
            Padding? margin = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                UseMnemonic = false,
                Font = font ?? FontRegular,
                ForeColor = foreColor ?? DarkText,
                Margin = margin ?? Pad(0, 0, 0, 4)
            };
        }

        /// <summary>
        /// Tạo DataGridView chuẩn đồng bộ màu sắc, chiều cao dòng và chiều cao tiêu đề cột theo DPI.
        /// </summary>
        public static DataGridView CreateStandardGrid(int baseRowHeight = 34, int baseHeaderHeight = 38)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = S(baseHeaderHeight),
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = FontRegular,
                RowTemplate = { Height = S(baseRowHeight) },
                Margin = Padding.Empty
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryBlue;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = Pad(6, 4, 6, 4);

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            dgv.DefaultCellStyle.SelectionForeColor = DarkText;
            dgv.DefaultCellStyle.Padding = Pad(6, 2, 6, 2);

            return dgv;
        }
    }
}
