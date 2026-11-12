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

            _dgvPrescription.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(223, 230, 233);
            _dgvPrescription.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(45, 52, 54);
            _dgvPrescription.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _dgvPrescription.ColumnHeadersHeight = 36;
            _dgvPrescription.DefaultCellStyle.SelectionBackColor = Color.FromArgb(174, 214, 241);
            _dgvPrescription.DefaultCellStyle.SelectionForeColor = Color.FromArgb(27, 79, 114);

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
    }
}
