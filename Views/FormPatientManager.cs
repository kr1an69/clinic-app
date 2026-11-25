using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using FontAwesome.Sharp;
using ClinicManagement.Models;
using System.Collections.Generic;

namespace ClinicManagement.Views
{
    public class FormPatientManager : BaseForm
    {
        private readonly ReceptionController _controller = new();
        private List<Patient> _patientList = new();

        private TextBox _txtSearch = null!;
        private DataGridView _dgvPatients = null!;

        private TextBox _txtId = null!;
        private TextBox _txtFullName = null!;
        private TextBox _txtPhone = null!;
        private ComboBox _cboGender = null!;
        private NumericUpDown _numBirthYear = null!;
        private TextBox _txtAddress = null!;
        
        public Patient SelectedPatient { get; private set; }

        public FormPatientManager()
        {
            InitializeComponent();
            LoadPatients();        }

        private void InitializeComponent()
        {
            this.Text = "Quản Lý Hồ Sơ Bệnh Nhân"; 
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);
            this.Padding = UiHelper.Pad(12);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpSearch = UiHelper.CreateGroupBox(
                title: "Tìm kiếm & Tra cứu hồ sơ bệnh nhân",
                margin: UiHelper.Pad(0, 0, 0, 10),
                padding: UiHelper.Pad(12, 8, 12, 10));
            grpSearch.AutoSize = true;
            grpSearch.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var searchLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1
            };
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var leftSearchFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _txtSearch = new TextBox
            {
                Width = UiHelper.S(280),
                PlaceholderText = "Gõ họ tên hoặc số điện thoại...",
                Font = UiHelper.FontRegular,
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 8, 0)
            };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchPatients();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            var btnSearch = UiHelper.CreateButton(
                text: "Tìm kiếm",
                IconChar.MagnifyingGlass,
                backColor: UiHelper.PrimaryBlue,
                minWidth: 105,
                height: 34
            );
            btnSearch.Click += (s, e) => SearchPatients();

            var btnShowAll = UiHelper.CreateButton(
                text: "Hiện tất cả",
                IconChar.Rotate,
                backColor: UiHelper.SecondaryGray,
                minWidth: 105,
                height: 34
            );
            btnShowAll.Click += (s, e) =>
            {
                _txtSearch.Clear();
                LoadPatients();
            };

            leftSearchFlow.Controls.Add(UiHelper.CreateLabel(
                "Tìm kiếm (Họ tên / SĐT):",
                font: UiHelper.FontBold,
                margin: UiHelper.Pad(0, 6, 8, 0)));
            leftSearchFlow.Controls.Add(_txtSearch);
            leftSearchFlow.Controls.Add(btnSearch);
            leftSearchFlow.Controls.Add(btnShowAll);

            var rightActionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var btnSelect = UiHelper.CreateButton(
                "Cấp STT Khám Cho BN Đang Chọn",
                IconChar.Ticket,
                backColor: UiHelper.SuccessGreen,
                minWidth: 255,
                height: 34
            );
            btnSelect.Click += (s, e) => SelectAndClose();

            var btnHistory = UiHelper.CreateButton(
                "Lịch Sử Khám",
                IconChar.ClockRotateLeft,
                backColor: UiHelper.PrimaryBlue,
                minWidth: 135,
                height: 34
            );
            btnHistory.Click += (s, e) => ShowPatientVisitHistory();
            
            rightActionFlow.Controls.Add(btnSelect);
            rightActionFlow.Controls.Add(btnHistory);

            searchLayout.Controls.Add(leftSearchFlow, 0, 0);
            searchLayout.Controls.Add(rightActionFlow, 1, 0);
            grpSearch.Controls.Add(searchLayout);

            // Bố cục chính
            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Bảng danh sách hồ sơ bệnh nhân bên trái
            var grpList = UiHelper.CreateGroupBox(
                title: "Danh Sách Hồ Sơ Bệnh Nhân Trong CSDL",
                padding: UiHelper.Pad(10),
                margin: UiHelper.Pad(0, 0, 10, 0));

            _dgvPatients = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 38);
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Mã BN", FillWeight = 45 });
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Họ và Tên", FillWeight = 135 });
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Số Điện Thoại", FillWeight = 90 });
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gender", HeaderText = "Giới Tính", FillWeight = 55 });
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "BirthYear", HeaderText = "Năm Sinh", FillWeight = 60 });
            _dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "Địa Chỉ", FillWeight = 165 });

            _dgvPatients.Columns["Id"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvPatients.Columns["Gender"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvPatients.Columns["BirthYear"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            _dgvPatients.DefaultCellStyle.ForeColor = Color.Black;
            _dgvPatients.RowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvPatients.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            _dgvPatients.DefaultCellStyle.SelectionForeColor = Color.Black;

            _dgvPatients.SelectionChanged += DgvPatients_SelectionChanged;
            _dgvPatients.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) SelectAndClose();
            };
            grpList.Controls.Add(_dgvPatients);

            // Form chi tiết / cập nhật hồ sơ bên phải
            var grpDetail = UiHelper.CreateGroupBox(
                title: "Chi Tiết / Cập Nhật Hồ Sơ",
                padding: UiHelper.Pad(12));

            var formPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 10
            };
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 10; i++)
            {
                detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            _txtId = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                BackColor = UiHelper.MutedInputBg,
                Font = UiHelper.FontBold,
                Margin = UiHelper.Pad(0, 2, 0, 10)
            };
            _txtFullName = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 2, 0, 10)
            };
            _txtPhone = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 2, 0, 10)
            };

            var genderYear = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };
            genderYear.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            genderYear.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            genderYear.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            genderYear.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _cboGender = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 2, 4, 0)
            };
            _cboGender.Items.AddRange(new object[] { "Nam", "Nữ" });
            _cboGender.SelectedIndex = -1; 

            _numBirthYear = new NumericUpDown
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 1900,
                Maximum = DateTime.Now.Year,
                Value = 1990,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(4, 2, 0, 0)
            };
            _numBirthYear.Text = string.Empty; 

            genderYear.Controls.Add(UiHelper.CreateLabel("Giới tính:", font: UiHelper.FontLabelBold), 0, 0);
            genderYear.Controls.Add(UiHelper.CreateLabel("Năm sinh:", font: UiHelper.FontLabelBold), 1, 0);
            genderYear.Controls.Add(_cboGender, 0, 1);
            genderYear.Controls.Add(_numBirthYear, 1, 1);

            _txtAddress = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                Height = UiHelper.S(76),
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 2, 0, 14)
            };

            // Cụm nút CRUD
            var btnCrud = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 4, 0, 0)
            };
            btnCrud.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnCrud.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnCrud.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            btnCrud.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var btnAdd = UiHelper.CreateButton("Thêm Mới", IconChar.Plus, UiHelper.SuccessGreen, minWidth: 110, height: 38);
            btnAdd.Dock = DockStyle.Fill;
            btnAdd.Margin = UiHelper.Pad(0, 0, 4, 6);
            btnAdd.Click += BtnAdd_Click;

            var btnUpdate = UiHelper.CreateButton("Lưu Sửa", IconChar.FloppyDisk, UiHelper.PrimaryBlue, minWidth: 110, height: 38);
            btnUpdate.Dock = DockStyle.Fill;
            btnUpdate.Margin = UiHelper.Pad(4, 0, 0, 6);
            btnUpdate.Click += BtnUpdate_Click;

            var btnDelete = UiHelper.CreateButton("Xóa Hồ Sơ", IconChar.Trash, UiHelper.DangerRed, minWidth: 110, height: 38);
            btnDelete.Dock = DockStyle.Fill;
            btnDelete.Margin = UiHelper.Pad(0, 0, 4, 0);
            btnDelete.Click += BtnDelete_Click;

            var btnClear = UiHelper.CreateButton("Nhập Mới", IconChar.RotateLeft, UiHelper.SecondaryGray, minWidth: 110, height: 38);
            btnClear.Dock = DockStyle.Fill;
            btnClear.Margin = UiHelper.Pad(4, 0, 0, 0);
            btnClear.Click += (s, e) => ClearDetailForm();

            btnCrud.Controls.Add(btnAdd, 0, 0);
            btnCrud.Controls.Add(btnUpdate, 1, 0);
            btnCrud.Controls.Add(btnDelete, 0, 1);
            btnCrud.Controls.Add(btnClear, 1, 1);

            detailLayout.Controls.Add(UiHelper.CreateLabel("Mã bệnh nhân:", font: UiHelper.FontLabelBold), 0, 0);
            detailLayout.Controls.Add(_txtId, 0, 1);
            detailLayout.Controls.Add(UiHelper.CreateLabel("Họ và tên bệnh nhân (*):", font: UiHelper.FontLabelBold), 0, 2);
            detailLayout.Controls.Add(_txtFullName, 0, 3);
            detailLayout.Controls.Add(UiHelper.CreateLabel("Số điện thoại liên hệ:", font: UiHelper.FontLabelBold), 0, 4);
            detailLayout.Controls.Add(_txtPhone, 0, 5);
            detailLayout.Controls.Add(genderYear, 0, 6);
            detailLayout.Controls.Add(UiHelper.CreateLabel("Địa chỉ thường trú:", font: UiHelper.FontLabelBold), 0, 7);
            detailLayout.Controls.Add(_txtAddress, 0, 8);
            detailLayout.Controls.Add(btnCrud, 0, 9);

            formPanel.Controls.Add(detailLayout);
            grpDetail.Controls.Add(formPanel);

            bodyLayout.Controls.Add(grpList, 0, 0);
            bodyLayout.Controls.Add(grpDetail, 1, 0);

            rootLayout.Controls.Add(grpSearch, 0, 0);
            rootLayout.Controls.Add(bodyLayout, 0, 1);
            this.Controls.Add(rootLayout);
        }

        public void LoadPatients(int? focusPatientId = null)
        {
            _patientList = _controller.GetAllPatients();
            BindGrid(_patientList, focusPatientId);
        }

        private void SearchPatients()
        {
            _patientList = _controller.SearchPatients(_txtSearch.Text.Trim());
            BindGrid(_patientList);
        }

        private void BindGrid(List<Patient> list, int? focusPatientId = null)
        {
            list = list.OrderBy(p => p.Id).ToList();
            _dgvPatients.Rows.Clear();
            int targetRowIndex = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                _dgvPatients.Rows.Add(
                    $"{p.Id:D2}",
                    p.FullName,
                    p.Phone ?? string.Empty,
                    p.Gender ?? string.Empty,
                    p.BirthYear > 0 ? p.BirthYear.ToString() : string.Empty,
                    p.Address ?? string.Empty);

                if (focusPatientId.HasValue && p.Id == focusPatientId.Value)
                {
                    targetRowIndex = i;
                }
            }

            if (_dgvPatients.Rows.Count > 0)
            {
                _dgvPatients.ClearSelection();
                _dgvPatients.Rows[targetRowIndex].Selected = true;
                DgvPatients_SelectionChanged(null, EventArgs.Empty);
            }
            else
            {
                ClearDetailForm();
            }
        }

        private void DgvPatients_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvPatients.SelectedRows.Count == 0) return;

            var row = _dgvPatients.SelectedRows[0];
            if (int.TryParse(row.Cells["Id"].Value?.ToString(), out int id))
            {
                var patient = _patientList.FirstOrDefault(p => p.Id == id);
                if (patient != null)
                {
                    SelectedPatient = patient;
                    _txtId.Text = patient.Id.ToString();
                    _txtFullName.Text = patient.FullName;
                    _txtPhone.Text = patient.Phone ?? string.Empty;

                    if (patient.Gender == "Nam" || patient.Gender == "Nữ")
                    {
                        _cboGender.SelectedItem = patient.Gender;
                    }
                    else
                    {
                        _cboGender.SelectedIndex = -1;
                    }

                    if (patient.BirthYear >= 1900 && patient.BirthYear <= DateTime.Now.Year)
                    {
                        _numBirthYear.Value = patient.BirthYear;
                        _numBirthYear.Text = patient.BirthYear.ToString();
                    }
                    else
                    {
                        _numBirthYear.Text = string.Empty;
                    }

                    _txtAddress.Text = patient.Address ?? string.Empty;
                }
            }
        }

        private void ClearDetailForm()
        {
            SelectedPatient = null;
            _txtId.Clear();
            _txtFullName.Clear();
            _txtPhone.Clear();
            _cboGender.SelectedIndex = -1;
            _numBirthYear.Text = string.Empty;
            _txtAddress.Clear();
            _txtFullName.Focus();
        }

        private bool TryParseBirthYear(out int birthYear)
        {
            birthYear = 0;
            if (string.IsNullOrWhiteSpace(_numBirthYear.Text))
            {
                return true;
            }

            if (!int.TryParse(_numBirthYear.Text.Trim(), out birthYear) || birthYear < 1900 || birthYear > DateTime.Now.Year)
            {
                MessageBox.Show($"Năm sinh không hợp lệ (phải từ 1900 đến {DateTime.Now.Year})!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _numBirthYear.Focus();
                return false;
            }

            return true;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = _txtFullName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Vui lòng nhập Họ và tên bệnh nhân!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFullName.Focus();
                return;
            }

            if (!TryParseBirthYear(out int birthYear)) return;
            string? gender = _cboGender.SelectedIndex >= 0 ? _cboGender.SelectedItem?.ToString() : null;

            try
            {
                int newId = _controller.AddPatient(new Patient
                {
                    FullName = name,
                    Phone = _txtPhone.Text.Trim(),
                    Gender = gender,
                    BirthYear = birthYear,
                    Address = _txtAddress.Text.Trim()
                });

                MessageBox.Show($"Thêm hồ sơ bệnh nhân thành công! (Mã BN: {newId})", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPatients(focusPatientId: newId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm hồ sơ bệnh nhân: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(_txtId.Text, out int id))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân cần cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = _txtFullName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Họ và tên bệnh nhân không được để trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFullName.Focus();
                return;
            }

            if (!TryParseBirthYear(out int birthYear)) return;
            string? gender = _cboGender.SelectedIndex >= 0 ? _cboGender.SelectedItem?.ToString() : null;

            try
            {
                if (_controller.UpdatePatient(new Patient
                {
                    Id = id,
                    FullName = name,
                    Phone = _txtPhone.Text.Trim(),
                    Gender = gender,
                    BirthYear = birthYear,
                    Address = _txtAddress.Text.Trim()
                }))
                {
                    MessageBox.Show("Cập nhật thông tin bệnh nhân thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadPatients(focusPatientId: id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật hồ sơ bệnh nhân: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(_txtId.Text, out int id))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa hồ sơ bệnh nhân [{_txtFullName.Text}] (Mã: {id}) không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    if (_controller.DeletePatient(id))
                    {
                        MessageBox.Show("Đã xóa hồ sơ bệnh nhân thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearDetailForm();
                        LoadPatients();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SelectAndClose()
        {
            if (SelectedPatient == null && _dgvPatients.SelectedRows.Count > 0)
            {
                DgvPatients_SelectionChanged(null, EventArgs.Empty);
            }

            if (SelectedPatient == null)
            {
                MessageBox.Show("Vui lòng chọn 1 bệnh nhân trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ShowPatientVisitHistory()
        {
            if (SelectedPatient == null && _dgvPatients.SelectedRows.Count > 0)
            {
                DgvPatients_SelectionChanged(null, EventArgs.Empty);
            }
            if (SelectedPatient == null)
            {
                MessageBox.Show("Vui lòng chọn 1 bệnh nhân để xem lịch sử khám!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var history = _controller.GetPatientVisitHistory(SelectedPatient.Id);
            if (history == null || history.Count == 0)
            {
                MessageBox.Show($"Bệnh nhân [{SelectedPatient.FullName}] chưa có lượt khám nào trong quá khứ.", "Lịch sử khám", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var dlg = new Form
            {
                Text = $"Lịch Sử Khám Bệnh - BN: {SelectedPatient.FullName} (Mã: {SelectedPatient.Id})",
                Size = new Size(680, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Padding = UiHelper.Pad(12)
            };
            
            var dgv = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 36);
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "STT Khám", FillWeight = 35 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueDate", HeaderText = "Ngày Khám", FillWeight = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueTime", HeaderText = "Giờ Đến", FillWeight = 45 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", FillWeight = 60 });
            
            dgv.Columns["QueueNumber"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["QueueDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["QueueTime"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Status"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            foreach (var item in history)
            {
                string dateStr = DateTime.TryParse(item.QueueDate.ToString(), out DateTime parsedDate)
                    ? parsedDate.ToString("dd/MM/yyyy")
                    : item.QueueDate.ToString();
                
                string timeStr = item.CreatedAt.ToString("HH:mm");
                
                string statusText = item.Status switch
                {
                    "Completed" => "Đã hoàn tất",
                    "Examining" => "Đang khám",
                    "Waiting" => "Đang chờ",
                    "PendingPayment" => "Chờ thanh toán",
                    "Cancelled" => "Đã hủy",
                    _ => item.Status
                };
                dgv.Rows.Add(
                    $"{item.QueueNumber:D2}", 
                    dateStr,                  
                    timeStr,                  
                    statusText                
                );
            }            
            
            dlg.Controls.Add(dgv);
            dlg.ShowDialog(this);
        }
    }
}  