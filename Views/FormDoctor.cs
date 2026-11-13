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
    public class FormDoctor : Form
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
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Padding = new Padding(12);
            this.Dock = DockStyle.Fill;

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
            var grpToolbar = new GroupBox
            {
                Text = "Điều Phối Khám Bệnh & Tra Cứu Nhanh",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 10),
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

            // Nút chọn bệnh nhân từ Hàng đợi bên trái
            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            _btnOpenQueue = UiHelper.CreateButton("Hàng Đợi & Gọi Khám", IconChar.BellConcierge, UiHelper.PrimaryBlue, 200, 36);
            _btnOpenQueue.Margin = new Padding(0, 0, 8, 0);
            leftFlow.Controls.Add(_btnOpenQueue);

            // Các nút mở màn hình tra cứu bên phải
            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            _btnOpenMedicineView = UiHelper.CreateButton("Tra Cứu Thuốc & Tồn Kho", IconChar.Capsules, Color.FromArgb(52, 73, 94), 215, 36);
            _btnOpenPrescriptionList = UiHelper.CreateButton("Đơn Thuốc Đã Kê", IconChar.FilePrescription, UiHelper.WarningOrange, 175, 36);
            _btnOpenHistory = UiHelper.CreateButton("Lịch Sử Bệnh Án", IconChar.NotesMedical, UiHelper.PrimaryBlue, 170, 36);

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
            var grpClinical = new GroupBox
            {
                Text = "1. Khám Lâm Sàng & Chẩn Đoán",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 10, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F)); // Thẻ thông tin BN
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));  // Triệu chứng
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));  // Chẩn đoán
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F)); // Tiền khám & Bác sĩ
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F)); // Tổng kết chi phí
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F)); // Nút Hoàn tất khám

            // Hộp hiển thị thông tin bệnh nhân đang khám
            var patientBanner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(235, 245, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblQueueStatus = new Label
            {
                Text = "Chưa chọn BN",
                AutoSize = true,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(214, 234, 248),
                ForeColor = Color.FromArgb(40, 116, 166),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(6, 4, 6, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblPatientName = new Label
            {
                Text = "Chưa chọn bệnh nhân khám",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            };

            _lblPatientMeta = new Label
            {
                Text = "Vui lòng chọn bệnh nhân từ Hàng đợi để bắt đầu khám",
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = UiHelper.SecondaryGray
            };

            patientBanner.Controls.Add(_lblQueueStatus);
            patientBanner.Controls.Add(_lblPatientName);
            patientBanner.Controls.Add(_lblPatientMeta);

            // Ô nhập Triệu chứng lâm sàng
            var pnlSymptoms = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            var lblSymptoms = new Label
            {
                Text = "Triệu chứng lâm sàng (*):",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            };
            _txtSymptoms = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                PlaceholderText = "Nhập triệu chứng bệnh nhân gặp phải (ví dụ: Sốt 38.5 độ, đau đầu, ho khan...)"
            };
            pnlSymptoms.Controls.Add(_txtSymptoms);
            pnlSymptoms.Controls.Add(lblSymptoms);

            // Ô nhập Chẩn đoán bệnh
            var pnlDiagnosis = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            var lblDiagnosis = new Label
            {
                Text = "Chẩn đoán bệnh (*):",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            };
            _txtDiagnosis = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                PlaceholderText = "Nhập kết luận chẩn đoán của bác sĩ (ví dụ: Viêm họng cấp / Cảm sốt...)"
            };
            pnlDiagnosis.Controls.Add(_txtDiagnosis);
            pnlDiagnosis.Controls.Add(lblDiagnosis);

            // Hàng Tiền công khám & Bác sĩ khám
            var feeDoctorGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 6)
            };
            feeDoctorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            feeDoctorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            feeDoctorGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            feeDoctorGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblExamFeeTitle = new Label
            {
                Text = "Tiền công khám (VNĐ):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            };
            var lblDoctorTitle = new Label
            {
                Text = "Bác sĩ phụ trách:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            };

            _nudExamFee = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 10000000,
                Increment = 10000,
                ThousandsSeparator = true,
                Value = 50000,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 8, 0)
            };

            _txtDoctorName = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = "BS.CKI Nguyễn Văn An",
                BackColor = Color.FromArgb(237, 242, 247),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            feeDoctorGrid.Controls.Add(lblExamFeeTitle, 0, 0);
            feeDoctorGrid.Controls.Add(lblDoctorTitle, 1, 0);
            feeDoctorGrid.Controls.Add(_nudExamFee, 0, 1);
            feeDoctorGrid.Controls.Add(_txtDoctorName, 1, 1);

            // Khung tổng kết tiền khám & tiền thuốc
            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.FromArgb(248, 250, 252),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 0, 0, 8)
            };
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));

            summaryPanel.Controls.Add(new Label
            {
                Text = "Tiền khám lâm sàng:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            _lblSummaryExamFee = new Label
            {
                Text = "50,000 đ",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Right
            };
            summaryPanel.Controls.Add(_lblSummaryExamFee, 1, 0);

            summaryPanel.Controls.Add(new Label
            {
                Text = "Tổng tiền thuốc trong đơn:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiHelper.DarkText,
                Anchor = AnchorStyles.Left
            }, 0, 1);

            _lblSummaryMedTotal = new Label
            {
                Text = "0 đ",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.DangerRed,
                Anchor = AnchorStyles.Right
            };
            summaryPanel.Controls.Add(_lblSummaryMedTotal, 1, 1);

            summaryPanel.Controls.Add(new Label
            {
                Text = "TỔNG CỘNG DỰ KIẾN:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Anchor = AnchorStyles.Left
            }, 0, 2);

            _lblSummaryGrandTotal = new Label
            {
                Text = "50,000 đ",
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                Anchor = AnchorStyles.Right
            };
            summaryPanel.Controls.Add(_lblSummaryGrandTotal, 1, 2);

            // Nút Hoàn tất khám & Chuyển thu ngân
            _btnSaveComplete = UiHelper.CreateButton(
                "HOÀN TẤT KHÁM & CHUYỂN THU NGÂN",
                IconChar.FloppyDisk,
                UiHelper.SuccessGreen,
                260,
                42);
            _btnSaveComplete.Dock = DockStyle.Fill;
            _btnSaveComplete.Margin = new Padding(0);

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
            var grpPrescription = new GroupBox
            {
                Text = "2. Kê Đơn Thuốc (Gợi Ý Tự Động & Kiểm Tra Tồn Kho)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiHelper.PrimaryBlue,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 134F)); // Khung nhập thuốc
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Bảng chi tiết đơn thuốc

            // Khung nhập thuốc có gợi ý AutoComplete
            var inputBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 249, 253),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 10)
            };

            var inputGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inputGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            inputGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Dòng 1: Ô tìm thuốc AutoComplete + Ô hiển thị trạng thái tồn kho
            var row1Grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0)
            };
            row1Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            row1Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            row1Grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            row1Grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            row1Grid.Controls.Add(new Label
            {
                Text = "Gõ tên thuốc hoặc mã thuốc (Gợi ý tự động):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 0, 0);

            row1Grid.Controls.Add(new Label
            {
                Text = "Tồn kho & Đơn giá hiện tại:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 1, 0);

            _txtMedSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                PlaceholderText = "Gõ tên thuốc (ví dụ: Paracetamol, Amoxicillin...)",
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                Margin = new Padding(0, 0, 8, 0)
            };

            _txtStockStatus = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = "Chưa chọn thuốc",
                BackColor = Color.FromArgb(237, 242, 247),
                ForeColor = UiHelper.SecondaryGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            row1Grid.Controls.Add(_txtMedSearch, 0, 1);
            row1Grid.Controls.Add(_txtStockStatus, 1, 1);

            // Dòng 2: Số lượng + Liều dùng + Nút Thêm vào đơn + Nút Xóa dòng
            var row2Grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0)
            };
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F)); // Số lượng
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Liều dùng
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // Nút Thêm
            row2Grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // Nút Xóa
            row2Grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            row2Grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            row2Grid.Controls.Add(new Label
            {
                Text = "Số lượng kê:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 0, 0);

            row2Grid.Controls.Add(new Label
            {
                Text = "Liều dùng / Cách dùng:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiHelper.DarkText
            }, 1, 0);

            _nudMedQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 2, 8, 0)
            };

            _cboMedDosage = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Margin = new Padding(0, 2, 8, 0)
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

            _btnAddMed = UiHelper.CreateButton("Thêm vào đơn", IconChar.CartPlus, UiHelper.PrimaryBlue, 145, 32, 18);
            _btnAddMed.Margin = new Padding(0, 0, 6, 0);

            _btnRemoveMed = UiHelper.CreateButton("Xóa thuốc", IconChar.Trash, UiHelper.DangerRed, 115, 32, 18);
            _btnRemoveMed.Margin = new Padding(0);

            row2Grid.Controls.Add(_nudMedQty, 0, 1);
            row2Grid.Controls.Add(_cboMedDosage, 1, 1);
            row2Grid.Controls.Add(_btnAddMed, 2, 1);
            row2Grid.Controls.Add(_btnRemoveMed, 3, 1);

            inputGrid.Controls.Add(row1Grid, 0, 0);
            inputGrid.Controls.Add(row2Grid, 0, 1);
            inputBox.Controls.Add(inputGrid);

            // Bảng DataGridView hiển thị danh sách thuốc trong đơn
            _dgvPrescription = new DataGridView
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

            _dgvPrescription.ColumnHeadersDefaultCellStyle.BackColor = UiHelper.PrimaryBlue;
            _dgvPrescription.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvPrescription.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _dgvPrescription.ColumnHeadersHeight = 36;
            _dgvPrescription.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            _dgvPrescription.DefaultCellStyle.SelectionForeColor = UiHelper.DarkText;

            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "STT", FillWeight = 12 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedicineName", HeaderText = "Tên Thuốc & Hàm Lượng", FillWeight = 34 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", FillWeight = 13 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Số Lượng", FillWeight = 15 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn Giá", FillWeight = 18 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dosage", HeaderText = "Liều Dùng Chỉ Định", FillWeight = 38 });
            _dgvPrescription.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "Thành Tiền", FillWeight = 20 });

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
                // Ưu tiên chọn bệnh nhân đang ở trạng thái Examining, nếu chưa có thì lấy người đầu hàng đợi
                _currentPatient = waitingList.FirstOrDefault(q => q.Status == "Examining")
                                  ?? waitingList.FirstOrDefault();
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
                    DoctorId = 3, // BS.CKI Nguyễn Văn An trong seed_clinic_data.sql
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
            MessageBox.Show(
                "Màn hình Tra cứu Lịch sử khám & Hồ sơ bệnh án (FormMedicalHistory) sẽ được tích hợp ở bước tiếp theo.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OpenPrescriptionListDialog()
        {
            MessageBox.Show(
                "Màn hình Quản lý Đơn thuốc đã kê (FormPrescriptionList) sẽ được tích hợp ở bước tiếp theo.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OpenMedicineViewDialog()
        {
            MessageBox.Show(
                "Màn hình Tra cứu Danh mục thuốc & Tồn kho (FormDoctorMedicineView) sẽ được tích hợp ở bước tiếp theo.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
