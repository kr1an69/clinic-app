using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    /// <summary>
    /// BÁC SĨ KHÁM BỆNH & KÊ ĐƠN THUỐC AUTOCOMPLETE
    /// Chuẩn hóa theo BaseForm và UiHelper (DPI-aware).
    /// </summary>
    public class FormDoctor : BaseForm
    {
        private readonly DoctorController _controller = new();
        private QueueItem? _currentPatient;
        private List<Medicine> _availableMedicines = new();
        private readonly List<PrescriptionDetail> _prescriptionDetails = new();

        // Các control thanh điều hướng phụ phía trên
        private IconButton _btnOpenQueue = null!;
        private IconButton _btnOpenHistory = null!;
        private IconButton _btnOpenPrescriptionList = null!;
        private IconButton _btnOpenMedicineView = null!;

        // Các control bên phần Khám lâm sàng & Chẩn đoán
        private Label _lblPatientName = null!;
        private Label _lblPatientMeta = null!;
        private Label _lblQueueStatus = null!;
        private TextBox _txtSymptoms = null!;
        private TextBox _txtDiagnosis = null!;
        private NumericUpDown _nudExamFee = null!;
        private TextBox _txtDoctorName = null!;
        private Label _lblSummaryExamFee = null!;
        private Label _lblSummaryMedTotal = null!;
        private Label _lblSummaryGrandTotal = null!;
        private IconButton _btnSaveComplete = null!;

        // Các control bên phần Kê đơn thuốc (AutoComplete) & Kiểm tra tồn kho
        private TextBox _txtMedSearch = null!;
        private TextBox _txtStockStatus = null!;
        private NumericUpDown _nudMedQty = null!;
        private ComboBox _cboMedDosage = null!;
        private IconButton _btnAddMed = null!;
        private IconButton _btnRemoveMed = null!;
        private DataGridView _dgvPrescription = null!;

        public FormDoctor(QueueItem? initialPatient = null)
        {
            _currentPatient = initialPatient;
            InitializeComponent();
            RegisterEvents();
            LoadInitialData();
        }

        private void InitializeComponent()
        {
            this.Text = "Bác Sĩ Khám Bệnh & Kê Đơn Thuốc";
            this.Dock = DockStyle.Fill;
            this.Padding = UiHelper.Pad(12);

            // Bố cục chính chia 2 hàng: Thanh điều hướng phụ phía trên và Khu vực khám 2 cột phía dưới
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 1. Thanh công cụ điều hướng của Bác sĩ
            var grpToolbar = BuildTopToolbar();

            // 2. Khu vực làm việc chính 2 cột (Cột trái: Khám lâm sàng, Cột phải: Kê đơn thuốc)
            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpClinicalExam = BuildClinicalExamGroup();
            var grpPrescription = BuildPrescriptionGroup();

            bodyLayout.Controls.Add(grpClinicalExam, 0, 0);
            bodyLayout.Controls.Add(grpPrescription, 1, 0);

            mainLayout.Controls.Add(grpToolbar, 0, 0);
            mainLayout.Controls.Add(bodyLayout, 0, 1);
            this.Controls.Add(mainLayout);
        }

        // Xây dựng thanh công cụ điều hướng tới các màn hình phụ của Bác sĩ
        private GroupBox BuildTopToolbar()
        {
            var grpToolbar = UiHelper.CreateGroupBox(
                "Điều Phối Khám Bệnh & Tra Cứu Nhanh",
                titleColor: UiHelper.DarkText,
                padding: UiHelper.Pad(12, 6, 12, 8),
                margin: UiHelper.Pad(0, 0, 0, 8)
            );
            grpToolbar.Dock = DockStyle.Top;
            grpToolbar.AutoSize = true;

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Nút chọn bệnh nhân từ Hàng đợi bên trái
            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            _btnOpenQueue = UiHelper.CreateButton("Hàng Đợi & Gọi Khám", IconChar.BellConcierge, UiHelper.PrimaryBlue, 180, 36);
            _btnOpenQueue.Margin = UiHelper.Pad(0, 0, 8, 0);
            leftFlow.Controls.Add(_btnOpenQueue);

            // Các nút mở màn hình tra cứu bên phải
            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };

            _btnOpenMedicineView = UiHelper.CreateButton("Tra Cứu Thuốc & Tồn Kho", IconChar.Capsules, Color.FromArgb(52, 73, 94), 195, 36);
            _btnOpenMedicineView.Margin = UiHelper.Pad(6, 0, 0, 0);

            _btnOpenPrescriptionList = UiHelper.CreateButton("Đơn Thuốc Đã Kê", IconChar.FilePrescription, UiHelper.WarningOrange, 160, 36);
            _btnOpenPrescriptionList.Margin = UiHelper.Pad(6, 0, 0, 0);

            _btnOpenHistory = UiHelper.CreateButton("Lịch Sử Bệnh Án", IconChar.NotesMedical, UiHelper.PrimaryBlue, 155, 36);
            _btnOpenHistory.Margin = UiHelper.Pad(6, 0, 0, 0);

            rightFlow.Controls.Add(_btnOpenMedicineView);
            rightFlow.Controls.Add(_btnOpenPrescriptionList);
            rightFlow.Controls.Add(_btnOpenHistory);

            toolbarLayout.Controls.Add(leftFlow, 0, 0);
            toolbarLayout.Controls.Add(rightFlow, 1, 0);
            grpToolbar.Controls.Add(toolbarLayout);
            return grpToolbar;
        }

        // Xây dựng cột trái: Khám lâm sàng & Chẩn đoán
        private GroupBox BuildClinicalExamGroup()
        {
            var grpClinical = UiHelper.CreateGroupBox(
                "1. Khám Lâm Sàng & Chẩn Đoán",
                padding: UiHelper.Pad(12),
                margin: UiHelper.Pad(0, 0, 10, 0)
            );

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Thẻ thông tin BN AutoSize
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));               // Triệu chứng co giãn
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));               // Chẩn đoán co giãn
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Tiền khám & Bác sĩ AutoSize
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Tổng kết chi phí AutoSize
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Nút Hoàn tất khám AutoSize

            // Hộp hiển thị thông tin bệnh nhân đang khám
            var patientBanner = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.FromArgb(235, 245, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = UiHelper.Pad(10, 8, 10, 8),
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };

            _lblQueueStatus = UiHelper.CreateLabel(
                text: "Chưa chọn BN",
                font: UiHelper.FontSmallBold,
                foreColor: Color.FromArgb(40, 116, 166),
                margin: Padding.Empty);
            _lblQueueStatus.Dock = DockStyle.Right;
            _lblQueueStatus.BackColor = Color.FromArgb(214, 234, 248);
            _lblQueueStatus.Padding = UiHelper.Pad(6, 4, 6, 4);
            _lblQueueStatus.TextAlign = ContentAlignment.MiddleCenter;

            _lblPatientName = UiHelper.CreateLabel(
                text: "Chưa chọn bệnh nhân khám",
                font: new Font("Segoe UI", 10.5F, FontStyle.Bold),
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 3));
            _lblPatientName.Dock = DockStyle.Top;

            _lblPatientMeta = UiHelper.CreateLabel(
                text: "Vui lòng chọn bệnh nhân từ Hàng đợi để bắt đầu khám",
                font: UiHelper.FontSmall,
                foreColor: UiHelper.SecondaryGray,
                margin: Padding.Empty);
            _lblPatientMeta.Dock = DockStyle.Bottom;

            patientBanner.Controls.Add(_lblQueueStatus);
            patientBanner.Controls.Add(_lblPatientName);
            patientBanner.Controls.Add(_lblPatientMeta);

            // Ô nhập Triệu chứng lâm sàng
            var pnlSymptoms = new Panel { Dock = DockStyle.Fill, Margin = UiHelper.Pad(0, 0, 0, 8) };
            var lblSymptoms = UiHelper.CreateLabel(
                text: "Triệu chứng lâm sàng (*):",
                font: UiHelper.FontBold,
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 4));
            lblSymptoms.Dock = DockStyle.Top;

            _txtSymptoms = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiHelper.FontRegular,
                PlaceholderText = "Nhập triệu chứng bệnh nhân gặp phải (ví dụ: Sốt 38.5 độ, đau đầu, ho khan...)"
            };
            pnlSymptoms.Controls.Add(_txtSymptoms);
            pnlSymptoms.Controls.Add(lblSymptoms);

            // Ô nhập Chẩn đoán bệnh
            var pnlDiagnosis = new Panel { Dock = DockStyle.Fill, Margin = UiHelper.Pad(0, 0, 0, 8) };
            var lblDiagnosis = UiHelper.CreateLabel(
                text: "Chẩn đoán bệnh (*):",
                font: UiHelper.FontBold,
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 4));
            lblDiagnosis.Dock = DockStyle.Top;

            _txtDiagnosis = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiHelper.FontRegular,
                PlaceholderText = "Nhập kết luận chẩn đoán của bác sĩ (ví dụ: Viêm họng cấp / Cảm sốt...)"
            };
            pnlDiagnosis.Controls.Add(_txtDiagnosis);
            pnlDiagnosis.Controls.Add(lblDiagnosis);

            // Hàng Tiền công khám & Bác sĩ khám
            var feeDoctorGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 0, 0, 6)
            };
            feeDoctorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            feeDoctorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            feeDoctorGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            feeDoctorGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblExamFeeTitle = UiHelper.CreateLabel(
                text: "Tiền công khám (VNĐ):",
                font: UiHelper.FontLabelBold,
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 3));

            var lblDoctorTitle = UiHelper.CreateLabel(
                text: "Bác sĩ phụ trách:",
                font: UiHelper.FontLabelBold,
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 3));

            _nudExamFee = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 10000000,
                Increment = 10000,
                ThousandsSeparator = true,
                Value = 50000,
                Font = UiHelper.FontBold,
                Margin = UiHelper.Pad(0, 0, 8, 0)
            };

            _txtDoctorName = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = AppSession.CurrentUser?.FullName ?? "BS.CKI Nguyễn Văn An",
                BackColor = UiHelper.MutedInputBg,
                Font = UiHelper.FontRegular
            };

            feeDoctorGrid.Controls.Add(lblExamFeeTitle, 0, 0);
            feeDoctorGrid.Controls.Add(lblDoctorTitle, 1, 0);
            feeDoctorGrid.Controls.Add(_nudExamFee, 0, 1);
            feeDoctorGrid.Controls.Add(_txtDoctorName, 1, 1);

            // Khung tổng kết tiền khám & tiền thuốc
            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = UiHelper.Pad(8, 6, 8, 6),
                Margin = UiHelper.Pad(0, 0, 0, 8)
            };
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblSumExamTitle = UiHelper.CreateLabel("Tiền khám lâm sàng:", font: UiHelper.FontRegular, margin: UiHelper.Pad(0, 2, 0, 2));
            lblSumExamTitle.Anchor = AnchorStyles.Left;
            summaryPanel.Controls.Add(lblSumExamTitle, 0, 0);

            _lblSummaryExamFee = UiHelper.CreateLabel("50,000 đ", font: UiHelper.FontBold, margin: UiHelper.Pad(0, 2, 0, 2));
            _lblSummaryExamFee.Anchor = AnchorStyles.Right;
            summaryPanel.Controls.Add(_lblSummaryExamFee, 1, 0);

            var lblSumMedTitle = UiHelper.CreateLabel("Tổng tiền thuốc trong đơn:", font: UiHelper.FontRegular, margin: UiHelper.Pad(0, 2, 0, 2));
            lblSumMedTitle.Anchor = AnchorStyles.Left;
            summaryPanel.Controls.Add(lblSumMedTitle, 0, 1);

            _lblSummaryMedTotal = UiHelper.CreateLabel("0 đ", font: new Font("Segoe UI", 10F, FontStyle.Bold), foreColor: UiHelper.DangerRed, margin: UiHelper.Pad(0, 2, 0, 2));
            _lblSummaryMedTotal.Anchor = AnchorStyles.Right;
            summaryPanel.Controls.Add(_lblSummaryMedTotal, 1, 1);

            var lblSumGrandTitle = UiHelper.CreateLabel("TỔNG CỘNG DỰ KIẾN:", font: UiHelper.FontBold, foreColor: UiHelper.PrimaryBlue, margin: UiHelper.Pad(0, 3, 0, 3));
            lblSumGrandTitle.Anchor = AnchorStyles.Left;
            summaryPanel.Controls.Add(lblSumGrandTitle, 0, 2);

            _lblSummaryGrandTotal = UiHelper.CreateLabel("50,000 đ", font: new Font("Segoe UI", 10.5F, FontStyle.Bold), foreColor: UiHelper.PrimaryBlue, margin: UiHelper.Pad(0, 3, 0, 3));
            _lblSummaryGrandTotal.Anchor = AnchorStyles.Right;
            summaryPanel.Controls.Add(_lblSummaryGrandTotal, 1, 2);

            // Nút Hoàn tất khám & Chuyển thu ngân
            _btnSaveComplete = UiHelper.CreateButton(
                "HOÀN TẤT KHÁM & CHUYỂN THU NGÂN",
                IconChar.FloppyDisk,
                UiHelper.SuccessGreen,
                240,
                44);
            _btnSaveComplete.AutoSize = false;
            _btnSaveComplete.Dock = DockStyle.Fill;
            _btnSaveComplete.Margin = Padding.Empty;

            layout.Controls.Add(patientBanner, 0, 0);
            layout.Controls.Add(pnlSymptoms, 0, 1);
            layout.Controls.Add(pnlDiagnosis, 0, 2);
            layout.Controls.Add(feeDoctorGrid, 0, 3);
            layout.Controls.Add(summaryPanel, 0, 4);
            layout.Controls.Add(_btnSaveComplete, 0, 5);

            grpClinical.Controls.Add(layout);
            return grpClinical;
        }

        // Xây dựng cột phải: Kê đơn thuốc (AutoComplete) & Kiểm tra tồn kho
        private GroupBox BuildPrescriptionGroup()
        {
            var grpPrescription = UiHelper.CreateGroupBox(
                "2. Kê Đơn Thuốc (Gợi Ý Tự Động & Kiểm Tra Tồn Kho)",
                padding: UiHelper.Pad(12),
                margin: Padding.Empty
            );

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));                      // Khung nhập thuốc AutoSize theo DPI
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));                 // Bảng chi tiết đơn thuốc co giãn

            // Khung nhập thuốc có gợi ý AutoComplete
            var inputBox = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.FromArgb(244, 249, 253),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = UiHelper.Pad(10, 8, 10, 8),
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };

            var inputGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2
            };
            inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inputGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inputGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Dòng 1: Ô tìm thuốc AutoComplete + Ô hiển thị trạng thái tồn kho
            var row1Grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = UiHelper.Pad(0, 0, 0, 6)
            };
            row1Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            row1Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            row1Grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row1Grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblSearchTitle = UiHelper.CreateLabel("Gõ tên thuốc hoặc mã thuốc (Gợi ý tự động):", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));
            var lblStockTitle = UiHelper.CreateLabel("Tồn kho & Đơn giá hiện tại:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));

            _txtMedSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = UiHelper.FontRegular,
                PlaceholderText = "Gõ tên thuốc (ví dụ: Paracetamol, Amoxicillin...)",
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                Margin = UiHelper.Pad(0, 0, 8, 0)
            };

            _txtStockStatus = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = "Chưa chọn thuốc",
                BackColor = UiHelper.MutedInputBg,
                ForeColor = UiHelper.SecondaryGray,
                Font = UiHelper.FontBold
            };

            row1Grid.Controls.Add(lblSearchTitle, 0, 0);
            row1Grid.Controls.Add(lblStockTitle, 1, 0);
            row1Grid.Controls.Add(_txtMedSearch, 0, 1);
            row1Grid.Controls.Add(_txtStockStatus, 1, 1);

            // Dòng 2: Số lượng + Liều dùng + Nút Thêm vào đơn + Nút Xóa dòng
            var row2Grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 2,
                Margin = Padding.Empty
            };
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiHelper.SF(105))); // Số lượng theo DPI
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));             // Liều dùng
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));                  // Nút Thêm
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));                  // Nút Xóa
            row2Grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row2Grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblQtyTitle = UiHelper.CreateLabel("Số lượng kê:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));
            var lblDosageTitle = UiHelper.CreateLabel("Liều dùng / Cách dùng:", font: UiHelper.FontLabelBold, margin: UiHelper.Pad(0, 0, 0, 3));

            _nudMedQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = UiHelper.FontBold,
                Margin = UiHelper.Pad(0, 2, 8, 0)
            };

            _cboMedDosage = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = UiHelper.FontRegular,
                Margin = UiHelper.Pad(0, 2, 8, 0)
            };
            _cboMedDosage.Items.AddRange(new object[]
            {
                "Sáng 1 viên, Tối 1 viên sau ăn",
                "Sáng 1 viên, Trưa 1 viên, Tối 1 viên sau ăn",
                "Sáng 1 viên, Chiều 1 viên sau ăn",
                "Uống 1 viên khi sốt >= 38.5 độ (cách nhau 4-6h)",
                "Ngày uống 2 lần, mỗi lần 1 gói sau ăn"
            });
            _cboMedDosage.SelectedIndex = 0;

            _btnAddMed = UiHelper.CreateButton("Thêm vào đơn", IconChar.CartPlus, UiHelper.PrimaryBlue, 140, 32, 18);
            _btnAddMed.Margin = UiHelper.Pad(0, 0, 6, 0);

            _btnRemoveMed = UiHelper.CreateButton("Xóa thuốc", IconChar.Trash, UiHelper.DangerRed, 110, 32, 18);
            _btnRemoveMed.Margin = Padding.Empty;

            row2Grid.Controls.Add(lblQtyTitle, 0, 0);
            row2Grid.Controls.Add(lblDosageTitle, 1, 0);
            row2Grid.Controls.Add(_nudMedQty, 0, 1);
            row2Grid.Controls.Add(_cboMedDosage, 1, 1);
            row2Grid.Controls.Add(_btnAddMed, 2, 1);
            row2Grid.Controls.Add(_btnRemoveMed, 3, 1);

            inputGrid.Controls.Add(row1Grid, 0, 0);
            inputGrid.Controls.Add(row2Grid, 0, 1);
            inputBox.Controls.Add(inputGrid);

            // Bảng DataGridView hiển thị danh sách thuốc trong đơn dùng chuẩn UiHelper.CreateStandardGrid
            _dgvPrescription = UiHelper.CreateStandardGrid(baseRowHeight: 34, baseHeaderHeight: 40);
            _dgvPrescription.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "STT", FillWeight = 10 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedicineName", HeaderText = "Tên Thuốc & Hàm Lượng", FillWeight = 34 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", FillWeight = 12 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Số Lượng", FillWeight = 16 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn Giá", FillWeight = 16 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dosage", HeaderText = "Liều Dùng Chỉ Định", FillWeight = 36 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "Thành Tiền", FillWeight = 18 });

            layout.Controls.Add(inputBox, 0, 0);
            layout.Controls.Add(_dgvPrescription, 0, 1);

            grpPrescription.Controls.Add(layout);
            return grpPrescription;
        }

        // --- CÁC PHƯƠNG THỨC NGHIỆP VỤ & SỰ KIỆN ---

        private void RegisterEvents()
        {
            _txtMedSearch.TextChanged += (_, _) => UpdateMedicineStockPreview();
            _nudMedQty.ValueChanged += (_, _) => UpdateMedicineStockPreview();
            _nudExamFee.ValueChanged += (_, _) => UpdateFinancialSummary();

            _btnAddMed.Click += (_, _) => HandleAddMedicineToPrescription();
            _btnRemoveMed.Click += (_, _) => HandleRemoveSelectedMedicine();
            _btnSaveComplete.Click += (_, _) => HandleSaveMedicalRecordAndPrescription();

            // Sự kiện điều hướng mở các màn hình của Bác sĩ
            _btnOpenQueue.Click += (_, _) => OpenDoctorQueueDialog();
            _btnOpenHistory.Click += (_, _) => OpenMedicalHistoryDialog();
            _btnOpenPrescriptionList.Click += (_, _) => OpenPrescriptionListDialog();
            _btnOpenMedicineView.Click += (_, _) => OpenMedicineViewDialog();
        }

        // Nạp danh mục thuốc AutoComplete và bệnh nhân đang khám ban đầu
        public void LoadInitialData()
        {
            LoadMedicinesForAutoComplete();

            if (_currentPatient == null)
            {
                var waitingList = _controller.GetPatientsWaitingForDoctor();
                // Ưu tiên chọn bệnh nhân đang ở trạng thái Examining, nếu chưa có thì lấy người đầu tiên đang Waiting
                _currentPatient = waitingList.FirstOrDefault(q => q.Status == "Examining")
                                  ?? waitingList.FirstOrDefault(q => q.Status == "Waiting");
            }

            BindCurrentPatientInfo();
            UpdateFinancialSummary();
        }

        // Nạp danh sách thuốc còn tồn kho vào bộ gợi ý AutoComplete của TextBox
        private void LoadMedicinesForAutoComplete()
        {
            try
            {
                _availableMedicines = _controller.GetAllMedicinesForPrescription();
                var autoComplete = new AutoCompleteStringCollection();
                foreach (var med in _availableMedicines)
                {
                    autoComplete.Add(med.Name);
                    autoComplete.Add($"{med.Code} - {med.Name}");
                }
                _txtMedSearch.AutoCompleteCustomSource = autoComplete;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nạp danh mục thuốc gợi ý: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hiển thị thông tin bệnh nhân đang được bác sĩ khám
        public void SetExaminingPatient(QueueItem patient)
        {
            _currentPatient = patient;
            BindCurrentPatientInfo();
        }

        private void BindCurrentPatientInfo()
        {
            if (_currentPatient != null)
            {
                _lblPatientName.Text = $"STT {_currentPatient.QueueNumber:D2}: {_currentPatient.PatientName}";
                _lblPatientMeta.Text = $"Mã BN: #{_currentPatient.PatientId} | SĐT: {_currentPatient.Phone ?? "---"} | Ngày khám: {_currentPatient.QueueDate}";
                _lblQueueStatus.Text = _currentPatient.Status == "Examining" ? "Đang khám" : "Đang chờ";
                _lblQueueStatus.BackColor = _currentPatient.Status == "Examining"
                    ? Color.FromArgb(212, 239, 223)
                    : Color.FromArgb(254, 249, 231);
                _lblQueueStatus.ForeColor = _currentPatient.Status == "Examining"
                    ? Color.FromArgb(30, 132, 73)
                    : Color.FromArgb(183, 149, 11);
            }
            else
            {
                _lblPatientName.Text = "Hiện chưa có bệnh nhân trong hàng đợi";
                _lblPatientMeta.Text = "Bấm nút 'Hàng Đợi & Gọi Khám' phía trên để chọn bệnh nhân";
                _lblQueueStatus.Text = "Trống";
                _lblQueueStatus.BackColor = Color.FromArgb(237, 242, 247);
                _lblQueueStatus.ForeColor = UiHelper.SecondaryGray;
            }
        }

        // Tìm thuốc khớp với nội dung bác sĩ gõ trong ô AutoComplete
        private Medicine? FindSelectedMedicine()
        {
            string input = _txtMedSearch.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            return _availableMedicines.FirstOrDefault(m =>
                       m.Name.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                       m.Code.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                       $"{m.Code} - {m.Name}".Equals(input, StringComparison.OrdinalIgnoreCase))
                   ?? _availableMedicines.FirstOrDefault(m =>
                       m.Name.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                       m.Code.Contains(input, StringComparison.OrdinalIgnoreCase));
        }

        // Kiểm tra tức thì số lượng tồn kho khi bác sĩ gõ tên thuốc hoặc đổi số lượng
        private void UpdateMedicineStockPreview()
        {
            var med = FindSelectedMedicine();
            if (med == null)
            {
                _txtStockStatus.Text = string.IsNullOrWhiteSpace(_txtMedSearch.Text)
                    ? "Chưa chọn thuốc"
                    : "Không tìm thấy thuốc phù hợp";
                _txtStockStatus.ForeColor = UiHelper.SecondaryGray;
                return;
            }

            int requestedQty = (int)_nudMedQty.Value;
            int existingInGrid = _prescriptionDetails
                .Where(d => d.MedicineId == med.Id)
                .Sum(d => d.Quantity);

            bool isEnough = _controller.CheckMedicineStock(med.Id, requestedQty + existingInGrid, out int currentStock);

            if (isEnough)
            {
                _txtStockStatus.Text = $"Tồn: {currentStock} {med.Unit} | Giá: {med.UnitPrice:N0}đ (Đủ thuốc)";
                _txtStockStatus.ForeColor = Color.FromArgb(30, 132, 73);
            }
            else
            {
                _txtStockStatus.Text = $"Chỉ còn {currentStock} {med.Unit} (Không đủ {requestedQty + existingInGrid} {med.Unit}!)";
                _txtStockStatus.ForeColor = UiHelper.DangerRed;
            }
        }

        // Thêm thuốc vào danh sách đơn thuốc (có kiểm tra tồn kho thực tế trong CSDL)
        private void HandleAddMedicineToPrescription()
        {
            var med = FindSelectedMedicine();
            if (med == null)
            {
                MessageBox.Show(
                    "Vui lòng nhập hoặc chọn đúng tên thuốc từ danh sách gợi ý!",
                    "Chưa chọn thuốc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtMedSearch.Focus();
                return;
            }

            int addQty = (int)_nudMedQty.Value;
            string dosage = _cboMedDosage.Text.Trim();
            if (string.IsNullOrEmpty(dosage))
            {
                MessageBox.Show(
                    "Vui lòng nhập liều dùng / cách dùng cho thuốc này!",
                    "Thiếu liều dùng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _cboMedDosage.Focus();
                return;
            }

            var existingItem = _prescriptionDetails.FirstOrDefault(d => d.MedicineId == med.Id);
            int totalRequestedQty = (existingItem?.Quantity ?? 0) + addQty;

            // Gọi DoctorController kiểm tra tồn kho tức thì
            if (!_controller.CheckMedicineStock(med.Id, totalRequestedQty, out int currentStock))
            {
                MessageBox.Show(
                    $"Thuốc '{med.Name}' trong kho hiện chỉ còn {currentStock} {med.Unit}, không đủ để kê {totalRequestedQty} {med.Unit}!",
                    "Cảnh Báo Không Đủ Tồn Kho",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity = totalRequestedQty;
                existingItem.Dosage = dosage;
            }
            else
            {
                _prescriptionDetails.Add(new PrescriptionDetail
                {
                    MedicineId = med.Id,
                    MedicineName = med.Name,
                    Quantity = addQty,
                    UnitPrice = med.UnitPrice,
                    Dosage = dosage
                });
            }

            RefreshPrescriptionGrid();
            UpdateFinancialSummary();

            // Xóa trắng ô tìm thuốc để bác sĩ nhập tiếp thuốc khác
            _txtMedSearch.Clear();
            _nudMedQty.Value = 10;
            _txtMedSearch.Focus();
        }

        // Xóa một dòng thuốc khỏi đơn đang kê
        private void HandleRemoveSelectedMedicine()
        {
            if (_dgvPrescription.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn dòng thuốc cần xóa trên bảng đơn thuốc!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = _dgvPrescription.SelectedRows[0];
            if (selectedRow.Tag is PrescriptionDetail detail)
            {
                _prescriptionDetails.Remove(detail);
                RefreshPrescriptionGrid();
                UpdateFinancialSummary();
                UpdateMedicineStockPreview();
            }
        }

        // Hiển thị lại danh sách thuốc đã kê lên DataGridView
        private void RefreshPrescriptionGrid()
        {
            _dgvPrescription.Rows.Clear();
            for (int i = 0; i < _prescriptionDetails.Count; i++)
            {
                var item = _prescriptionDetails[i];
                var medInfo = _availableMedicines.FirstOrDefault(m => m.Id == item.MedicineId);
                string unit = medInfo?.Unit ?? "Viên";

                int rowIdx = _dgvPrescription.Rows.Add(
                    i + 1,
                    item.MedicineName ?? string.Empty,
                    unit,
                    item.Quantity,
                    $"{item.UnitPrice:N0} đ",
                    item.Dosage ?? string.Empty,
                    $"{item.SubTotal:N0} đ"
                );
                _dgvPrescription.Rows[rowIdx].Tag = item;
            }
        }

        // Tự động tính lại Tổng tiền thuốc và Tổng cộng dự kiến
        private void UpdateFinancialSummary()
        {
            double examFee = (double)_nudExamFee.Value;
            double medTotal = _prescriptionDetails.Sum(d => d.SubTotal);
            double grandTotal = examFee + medTotal;

            _lblSummaryExamFee.Text = $"{examFee:N0} đ";
            _lblSummaryMedTotal.Text = $"{medTotal:N0} đ";
            _lblSummaryGrandTotal.Text = $"{grandTotal:N0} đ";
        }

        // Lưu Hồ sơ bệnh án + Đơn thuốc bằng Transaction và chuyển trạng thái sang PendingPayment
        private void HandleSaveMedicalRecordAndPrescription()
        {
            if (_currentPatient == null)
            {
                MessageBox.Show(
                    "Chưa có bệnh nhân nào được chọn! Vui lòng bấm 'Hàng Đợi & Gọi Khám' để chọn bệnh nhân.",
                    "Chưa chọn bệnh nhân",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string symptoms = _txtSymptoms.Text.Trim();
            string diagnosis = _txtDiagnosis.Text.Trim();

            if (string.IsNullOrEmpty(symptoms) || string.IsNullOrEmpty(diagnosis))
            {
                MessageBox.Show(
                    "Vui lòng nhập đầy đủ Triệu chứng lâm sàng và Chẩn đoán bệnh trước khi hoàn tất khám!",
                    "Thiếu thông tin khám bệnh",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var record = new MedicalRecord
                {
                    PatientId = _currentPatient.PatientId,
                    DoctorId = AppSession.CurrentUser?.Id ?? 3, // Bác sĩ đang đăng nhập hoặc ID 3 mặc định (BS.CKI Nguyễn Văn An)
                    QueueId = _currentPatient.Id,
                    Symptoms = symptoms,
                    Diagnosis = diagnosis,
                    ExamFee = (double)_nudExamFee.Value
                };

                int recordId = _controller.SaveMedicalRecordAndPrescription(record, _prescriptionDetails);
                double medTotal = _prescriptionDetails.Sum(d => d.SubTotal);

                MessageBox.Show(
                    $"Đã lưu Hồ sơ bệnh án (Mã BA-{recordId:D3}) và Đơn thuốc cho bệnh nhân {_currentPatient.PatientName}!\n\n" +
                    $"• Tiền công khám: {record.ExamFee:N0} đ\n" +
                    $"• Tổng tiền thuốc ({_prescriptionDetails.Count} loại): {medTotal:N0} đ\n" +
                    $"• Trạng thái hàng đợi đã chuyển sang: Chờ thu ngân thanh toán (PendingPayment).",
                    "Hoàn Tất Khám & Kê Đơn",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Reset form và tự động nạp bệnh nhân tiếp theo trong hàng đợi
                _txtSymptoms.Clear();
                _txtDiagnosis.Clear();
                _txtMedSearch.Clear();
                _prescriptionDetails.Clear();
                RefreshPrescriptionGrid();

                _currentPatient = null;
                LoadInitialData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu bệnh án và đơn thuốc: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Mở màn hình Hàng đợi & Gọi khám của Bác sĩ
        private void OpenDoctorQueueDialog()
        {
            QueueItem? calledPatient = null;
            FormDoctorQueue? dialogRef = null;
            dialogRef = new FormDoctorQueue(
                onOpenExamScreen: (queueItem) =>
                {
                    calledPatient = queueItem;
                    dialogRef?.Close();
                });

            using (dialogRef)
            {
                dialogRef.TopLevel = true;
                dialogRef.FormBorderStyle = FormBorderStyle.Sizable;
                dialogRef.StartPosition = FormStartPosition.CenterParent;
                dialogRef.Size = new Size(1120, 680);
                dialogRef.MinimumSize = new Size(980, 600);
                dialogRef.ShowDialog(this);
            }

            if (calledPatient != null)
            {
                SetExaminingPatient(calledPatient);
            }
            else
            {
                LoadInitialData();
            }
        }

        private void OpenMedicalHistoryDialog()
        {
            string initialKeyword = _currentPatient?.PatientName ?? string.Empty;
            using var frmHistory = new FormMedicalHistory(initialKeyword);
            frmHistory.ShowDialog(this);
        }

        private void OpenPrescriptionListDialog()
        {
            using var frmPrescription = new FormPrescriptionList(string.Empty);
            frmPrescription.ShowDialog(this);
        }

        private void OpenMedicineViewDialog()
        {
            using var frmMedicine = new FormDoctorMedicineView();
            frmMedicine.ShowDialog(this);
        }
    }
}
