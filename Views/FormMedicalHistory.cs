using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    /// <summary>
    /// SCREEN #08: TRA CỨU LỊCH SỬ KHÁM & HỒ SƠ BỆNH ÁN
    /// Chuẩn hóa theo BaseForm và UiHelper (DPI-aware).
    /// </summary>
    public class FormMedicalHistory : BaseForm
    {
        private readonly DoctorController _controller = new();
        private readonly string _initialKeyword;

        // Các control khu vực tìm kiếm phía trên
        private TextBox _txtSearchHistory = null!;
        private IconButton _btnSearch = null!;
        private IconButton _btnRefresh = null!;
        private Label _lblRecordCount = null!;

        // Bảng danh sách hồ sơ bệnh án
        private DataGridView _dgvMedicalRecords = null!;

        // Các control khu vực xem chi tiết bệnh án được chọn phía dưới
        private Label _lblDetailRecordCode = null!;
        private Label _lblDetailPatient = null!;
        private Label _lblDetailDateAndDoctor = null!;
        private Label _lblDetailExamFee = null!;
        private TextBox _txtDetailSymptoms = null!;
        private TextBox _txtDetailDiagnosis = null!;

        public FormMedicalHistory(string initialKeyword = "")
        {
            _initialKeyword = initialKeyword;
            InitializeComponent();
            RegisterEvents();
            LoadMedicalRecords(_initialKeyword);
        }

        private void InitializeComponent()
        {
            this.Text = "Tra Cứu Lịch Sử Khám & Hồ Sơ Bệnh Án";
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);
            this.Padding = UiHelper.Pad(14);

            // Bố cục chính chia 3 hàng: Thanh tìm kiếm, Bảng danh sách bệnh án, Khung chi tiết bệnh án
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(220)));

            var grpSearch = BuildSearchGroup();
            var grpRecordsGrid = BuildRecordsGridGroup();
            var grpRecordDetail = BuildRecordDetailGroup();

            mainLayout.Controls.Add(grpSearch, 0, 0);
            mainLayout.Controls.Add(grpRecordsGrid, 0, 1);
            mainLayout.Controls.Add(grpRecordDetail, 0, 2);

            this.Controls.Add(mainLayout);
        }

        // 1. Xây dựng thanh công cụ tìm kiếm theo Tên hoặc SĐT bệnh nhân
        private GroupBox BuildSearchGroup()
        {
            var grpSearch = UiHelper.CreateGroupBox(
                "Tra Cứu Lịch Sử Khám Bệnh",
                titleColor: UiHelper.DarkText,
                padding: UiHelper.Pad(12, 8, 12, 10),
                margin: UiHelper.Pad(0, 0, 0, 10)
            );
            grpSearch.Dock = DockStyle.Top;
            grpSearch.AutoSize = true;

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var searchFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var lblSearch = UiHelper.CreateLabel(
                text: "Tìm theo Tên / SĐT Bệnh nhân:",
                font: UiHelper.FontRegular,
                margin: UiHelper.Pad(0, 6, 8, 0));
            lblSearch.Anchor = AnchorStyles.Left;

            _txtSearchHistory = new TextBox
            {
                Width = UiHelper.S(300),
                Font = UiHelper.FontRegular,
                PlaceholderText = "Nhập họ tên hoặc số điện thoại bệnh nhân...",
                Text = _initialKeyword,
                Anchor = AnchorStyles.Left,
                Margin = UiHelper.Pad(0, 4, 8, 0)
            };

            _btnSearch = UiHelper.CreateButton("Tra cứu bệnh án", IconChar.MagnifyingGlass, UiHelper.PrimaryBlue, 165, 34);
            _btnRefresh = UiHelper.CreateButton("Hiển thị tất cả", IconChar.Rotate, Color.FromArgb(52, 73, 94), 155, 34);

            searchFlow.Controls.Add(lblSearch);
            searchFlow.Controls.Add(_txtSearchHistory);
            searchFlow.Controls.Add(_btnSearch);
            searchFlow.Controls.Add(_btnRefresh);

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };

            _lblRecordCount = UiHelper.CreateLabel(
                text: "Tổng số: 0 hồ sơ bệnh án",
                font: UiHelper.FontBold,
                foreColor: UiHelper.PrimaryBlue,
                margin: UiHelper.Pad(0, 8, 4, 0));
            rightFlow.Controls.Add(_lblRecordCount);

            toolbarLayout.Controls.Add(searchFlow, 0, 0);
            toolbarLayout.Controls.Add(rightFlow, 1, 0);
            grpSearch.Controls.Add(toolbarLayout);
            return grpSearch;
        }

        // 2. Xây dựng bảng DataGridView danh sách hồ sơ bệnh án
        private GroupBox BuildRecordsGridGroup()
        {
            var grpGrid = UiHelper.CreateGroupBox(
                "Danh Sách Hồ Sơ Bệnh Án Đã Lưu",
                padding: UiHelper.Pad(12, 8, 12, 12),
                margin: UiHelper.Pad(0, 0, 0, 10)
            );

            _dgvMedicalRecords = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 36);
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordCode", HeaderText = "Mã Hồ Sơ", FillWeight = 13 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Ngày Giờ Khám", FillWeight = 19 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Họ Tên Bệnh Nhân", FillWeight = 23 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Số Điện Thoại", FillWeight = 15 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "Symptoms", HeaderText = "Triệu Chứng Lâm Sàng", FillWeight = 32 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "Chẩn Đoán Bệnh", FillWeight = 28 });
            _dgvMedicalRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExamFee", HeaderText = "Tiền Khám", FillWeight = 14 });

            grpGrid.Controls.Add(_dgvMedicalRecords);
            return grpGrid;
        }

        // 3. Xây dựng khung xem chi tiết hồ sơ bệnh án đang chọn ở phía dưới
        private GroupBox BuildRecordDetailGroup()
        {
            var grpDetail = UiHelper.CreateGroupBox(
                "Chi Tiết Hồ Sơ Bệnh Án Đang Chọn",
                titleColor: UiHelper.DarkText,
                padding: UiHelper.Pad(12, 8, 12, 10),
                margin: Padding.Empty
            );

            var detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Thanh tóm tắt thông tin hành chính của ca khám
            var infoBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.FromArgb(235, 245, 251),
                Padding = UiHelper.Pad(10, 6, 10, 6),
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

            _lblDetailRecordCode = UiHelper.CreateLabel("Mã hồ sơ: ---", font: UiHelper.FontBold, foreColor: UiHelper.PrimaryBlue);
            _lblDetailRecordCode.Anchor = AnchorStyles.Left;

            _lblDetailPatient = UiHelper.CreateLabel("Bệnh nhân: Chưa chọn hồ sơ", font: UiHelper.FontBold);
            _lblDetailPatient.Anchor = AnchorStyles.Left;

            _lblDetailDateAndDoctor = UiHelper.CreateLabel("Ngày khám: --- | Bác sĩ: ---", font: UiHelper.FontRegular);
            _lblDetailDateAndDoctor.Anchor = AnchorStyles.Left;

            _lblDetailExamFee = UiHelper.CreateLabel("Tiền khám: 0 đ", font: UiHelper.FontBold, foreColor: UiHelper.DangerRed);
            _lblDetailExamFee.Anchor = AnchorStyles.Right;

            infoBar.Controls.Add(_lblDetailRecordCode, 0, 0);
            infoBar.Controls.Add(_lblDetailPatient, 1, 0);
            infoBar.Controls.Add(_lblDetailDateAndDoctor, 2, 0);
            infoBar.Controls.Add(_lblDetailExamFee, 3, 0);

            // Hai cột hiển thị đầy đủ Triệu chứng lâm sàng và Chẩn đoán
            var notesGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = Padding.Empty
            };
            notesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            notesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            notesGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            notesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblSymptomsHeader = UiHelper.CreateLabel("Triệu chứng lâm sàng ghi nhận:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));
            var lblDiagnosisHeader = UiHelper.CreateLabel("Kết luận chẩn đoán của bác sĩ:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));

            _txtDetailSymptoms = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 0, 8, 0)
            };

            _txtDetailDiagnosis = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = UiHelper.FontBold,
                ForeColor = UiHelper.DarkText,
                Margin = Padding.Empty
            };

            notesGrid.Controls.Add(lblSymptomsHeader, 0, 0);
            notesGrid.Controls.Add(lblDiagnosisHeader, 1, 0);
            notesGrid.Controls.Add(_txtDetailSymptoms, 0, 1);
            notesGrid.Controls.Add(_txtDetailDiagnosis, 1, 1);

            detailLayout.Controls.Add(infoBar, 0, 0);
            detailLayout.Controls.Add(notesGrid, 0, 1);
            grpDetail.Controls.Add(detailLayout);
            return grpDetail;
        }

        // --- CÁC PHƯƠNG THỨC NGHIỆP VỤ & SỰ KIỆN ---

        private void RegisterEvents()
        {
            _btnSearch.Click += (_, _) => LoadMedicalRecords(_txtSearchHistory.Text.Trim());
            _btnRefresh.Click += (_, _) =>
            {
                _txtSearchHistory.Clear();
                LoadMedicalRecords();
            };

            _txtSearchHistory.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    LoadMedicalRecords(_txtSearchHistory.Text.Trim());
                }
            };

            _dgvMedicalRecords.SelectionChanged += (_, _) => OnRecordSelectionChanged();
        }

        // Tải danh sách lịch sử bệnh án từ CSDL theo từ khóa
        public void LoadMedicalRecords(string keyword = "")
        {
            try
            {
                var records = _controller.GetMedicalRecordsHistory(keyword);
                _dgvMedicalRecords.Rows.Clear();

                foreach (var rec in records)
                {
                    string recordCode = Convert.ToString(rec.RecordCode) ?? $"BA-{rec.Id:D3}";
                    string createdAt = FormatDateTime(rec.CreatedAt);
                    string patientName = Convert.ToString(rec.PatientName) ?? string.Empty;
                    string phone = Convert.ToString(rec.Phone) ?? "---";
                    string symptoms = Convert.ToString(rec.Symptoms) ?? string.Empty;
                    string diagnosis = Convert.ToString(rec.Diagnosis) ?? string.Empty;
                    double examFee = rec.ExamFee != null ? Convert.ToDouble(rec.ExamFee) : 50000;

                    int rowIdx = _dgvMedicalRecords.Rows.Add(
                        recordCode,
                        createdAt,
                        patientName,
                        phone,
                        symptoms,
                        diagnosis,
                        $"{examFee:N0} đ"
                    );
                    _dgvMedicalRecords.Rows[rowIdx].Tag = rec;
                }

                _lblRecordCount.Text = $"Tổng số: {records.Count} hồ sơ bệnh án";

                if (_dgvMedicalRecords.Rows.Count > 0)
                {
                    _dgvMedicalRecords.Rows[0].Selected = true;
                    OnRecordSelectionChanged();
                }
                else
                {
                    ResetDetailPanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tra cứu lịch sử bệnh án: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hiển thị thông tin chi tiết khi chọn một dòng hồ sơ trên bảng
        private void OnRecordSelectionChanged()
        {
            if (_dgvMedicalRecords.SelectedRows.Count == 0)
            {
                ResetDetailPanel();
                return;
            }

            var row = _dgvMedicalRecords.SelectedRows[0];
            if (row.Tag == null)
            {
                ResetDetailPanel();
                return;
            }

            dynamic rec = row.Tag;
            string recordCode = Convert.ToString(rec.RecordCode) ?? $"BA-{rec.Id:D3}";
            string patientName = Convert.ToString(rec.PatientName) ?? string.Empty;
            string gender = Convert.ToString(rec.Gender) ?? "---";
            string birthYear = rec.BirthYear != null ? Convert.ToString(rec.BirthYear) : "---";
            string phone = Convert.ToString(rec.Phone) ?? "---";
            string createdAt = FormatDateTime(rec.CreatedAt);
            string doctorName = Convert.ToString(rec.DoctorName) ?? "BS.CKI Nguyễn Văn An";
            double examFee = rec.ExamFee != null ? Convert.ToDouble(rec.ExamFee) : 50000;

            _lblDetailRecordCode.Text = $"Mã hồ sơ: {recordCode}";
            _lblDetailPatient.Text = $"BN: {patientName} ({gender} - {birthYear}) | SĐT: {phone}";
            _lblDetailDateAndDoctor.Text = $"Ngày khám: {createdAt} | BS: {doctorName}";
            _lblDetailExamFee.Text = $"Tiền khám: {examFee:N0} đ";

            _txtDetailSymptoms.Text = Convert.ToString(rec.Symptoms) ?? string.Empty;
            _txtDetailDiagnosis.Text = Convert.ToString(rec.Diagnosis) ?? string.Empty;
        }

        private void ResetDetailPanel()
        {
            _lblDetailRecordCode.Text = "Mã hồ sơ: ---";
            _lblDetailPatient.Text = "Bệnh nhân: Không có hồ sơ phù hợp";
            _lblDetailDateAndDoctor.Text = "Ngày khám: --- | Bác sĩ: ---";
            _lblDetailExamFee.Text = "Tiền khám: 0 đ";
            _txtDetailSymptoms.Clear();
            _txtDetailDiagnosis.Clear();
        }

        private static string FormatDateTime(object? rawValue)
        {
            if (rawValue == null)
            {
                return string.Empty;
            }

            if (rawValue is DateTime dt)
            {
                return dt.ToString("dd/MM/yyyy HH:mm");
            }

            string str = Convert.ToString(rawValue) ?? string.Empty;
            if (DateTime.TryParse(str, out DateTime parsed))
            {
                return parsed.ToString("dd/MM/yyyy HH:mm");
            }

            return str;
        }
    }
}
