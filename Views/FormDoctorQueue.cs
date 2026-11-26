using System;
using System.Drawing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    /// <summary>
    /// DANH SÁCH CHỜ KHÁM & ĐIỀU PHỐI GỌI BỆNH NHÂN
    /// Chuẩn hóa theo BaseForm và UiHelper (DPI-aware).
    /// </summary>
    public class FormDoctorQueue : BaseForm
    {
        private readonly DoctorController _controller = new();
        private readonly Action<QueueItem>? _onOpenExamScreen;
        private readonly Action<int>? _onOpenHistoryScreen;

        // Các thành phần giao diện
        private TableLayoutPanel _mainLayout = null!;
        private GroupBox _grpWaitingList = null!;
        private GroupBox _grpCallAction = null!;
        private DataGridView _dgvDoctorWaiting = null!;
        private IconButton _btnRefresh = null!;
        private IconButton _btnCallPatient = null!;
        private IconButton _btnViewHistory = null!;
        private IconButton _btnCancelQueue = null!;

        // Thẻ hiển thị xem trước thông tin bệnh nhân
        private Panel _patientCard = null!;
        private Label _lblQueueNumberBig = null!;
        private Label _lblPatientName = null!;
        private Label _lblPatientMeta = null!;

        // Dữ liệu bệnh nhân đang chọn
        private QueueItem? _selectedQueueItem;

        // Hiệu ứng Pulse Animation khi gọi khám
        private System.Windows.Forms.Timer? _pulseTimer;
        private int _pulseStep = 0;

        public FormDoctorQueue(Action<QueueItem>? onOpenExamScreen = null, Action<int>? onOpenHistoryScreen = null)
        {
            _onOpenExamScreen = onOpenExamScreen;
            _onOpenHistoryScreen = onOpenHistoryScreen;
            InitializeComponent();
            SetupAnimationTimer();
            RegisterEvents();
            LoadQueueData();
        }

        private void InitializeComponent()
        {
            this.Text = "Hàng Đợi Khám Bệnh - Bác Sĩ";
            this.ConfigureAsFixedDialog(baseWidth: 1150, baseHeight: 680);

            var bannerPanel = UiHelper.CreateHeaderBanner(
                title: "DANH SÁCH CHỜ KHÁM & ĐIỀU PHỐI GỌI BỆNH NHÂN (BS)",
                subtitle: "Bác sĩ chọn số thứ tự -> Bấm 'Gọi Vào Khám' để chuyển trạng thái sang Examining"
            );

            _mainLayout = BuildMainLayout();
            _grpWaitingList = BuildWaitingListGroup();
            _grpCallAction = BuildCallActionGroup();

            _mainLayout.Controls.Add(_grpWaitingList, 0, 0);
            _mainLayout.Controls.Add(_grpCallAction, 1, 0);

            this.Controls.Add(_mainLayout);
            this.Controls.Add(bannerPanel);
        }

        private TableLayoutPanel BuildMainLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = UiHelper.Pad(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private GroupBox BuildWaitingListGroup()
        {
            var group = UiHelper.CreateGroupBox("DANH SÁCH BỆNH NHÂN ĐANG CHỜ KHÁM HÔM NAY", padding: UiHelper.Pad(10));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = UiHelper.Pad(0, 2, 0, 6)
            };

            _btnRefresh = UiHelper.CreateButton("Làm mới", IconChar.Rotate, Color.FromArgb(52, 73, 94), minWidth: 110, height: 34);
            topPanel.Controls.Add(_btnRefresh);

            _dgvDoctorWaiting = UiHelper.CreateStandardGrid(baseRowHeight: 36, baseHeaderHeight: 38);
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueNumber", HeaderText = "STT", FillWeight = 18 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Họ và Tên Bệnh Nhân", FillWeight = 42 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Số Điện Thoại", FillWeight = 25 });
            _dgvDoctorWaiting.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", FillWeight = 25 });

            layout.Controls.Add(topPanel, 0, 0);
            layout.Controls.Add(_dgvDoctorWaiting, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildCallActionGroup()
        {
            var group = UiHelper.CreateGroupBox("ĐIỀU PHỐI GỌI KHÁM", padding: UiHelper.Pad(12));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(165)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(52)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(48)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(48)));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UiHelper.SF(44)));

            _patientCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(235, 245, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = UiHelper.Pad(10),
                Margin = UiHelper.Pad(0, 0, 0, 10)
            };

            var lblCardHeader = UiHelper.CreateLabel(
                text: "BỆNH NHÂN ĐANG CHỌN",
                font: UiHelper.FontLabelBold,
                foreColor: Color.FromArgb(40, 116, 166),
                margin: UiHelper.Pad(0, 0, 0, 4));
            lblCardHeader.Dock = DockStyle.Top;
            lblCardHeader.TextAlign = ContentAlignment.MiddleCenter;

            _lblQueueNumberBig = UiHelper.CreateLabel(
                text: "STT: --",
                font: new Font("Segoe UI", 26F, FontStyle.Bold),
                foreColor: UiHelper.PrimaryBlue,
                margin: UiHelper.Pad(0, 0, 0, 2));
            _lblQueueNumberBig.Dock = DockStyle.Top;
            _lblQueueNumberBig.TextAlign = ContentAlignment.MiddleCenter;
            _lblQueueNumberBig.Height = UiHelper.S(52);

            _lblPatientName = UiHelper.CreateLabel(
                text: "Chưa chọn bệnh nhân",
                font: new Font("Segoe UI", 12F, FontStyle.Bold),
                foreColor: UiHelper.DarkText,
                margin: UiHelper.Pad(0, 0, 0, 2));
            _lblPatientName.Dock = DockStyle.Top;
            _lblPatientName.TextAlign = ContentAlignment.MiddleCenter;

            _lblPatientMeta = UiHelper.CreateLabel(
                text: "Chọn một bệnh nhân trong danh sách bên trái",
                font: UiHelper.FontSmall,
                foreColor: UiHelper.SecondaryGray,
                margin: Padding.Empty);
            _lblPatientMeta.Dock = DockStyle.Top;
            _lblPatientMeta.TextAlign = ContentAlignment.MiddleCenter;

            _patientCard.Controls.Add(_lblPatientMeta);
            _patientCard.Controls.Add(_lblPatientName);
            _patientCard.Controls.Add(_lblQueueNumberBig);
            _patientCard.Controls.Add(lblCardHeader);

            _btnCallPatient = UiHelper.CreateButton("GỌI VÀO KHÁM & MỞ PHIẾU KHÁM", IconChar.Stethoscope, UiHelper.SuccessGreen, height: 44);
            _btnCallPatient.AutoSize = false;
            _btnCallPatient.Dock = DockStyle.Fill;
            _btnCallPatient.Margin = UiHelper.Pad(0, 0, 0, 8);

            _btnViewHistory = UiHelper.CreateButton("Xem tiền sử khám bệnh của BN này", IconChar.ClockRotateLeft, UiHelper.PrimaryBlue, height: 40);
            _btnViewHistory.AutoSize = false;
            _btnViewHistory.Dock = DockStyle.Fill;
            _btnViewHistory.Margin = UiHelper.Pad(0, 0, 0, 8);

            _btnCancelQueue = UiHelper.CreateButton("Đánh dấu Vắng mặt / Hủy lượt (Cancelled)", IconChar.UserSlash, UiHelper.DangerRed, height: 40);
            _btnCancelQueue.AutoSize = false;
            _btnCancelQueue.Dock = DockStyle.Fill;
            _btnCancelQueue.Margin = UiHelper.Pad(0, 0, 0, 8);

            var lblTip = UiHelper.CreateLabel(
                text: "Ghi chú: Thao tác 'Gọi Vào Khám' sẽ phát tín hiệu chuông và cập nhật trạng thái đồng bộ sang TV phòng chờ.",
                font: new Font("Segoe UI", 8.5F, FontStyle.Italic),
                foreColor: UiHelper.SecondaryGray,
                margin: UiHelper.Pad(0, 4, 0, 4));
            lblTip.AutoSize = false;
            lblTip.Dock = DockStyle.Fill;
            lblTip.TextAlign = ContentAlignment.MiddleLeft;

            layout.Controls.Add(_patientCard, 0, 0);
            layout.Controls.Add(_btnCallPatient, 0, 1);
            layout.Controls.Add(_btnViewHistory, 0, 2);
            layout.Controls.Add(_btnCancelQueue, 0, 3);
            layout.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 4);
            layout.Controls.Add(lblTip, 0, 5);

            group.Controls.Add(layout);
            return group;
        }

        // --- CÁC PHƯƠNG THỨC NGHIỆP VỤ & SỰ KIỆN ---

        private void RegisterEvents()
        {
            _btnRefresh.Click += (_, _) => LoadQueueData();
            _btnCallPatient.Click += (_, _) => HandleCallPatientClick();
            _btnViewHistory.Click += (_, _) => HandleViewHistoryClick();
            _btnCancelQueue.Click += (_, _) => HandleCancelQueueClick();
            _dgvDoctorWaiting.SelectionChanged += (_, _) => OnSelectionChanged();

            // Định dạng màu sắc trạng thái cột Status
            _dgvDoctorWaiting.CellFormatting += (_, e) =>
            {
                if (e.RowIndex >= 0 && _dgvDoctorWaiting.Columns[e.ColumnIndex].Name == "Status")
                {
                    string status = e.Value?.ToString() ?? "";
                    if (status == "Examining")
                    {
                        e.Value = "Đang khám";
                        e.CellStyle.ForeColor = UiHelper.PrimaryBlue;
                        e.CellStyle.Font = UiHelper.FontBold;
                    }
                    else if (status == "Waiting")
                    {
                        e.Value = "Đang chờ";
                        e.CellStyle.ForeColor = UiHelper.WarningOrange;
                        e.CellStyle.Font = UiHelper.FontBold;
                    }
                    else if (status == "Cancelled")
                    {
                        e.Value = "Vắng mặt / Đã hủy";
                        e.CellStyle.ForeColor = UiHelper.DangerRed;
                        e.CellStyle.Font = UiHelper.FontBold;
                    }
                }
            };
        }

        private void SetupAnimationTimer()
        {
            _pulseTimer = new System.Windows.Forms.Timer
            {
                Interval = 70
            };
            _pulseTimer.Tick += (_, _) =>
            {
                _pulseStep++;
                if (_pulseStep % 2 == 1)
                {
                    _patientCard.BackColor = Color.FromArgb(212, 239, 223); // Xanh nhạt sáng
                    _lblQueueNumberBig.ForeColor = UiHelper.SuccessGreen;
                }
                else
                {
                    _patientCard.BackColor = Color.FromArgb(235, 245, 251); // Màu gốc
                    _lblQueueNumberBig.ForeColor = UiHelper.PrimaryBlue;
                }

                if (_pulseStep >= 6) // Chớp 3 nhịp
                {
                    _pulseTimer.Stop();
                    _pulseStep = 0;

                    if (_selectedQueueItem != null)
                    {
                        if (_onOpenExamScreen != null)
                        {
                            _onOpenExamScreen.Invoke(_selectedQueueItem);
                        }
                        else
                        {
                            MessageBox.Show(
                                $"Đã gọi thành công bệnh nhân: {_selectedQueueItem.PatientName} (STT {_selectedQueueItem.QueueNumber:D2})!\nTrạng thái đã cập nhật sang 'Examining'.",
                                "Thông Báo Gọi Khám",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            LoadQueueData();
                        }
                    }
                }
            };
        }

        public void LoadQueueData()
        {
            try
            {
                var list = _controller.GetPatientsWaitingForDoctor();
                _dgvDoctorWaiting.Rows.Clear();

                foreach (var item in list)
                {
                    int rowIndex = _dgvDoctorWaiting.Rows.Add(
                        $"{item.QueueNumber:D2}",
                        item.PatientName ?? "Không có tên",
                        item.Phone ?? "---",
                        item.Status
                    );
                    _dgvDoctorWaiting.Rows[rowIndex].Tag = item;
                }

                if (_dgvDoctorWaiting.Rows.Count > 0)
                {
                    _dgvDoctorWaiting.Rows[0].Selected = true;
                    OnSelectionChanged();
                }
                else
                {
                    ResetPatientCard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách hàng đợi: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSelectionChanged()
        {
            if (_dgvDoctorWaiting.SelectedRows.Count > 0)
            {
                var row = _dgvDoctorWaiting.SelectedRows[0];
                if (row.Tag is QueueItem item)
                {
                    _selectedQueueItem = item;
                    _lblQueueNumberBig.Text = $"STT: {item.QueueNumber:D2}";
                    _lblPatientName.Text = item.PatientName ?? "Chưa rõ tên";

                    string statusVi = item.Status switch
                    {
                        "Examining" => "Đang khám",
                        "Waiting" => "Đang chờ khám",
                        "Cancelled" => "Vắng mặt / Đã hủy",
                        _ => item.Status
                    };

                    _lblPatientMeta.Text = $"Mã BN: #{item.PatientId} | SĐT: {item.Phone ?? "---"} | Trạng thái: {statusVi}";
                    return;
                }
            }
            ResetPatientCard();
        }

        private void ResetPatientCard()
        {
            _selectedQueueItem = null;
            _lblQueueNumberBig.Text = "STT: --";
            _lblPatientName.Text = "Chưa có lượt chờ nào";
            _lblPatientMeta.Text = "Hàng đợi phòng khám hiện đang trống";
        }

        private void HandleCallPatientClick()
        {
            if (_selectedQueueItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bệnh nhân trong danh sách trước khi gọi khám!", "Chưa chọn bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _controller.CallPatient(_selectedQueueItem.Id);
                _selectedQueueItem.Status = "Examining";

                _pulseStep = 0;
                _pulseTimer?.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật trạng thái gọi khám: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleViewHistoryClick()
        {
            if (_selectedQueueItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bệnh nhân để xem tiền sử khám!", "Chưa chọn bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_onOpenHistoryScreen != null)
            {
                _onOpenHistoryScreen.Invoke(_selectedQueueItem.PatientId);
            }
            else
            {
                using var frmHistory = new FormMedicalHistory(_selectedQueueItem.PatientName ?? string.Empty);
                frmHistory.ShowDialog(this);
            }
        }

        private void HandleCancelQueueClick()
        {
            if (_selectedQueueItem == null)
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân cần hủy lượt!", "Chưa chọn bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy lượt khám của bệnh nhân {_selectedQueueItem.PatientName} (STT {_selectedQueueItem.QueueNumber:D2})?\nTrạng thái sẽ chuyển thành 'Cancelled'.",
                "Xác Nhận Hủy Lượt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    _controller.CancelPatientQueue(_selectedQueueItem.Id);
                    LoadQueueData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi hủy lượt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pulseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
