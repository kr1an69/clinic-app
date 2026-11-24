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
    public class FormMedicineEditor : BaseForm
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
            this.ConfigureAsFixedDialog(baseWidth: 1080, baseHeight: 650);

            // Bố cục tổng thể 3 hàng: Header trên cùng, 2 GroupBox ở giữa, Nút đóng ở dưới
            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UiHelper.BgMain,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var headerBanner = UiHelper.CreateHeaderBanner(
                "QUẢN LÝ NHẬP KHO & THÊM THUỐC MỚI",
                "Thêm danh mục thuốc mới vào kho hoặc nhập bổ sung số lượng cho thuốc đã có sẵn");

            // 2. Khu vực nội dung chia 2 cột đều nhau
            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = UiHelper.Pad(14, 12, 14, 6),
                Margin = Padding.Empty
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpAddNew = BuildAddNewGroupBox();
            var grpRestock = BuildRestockGroupBox();

            contentLayout.Controls.Add(grpAddNew, 0, 0);
            contentLayout.Controls.Add(grpRestock, 1, 0);

            // 3. Footer dưới cùng chứa nút Đóng
            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = UiHelper.Pad(14, 4, 14, 12),
                Margin = Padding.Empty
            };

            var btnClose = UiHelper.CreateTextButton("Đóng / Quay lại", UiHelper.SecondaryGray, 130, 38);
            btnClose.Click += (_, _) => this.Close();
            footerFlow.Controls.Add(btnClose);

            rootLayout.Controls.Add(headerBanner, 0, 0);
            rootLayout.Controls.Add(contentLayout, 0, 1);
            rootLayout.Controls.Add(footerFlow, 0, 2);

            this.Controls.Add(rootLayout);
        }

        // Xây dựng khối bên trái: Thêm thuốc mới
        private GroupBox BuildAddNewGroupBox()
        {
            var grp = UiHelper.CreateGroupBox(
                "1. Thêm Thuốc Mới Vào Kho",
                UiHelper.DarkText,
                UiHelper.Pad(14, 10, 14, 12),
                UiHelper.Pad(0, 0, 8, 0));

            var outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            outerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var formGrid = UiHelper.CreateFormGrid(labelColumnBaseWidth: 135);

            _txtNewCode = new TextBox
            {
                Font = UiHelper.FontRegular,
                CharacterCasing = CharacterCasing.Upper,
                PlaceholderText = "VD: MED009",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _txtNewName = new TextBox
            {
                Font = UiHelper.FontRegular,
                PlaceholderText = "VD: Paracetamol 500mg",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _cboNewUnit = new ComboBox
            {
                Font = UiHelper.FontRegular,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };
            _cboNewUnit.Items.AddRange(new object[] { "Viên", "Vỉ", "Hộp", "Chai", "Lọ", "Gói", "Tuýp", "Ống" });
            _cboNewUnit.SelectedIndex = 0;

            _nudNewPrice = new NumericUpDown
            {
                Font = UiHelper.FontRegular,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 1000,
                ThousandsSeparator = true,
                Value = 10000,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _nudNewStock = new NumericUpDown
            {
                Font = UiHelper.FontRegular,
                Minimum = 0,
                Maximum = 100000,
                ThousandsSeparator = true,
                Value = 100,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _nudNewMinAlert = new NumericUpDown
            {
                Font = UiHelper.FontRegular,
                Minimum = 0,
                Maximum = 1000,
                Value = 10,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _dtpNewExpiry = new DateTimePicker
            {
                Font = UiHelper.FontRegular,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddYears(1),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Mã thuốc (*):", UiHelper.FontRegular, UiHelper.DarkText), _txtNewCode);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Tên thuốc (*):", UiHelper.FontRegular, UiHelper.DarkText), _txtNewName);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Đơn vị tính:", UiHelper.FontRegular, UiHelper.DarkText), _cboNewUnit);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Đơn giá (VNĐ):", UiHelper.FontRegular, UiHelper.DarkText), _nudNewPrice);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Tồn ban đầu:", UiHelper.FontRegular, UiHelper.DarkText), _nudNewStock);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Mức báo tồn:", UiHelper.FontRegular, UiHelper.DarkText), _nudNewMinAlert);
            UiHelper.AddFormRow(formGrid, UiHelper.CreateLabel("Hạn sử dụng:", UiHelper.FontRegular, UiHelper.DarkText), _dtpNewExpiry);

            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = UiHelper.Pad(0, 12, 0, 0)
            };
            _btnAddNew = UiHelper.CreateButton("Lưu Thuốc Mới Vào Kho", IconChar.Plus, UiHelper.SuccessGreen, 230, 38);
            _btnAddNew.Click += BtnAddNew_Click;
            btnFlow.Controls.Add(_btnAddNew);

            outerLayout.Controls.Add(formGrid, 0, 0);
            outerLayout.Controls.Add(btnFlow, 0, 1);

            grp.Controls.Add(outerLayout);
            return grp;
        }

        // Xây dựng khối bên phải: Nhập bổ sung tồn kho cho thuốc đã có
        private GroupBox BuildRestockGroupBox()
        {
            var grp = UiHelper.CreateGroupBox(
                "2. Nhập Bổ Sung Tồn Kho (Thuốc Đã Có)",
                UiHelper.DarkText,
                UiHelper.Pad(14, 10, 14, 12),
                UiHelper.Pad(8, 0, 0, 0));

            var outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            outerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var topGrid = UiHelper.CreateFormGrid(labelColumnBaseWidth: 145);

            _cboSelectMedicine = new ComboBox
            {
                Font = UiHelper.FontRegular,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = UiHelper.S(520),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };
            _cboSelectMedicine.SelectedIndexChanged += (_, _) => UpdateSelectedMedicineInfo();
            UiHelper.AddFormRow(topGrid, UiHelper.CreateLabel("Chọn thuốc (*):", UiHelper.FontRegular, UiHelper.DarkText), _cboSelectMedicine);

            // Thẻ hiển thị thông tin thuốc hiện tại
            var infoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = UiHelper.MutedInputBg,
                Padding = UiHelper.Pad(12, 8, 12, 8),
                Margin = UiHelper.Pad(0, 6, 0, 10)
            };
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblCurrentCode = UiHelper.CreateLabel("Mã thuốc: --", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(2, 3, 4, 3));
            _lblCurrentStock = UiHelper.CreateLabel("Tồn kho: --", UiHelper.FontBold, UiHelper.PrimaryBlue, UiHelper.Pad(2, 3, 4, 3));
            _lblCurrentPrice = UiHelper.CreateLabel("Đơn giá: --", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(2, 3, 4, 3));
            _lblCurrentExpiry = UiHelper.CreateLabel("Hạn dùng: --", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(2, 3, 4, 3));

            infoLayout.Controls.Add(_lblCurrentCode, 0, 0);
            infoLayout.Controls.Add(_lblCurrentStock, 1, 0);
            infoLayout.Controls.Add(_lblCurrentPrice, 0, 1);
            infoLayout.Controls.Add(_lblCurrentExpiry, 1, 1);

            var bottomGrid = UiHelper.CreateFormGrid(labelColumnBaseWidth: 145);

            _nudAddQty = new NumericUpDown
            {
                Font = UiHelper.FontRegular,
                Minimum = 1,
                Maximum = 100000,
                ThousandsSeparator = true,
                Value = 50,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };

            _chkUpdatePrice = new CheckBox
            {
                Text = "Cập nhật giá bán:",
                Font = UiHelper.FontRegular,
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };
            _nudRestockPrice = new NumericUpDown
            {
                Font = UiHelper.FontRegular,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 1000,
                ThousandsSeparator = true,
                Enabled = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };
            _chkUpdatePrice.CheckedChanged += (_, _) => _nudRestockPrice.Enabled = _chkUpdatePrice.Checked;

            _chkUpdateExpiry = new CheckBox
            {
                Text = "Cập nhật hạn dùng:",
                Font = UiHelper.FontRegular,
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };
            _dtpRestockExpiry = new DateTimePicker
            {
                Font = UiHelper.FontRegular,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddYears(1),
                Enabled = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 4, 0, 4)
            };
            _chkUpdateExpiry.CheckedChanged += (_, _) => _dtpRestockExpiry.Enabled = _chkUpdateExpiry.Checked;

            UiHelper.AddFormRow(bottomGrid, UiHelper.CreateLabel("Số lượng nhập (*):", UiHelper.FontRegular, UiHelper.DarkText), _nudAddQty);
            UiHelper.AddFormRow(bottomGrid, _chkUpdatePrice, _nudRestockPrice);
            UiHelper.AddFormRow(bottomGrid, _chkUpdateExpiry, _dtpRestockExpiry);

            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = UiHelper.Pad(0, 12, 0, 0)
            };
            _btnRestock = UiHelper.CreateButton("Xác Nhận Nhập Kho", IconChar.BoxesStacked, UiHelper.PrimaryBlue, 220, 38);
            _btnRestock.Click += BtnRestock_Click;
            btnFlow.Controls.Add(_btnRestock);

            var middleContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            middleContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            middleContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            middleContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            middleContainer.Controls.Add(infoLayout, 0, 0);
            middleContainer.Controls.Add(bottomGrid, 0, 1);

            outerLayout.Controls.Add(topGrid, 0, 0);
            outerLayout.Controls.Add(middleContainer, 0, 1);
            outerLayout.Controls.Add(btnFlow, 0, 2);

            grp.Controls.Add(outerLayout);
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

            if (unitPrice < 0 || stockQty < 0 || minAlert < 0)
            {
                MessageBox.Show("Đơn giá, tồn kho và mức cảnh báo phải lớn hơn hoặc bằng 0!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dtpNewExpiry.Value.Date < DateTime.Today)
            {
                var confirmExpired = MessageBox.Show(
                    "Hạn sử dụng đang chọn nhỏ hơn ngày hiện tại (thuốc đã hết hạn).\nBạn có chắc chắn muốn tiếp tục lưu không?",
                    "Cảnh báo hạn sử dụng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmExpired != DialogResult.Yes)
                {
                    _dtpNewExpiry.Focus();
                    return;
                }
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

            if (newPrice.HasValue && newPrice.Value < 0)
            {
                MessageBox.Show("Đơn giá mới phải lớn hơn hoặc bằng 0!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_chkUpdateExpiry.Checked && _dtpRestockExpiry.Value.Date < DateTime.Today)
            {
                var confirmExpired = MessageBox.Show(
                    "Hạn sử dụng mới đang chọn nhỏ hơn ngày hiện tại.\nBạn có chắc chắn muốn cập nhật không?",
                    "Cảnh báo hạn sử dụng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmExpired != DialogResult.Yes)
                {
                    return;
                }
            }

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
