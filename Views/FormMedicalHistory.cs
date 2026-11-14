using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormMedicalHistory : Form
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
        }

        private void InitializeComponent()
        {
            this.Text = "Tra Cứu Lịch Sử Khám & Hồ Sơ Bệnh Án";
            this.Size = new Size(1220, 740);
            this.MinimumSize = new Size(1020, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Padding = new Padding(14);

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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 215F));

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
            var grpSearch = new GroupBox
            {
                Text = "Tra Cứu Lịch Sử Khám Bệnh",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

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
                Margin = new Padding(0)
            };

            var lblSearch = new Label
            {
                Text = "Tìm theo Tên / SĐT Bệnh nhân:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 8, 0)
            };

            _txtSearchHistory = new TextBox
            {
                Width = 300,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                PlaceholderText = "Nhập họ tên hoặc số điện thoại bệnh nhân...",
                Text = _initialKeyword,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 8, 0)
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
                Margin = new Padding(0)
            };

            _lblRecordCount = new Label
            {
                Text = "Tổng số: 0 hồ sơ bệnh án",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Margin = new Padding(0, 8, 4, 0)
            };
            rightFlow.Controls.Add(_lblRecordCount);

            toolbarLayout.Controls.Add(searchFlow, 0, 0);
            toolbarLayout.Controls.Add(rightFlow, 1, 0);
            grpSearch.Controls.Add(toolbarLayout);
            return grpSearch;
        }

        // 2. Xây dựng bảng DataGridView danh sách hồ sơ bệnh án
        private GroupBox BuildRecordsGridGroup()
        {
            var grpGrid = new GroupBox
            {
                Text = "Danh Sách Hồ Sơ Bệnh Án Đã Lưu",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 12),
                Margin = new Padding(0, 0, 0, 10)
            };

            _dgvMedicalRecords = new DataGridView
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
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                RowTemplate = { Height = 34 }
            };

            _dgvMedicalRecords.ColumnHeadersDefaultCellStyle.BackColor = UiHelper.PrimaryBlue;
            _dgvMedicalRecords.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvMedicalRecords.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _dgvMedicalRecords.ColumnHeadersHeight = 36;
            _dgvMedicalRecords.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            _dgvMedicalRecords.DefaultCellStyle.SelectionForeColor = UiHelper.DarkText;

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
            var grpDetail = new GroupBox
            {
                Text = "Chi Tiết Hồ Sơ Bệnh Án Đang Chọn",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 10),
                Margin = new Padding(0)
            };

            var detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Thanh tóm tắt thông tin hành chính của ca khám
            var infoBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.FromArgb(235, 245, 251),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 0, 8)
            };
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            infoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

            _lblDetailRecordCode = new Label
            {
                Text = "Mã hồ sơ: ---",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Anchor = AnchorStyles.Left
            };

            _lblDetailPatient = new Label
            {
                Text = "Bệnh nhân: Chưa chọn hồ sơ",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left
            };

            _lblDetailDateAndDoctor = new Label
            {
                Text = "Ngày khám: --- | Bác sĩ: ---",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left
            };

            _lblDetailExamFee = new Label
            {
                Text = "Tiền khám: 0 đ",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DangerRed,
                Anchor = AnchorStyles.Right
            };

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
                Margin = new Padding(0)
            };
            notesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            notesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            notesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            notesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            notesGrid.Controls.Add(new Label
            {
                Text = "Triệu chứng lâm sàng ghi nhận:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 0, 0);

            notesGrid.Controls.Add(new Label
            {
                Text = "Kết luận chẩn đoán của bác sĩ:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 1, 0);

            _txtDetailSymptoms = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Margin = new Padding(0, 0, 8, 0)
            };

            _txtDetailDiagnosis = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                Margin = new Padding(0)
            };

            notesGrid.Controls.Add(_txtDetailSymptoms, 0, 1);
            notesGrid.Controls.Add(_txtDetailDiagnosis, 1, 1);

            detailLayout.Controls.Add(infoBar, 0, 0);
            detailLayout.Controls.Add(notesGrid, 0, 1);
            grpDetail.Controls.Add(detailLayout);
            return grpDetail;
        }
    }
}
