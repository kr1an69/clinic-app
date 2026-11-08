using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormMedicineEditor : Form
    {
        private readonly PharmacyController _controller = new();
        private readonly int? _initialMedicineId;
        private List<Medicine> _medicinesList = new();

        // Control bên phần Thêm thuốc mới
        private TextBox _txtNewCode = null!;
        private TextBox _txtNewName = null!;
        private ComboBox _cboNewUnit = null!;
        private NumericUpDown _nudNewPrice = null!;
        private NumericUpDown _nudNewStock = null!;
        private NumericUpDown _nudNewMinAlert = null!;
        private DateTimePicker _dtpNewExpiry = null!;
        private IconButton _btnAddNew = null!;

        // Control bên phần Nhập bổ sung tồn kho
        private ComboBox _cboSelectMedicine = null!;
        private Label _lblCurrentCode = null!;
        private Label _lblCurrentStock = null!;
        private Label _lblCurrentPrice = null!;
        private Label _lblCurrentExpiry = null!;
        private NumericUpDown _nudAddQty = null!;
        private CheckBox _chkUpdatePrice = null!;
        private NumericUpDown _nudRestockPrice = null!;
        private CheckBox _chkUpdateExpiry = null!;
        private DateTimePicker _dtpRestockExpiry = null!;
        private IconButton _btnRestock = null!;

        public FormMedicineEditor(int? initialMedicineId = null)
        {
            _initialMedicineId = initialMedicineId;
            InitializeComponent();
            LoadMedicinesIntoDropdown();
        }

        private void InitializeComponent()
        {
            this.Text = "Thêm Thuốc Mới & Nhập Bổ Sung Tồn Kho";
            this.Size = new Size(1180, 750);
            this.MinimumSize = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Padding = new Padding(16);

            // Bố cục tổng thể 3 hàng: Tiêu đề trên cùng, 2 GroupBox ở giữa, Nút đóng ở dưới
            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 1. Header tiêu đề
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblTitle = new Label
            {
                Text = "QUẢN LÝ NHẬP KHO && THÊM THUỐC MỚI",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblSubtitle = new Label
            {
                Text = "Thêm danh mục thuốc mới vào kho hoặc nhập bổ sung số lượng cho thuốc đã có sẵn.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.SecondaryGray,
                AutoSize = true,
                Location = new Point(0, 26)
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);

            // 2. Khu vực nội dung chia 2 cột đều nhau
            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Cột trái: Thêm thuốc mới
            var grpAddNew = BuildAddNewGroupBox();
            // Cột phải: Nhập bổ sung tồn kho
            var grpRestock = BuildRestockGroupBox();

            contentLayout.Controls.Add(grpAddNew, 0, 0);
            contentLayout.Controls.Add(grpRestock, 1, 0);

            // 3. Footer dưới cùng chứa nút Đóng
            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnClose = UiHelper.CreateTextButton("Đóng / Quay lại", UiHelper.SecondaryGray, 130, 38);
            btnClose.Click += (_, _) => this.Close();
            footerFlow.Controls.Add(btnClose);

            rootLayout.Controls.Add(headerPanel, 0, 0);
            rootLayout.Controls.Add(contentLayout, 0, 1);
            rootLayout.Controls.Add(footerFlow, 0, 2);

            this.Controls.Add(rootLayout);
        }

        // Xây dựng khối bên trái: Thêm thuốc mới
        private GroupBox BuildAddNewGroupBox()
        {
            var grp = new GroupBox
            {
                Text = "1. Thêm Thuốc Mới Vào Kho",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 14),
                Margin = new Padding(0, 0, 8, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 7; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Mã thuốc
            var lblCode = new Label { Text = "Mã thuốc (*):", Anchor = AnchorStyles.Left, AutoSize = true };
            _txtNewCode = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "VD: MED009" };

            // Tên thuốc
            var lblName = new Label { Text = "Tên thuốc (*):", Anchor = AnchorStyles.Left, AutoSize = true };
            _txtNewName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "VD: Paracetamol 500mg" };

            // Đơn vị tính
            var lblUnit = new Label { Text = "Đơn vị tính:", Anchor = AnchorStyles.Left, AutoSize = true };
            _cboNewUnit = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboNewUnit.Items.AddRange(new object[] { "Viên", "Vỉ", "Hộp", "Chai", "Lọ", "Gói", "Tuýp", "Ống" });
            _cboNewUnit.SelectedIndex = 0;

            // Đơn giá bán
            var lblPrice = new Label { Text = "Đơn giá (VNĐ):", Anchor = AnchorStyles.Left, AutoSize = true };
            _nudNewPrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 1000,
                ThousandsSeparator = true,
                Value = 10000
            };

            // Tồn kho ban đầu
            var lblStock = new Label { Text = "Tồn ban đầu:", Anchor = AnchorStyles.Left, AutoSize = true };
            _nudNewStock = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100000,
                ThousandsSeparator = true,
                Value = 100
            };

            // Cảnh báo tồn tối thiểu
            var lblMinAlert = new Label { Text = "Mức báo tồn:", Anchor = AnchorStyles.Left, AutoSize = true };
            _nudNewMinAlert = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1000,
                Value = 10
            };

            // Hạn sử dụng
            var lblExpiry = new Label { Text = "Hạn sử dụng:", Anchor = AnchorStyles.Left, AutoSize = true };
            _dtpNewExpiry = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddYears(1)
            };

            // Nút Lưu thuốc mới
            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 0)
            };
            _btnAddNew = UiHelper.CreateButton("Lưu Thuốc Mới Vào Kho", IconChar.Plus, UiHelper.SuccessGreen, 230, 38);
            _btnAddNew.Click += BtnAddNew_Click;
            btnFlow.Controls.Add(_btnAddNew);

            layout.Controls.Add(lblCode, 0, 0);
            layout.Controls.Add(_txtNewCode, 1, 0);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(_txtNewName, 1, 1);
            layout.Controls.Add(lblUnit, 0, 2);
            layout.Controls.Add(_cboNewUnit, 1, 2);
            layout.Controls.Add(lblPrice, 0, 3);
            layout.Controls.Add(_nudNewPrice, 1, 3);
            layout.Controls.Add(lblStock, 0, 4);
            layout.Controls.Add(_nudNewStock, 1, 4);
            layout.Controls.Add(lblMinAlert, 0, 5);
            layout.Controls.Add(_nudNewMinAlert, 1, 5);
            layout.Controls.Add(lblExpiry, 0, 6);
            layout.Controls.Add(_dtpNewExpiry, 1, 6);
            layout.Controls.Add(btnFlow, 1, 7);

            grp.Controls.Add(layout);
            return grp;
        }

        // Xây dựng khối bên phải: Nhập bổ sung tồn kho cho thuốc đã có
        private GroupBox BuildRestockGroupBox()
        {
            var grp = new GroupBox
            {
                Text = "2. Nhập Bổ Sung Tồn Kho (Thuốc Đã Có)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 14),
                Margin = new Padding(8, 0, 0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Chọn thuốc
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F)); // Panel thông tin hiện tại
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Số lượng nhập
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Cập nhật đơn giá
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Cập nhật hạn dùng
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Nút bấm
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Chọn thuốc
            var lblSelect = new Label { Text = "Chọn thuốc (*):", Anchor = AnchorStyles.Left, AutoSize = true };
            _cboSelectMedicine = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 600
            };
            _cboSelectMedicine.SelectedIndexChanged += (_, _) => UpdateSelectedMedicineInfo();

            // Panel hiển thị thông tin thuốc hiện tại
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 4, 0, 8)
            };

            var infoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            _lblCurrentCode = new Label { Text = "Mã thuốc: --", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), Anchor = AnchorStyles.Left, Margin = new Padding(4, 4, 4, 4) };
            _lblCurrentStock = new Label { Text = "Tồn kho: --", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = UiHelper.PrimaryBlue, Anchor = AnchorStyles.Left, Margin = new Padding(4, 4, 4, 4) };
            _lblCurrentPrice = new Label { Text = "Đơn giá: --", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), Anchor = AnchorStyles.Left, Margin = new Padding(4, 4, 4, 4) };
            _lblCurrentExpiry = new Label { Text = "Hạn dùng: --", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), Anchor = AnchorStyles.Left, Margin = new Padding(4, 4, 4, 4) };

            infoLayout.Controls.Add(_lblCurrentCode, 0, 0);
            infoLayout.Controls.Add(_lblCurrentStock, 1, 0);
            infoLayout.Controls.Add(_lblCurrentPrice, 0, 1);
            infoLayout.Controls.Add(_lblCurrentExpiry, 1, 1);
            pnlInfo.Controls.Add(infoLayout);

            // Số lượng nhập thêm
            var lblAddQty = new Label { Text = "Số lượng nhập (*):", Anchor = AnchorStyles.Left, AutoSize = true };
            _nudAddQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 100000,
                ThousandsSeparator = true,
                Value = 50
            };

            // Tùy chọn cập nhật giá mới
            _chkUpdatePrice = new CheckBox
            {
                Text = "Cập nhật giá bán:",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };
            _nudRestockPrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 1000,
                ThousandsSeparator = true,
                Enabled = false
            };
            _chkUpdatePrice.CheckedChanged += (_, _) => _nudRestockPrice.Enabled = _chkUpdatePrice.Checked;

            // Tùy chọn cập nhật hạn dùng mới
            _chkUpdateExpiry = new CheckBox
            {
                Text = "Cập nhật hạn dùng:",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };
            _dtpRestockExpiry = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddYears(1),
                Enabled = false
            };
            _chkUpdateExpiry.CheckedChanged += (_, _) => _dtpRestockExpiry.Enabled = _chkUpdateExpiry.Checked;

            // Nút Xác nhận nhập kho
            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 0)
            };
            _btnRestock = UiHelper.CreateButton("Xác Nhận Nhập Kho", IconChar.BoxesStacked, UiHelper.PrimaryBlue, 220, 38);
            _btnRestock.Click += BtnRestock_Click;
            btnFlow.Controls.Add(_btnRestock);

            layout.Controls.Add(lblSelect, 0, 0);
            layout.Controls.Add(_cboSelectMedicine, 1, 0);
            layout.Controls.Add(new Label { Text = "Thông tin hiện tại:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
            layout.Controls.Add(pnlInfo, 1, 1);
            layout.Controls.Add(lblAddQty, 0, 2);
            layout.Controls.Add(_nudAddQty, 1, 2);
            layout.Controls.Add(_chkUpdatePrice, 0, 3);
            layout.Controls.Add(_nudRestockPrice, 1, 3);
            layout.Controls.Add(_chkUpdateExpiry, 0, 4);
            layout.Controls.Add(_dtpRestockExpiry, 1, 4);
            layout.Controls.Add(btnFlow, 1, 5);

            grp.Controls.Add(layout);
            return grp;
        }

        // Tải danh sách thuốc vào ComboBox chọn thuốc bên phần Nhập bổ sung
        private void LoadMedicinesIntoDropdown()
        {
            _medicinesList = _controller.GetAllMedicines();
            _cboSelectMedicine.Items.Clear();

            int selectedIndex = -1;
            for (int i = 0; i < _medicinesList.Count; i++)
            {
                var med = _medicinesList[i];
                _cboSelectMedicine.Items.Add($"[{med.Code}] {med.Name} (Tồn: {med.StockQuantity} {med.Unit})");

                if (_initialMedicineId.HasValue && med.Id == _initialMedicineId.Value)
                {
                    selectedIndex = i;
                }
            }

            if (_cboSelectMedicine.Items.Count > 0)
            {
                _cboSelectMedicine.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
            else
            {
                _lblCurrentCode.Text = "Mã thuốc: (Kho trống)";
                _lblCurrentStock.Text = "Tồn kho: 0";
                _lblCurrentPrice.Text = "Đơn giá: 0 đ";
                _lblCurrentExpiry.Text = "Hạn dùng: --";
            }
        }

        // Cập nhật thẻ thông tin khi người dùng chọn thuốc khác trong danh sách
        private void UpdateSelectedMedicineInfo()
        {
            if (_cboSelectMedicine.SelectedIndex < 0 || _cboSelectMedicine.SelectedIndex >= _medicinesList.Count)
                return;

            var med = _medicinesList[_cboSelectMedicine.SelectedIndex];
            _lblCurrentCode.Text = $"Mã thuốc: {med.Code}";
            _lblCurrentStock.Text = $"Tồn kho: {med.StockQuantity} {med.Unit}";
            _lblCurrentPrice.Text = $"Đơn giá: {med.UnitPrice:N0} đ";

            if (!string.IsNullOrWhiteSpace(med.ExpiryDate) && DateTime.TryParse(med.ExpiryDate, out var exp))
            {
                _lblCurrentExpiry.Text = $"Hạn dùng: {exp:dd/MM/yyyy}";
                _dtpRestockExpiry.Value = exp > DateTime.Today ? exp : DateTime.Today.AddYears(1);
            }
            else
            {
                _lblCurrentExpiry.Text = "Hạn dùng: Không rõ";
                _dtpRestockExpiry.Value = DateTime.Today.AddYears(1);
            }

            _nudRestockPrice.Value = (decimal)med.UnitPrice;
        }

        // Xử lý sự kiện lưu thuốc mới
        private void BtnAddNew_Click(object? sender, EventArgs e)
        {
            var code = _txtNewCode.Text.Trim();
            var name = _txtNewName.Text.Trim();
            var unit = _cboNewUnit.SelectedItem?.ToString() ?? "Viên";
            var unitPrice = (double)_nudNewPrice.Value;
            var stockQty = (int)_nudNewStock.Value;
            var minAlert = (int)_nudNewMinAlert.Value;
            var expiryDate = _dtpNewExpiry.Value.ToString("yyyy-MM-dd");

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Vui lòng nhập Mã thuốc!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNewCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập Tên thuốc!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNewName.Focus();
                return;
            }

            // Kiểm tra trùng mã thuốc trong CSDL
            if (_controller.IsMedicineCodeExists(code))
            {
                MessageBox.Show(
                    $"Mã thuốc '{code}' đã tồn tại trong danh mục kho!\nVui lòng nhập mã thuốc khác hoặc chọn thuốc bên phần Nhập bổ sung tồn kho.",
                    "Trùng mã thuốc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtNewCode.Focus();
                return;
            }

            var newMedicine = new Medicine
            {
                Code = code,
                Name = name,
                Unit = unit,
                UnitPrice = unitPrice,
                StockQuantity = stockQty,
                MinStockAlert = minAlert,
                ExpiryDate = expiryDate
            };

            try
            {
                _controller.AddMedicine(newMedicine);

                MessageBox.Show(
                    $"Đã thêm thành công thuốc mới:\n- [{code}] {name}\n- Đơn vị: {unit}\n- Tồn kho ban đầu: {stockQty}\n- Giá bán: {unitPrice:N0} đ",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Đánh dấu Dialog thành công để form cha biết làm mới danh sách
                this.DialogResult = DialogResult.OK;

                // Tải lại danh sách chọn thuốc bên cột nhập kho và xóa trắng form nhập mới
                LoadMedicinesIntoDropdown();
                _txtNewCode.Clear();
                _txtNewName.Clear();
                _nudNewStock.Value = 100;
                _txtNewCode.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm thuốc mới: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Xử lý sự kiện nhập bổ sung tồn kho cho thuốc đã có
        private void BtnRestock_Click(object? sender, EventArgs e)
        {
            if (_cboSelectMedicine.SelectedIndex < 0 || _cboSelectMedicine.SelectedIndex >= _medicinesList.Count)
            {
                MessageBox.Show("Vui lòng chọn thuốc cần nhập bổ sung tồn kho!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedMed = _medicinesList[_cboSelectMedicine.SelectedIndex];
            var addQty = (int)_nudAddQty.Value;

            if (addQty <= 0)
            {
                MessageBox.Show("Số lượng nhập thêm phải lớn hơn 0!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double? newPrice = _chkUpdatePrice.Checked ? (double)_nudRestockPrice.Value : null;
            string? newExpiry = _chkUpdateExpiry.Checked ? _dtpRestockExpiry.Value.ToString("yyyy-MM-dd") : null;

            try
            {
                _controller.RestockMedicine(selectedMed.Id, addQty, newPrice, newExpiry);

                var newTotalStock = selectedMed.StockQuantity + addQty;
                MessageBox.Show(
                    $"Nhập kho thành công!\n- Thuốc: [{selectedMed.Code}] {selectedMed.Name}\n- Đã cộng thêm: +{addQty} {selectedMed.Unit}\n- Tồn kho mới: {newTotalStock} {selectedMed.Unit}",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;

                // Cập nhật lại danh sách và thông tin trên màn hình
                LoadMedicinesIntoDropdown();
                _nudAddQty.Value = 50;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhập bổ sung tồn kho: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
