using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ClinicManagement.Controllers;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using FontAwesome.Sharp;

namespace ClinicManagement.Views
{
    public class FormPrintInvoice : BaseForm
    {
        private readonly PharmacyController _controller = new();
        private readonly PrintDocument _printDocument = new();
        private InvoicePrintData _printData;
        private List<InvoiceHistoryItem> _recentInvoices = new();
        private bool _isLoadingInvoices;
        private int _currentItemIndex;
        private int _currentPageNumber;
        private int _totalPages = 1;

        private PrintPreviewControl _previewControl = null!;
        private ComboBox _cboInvoicePicker = null!;
        private ComboBox _cboZoom = null!;
        private IconButton _btnPrevPage = null!;
        private Label _lblPageInfo = null!;
        private IconButton _btnNextPage = null!;
        private IconButton _btnPrint = null!;
        private IconButton _btnClose = null!;
        private Label _lblDocSummary = null!;

        // Constructor mặc định: tải hóa đơn mới nhất trong CSDL hoặc dữ liệu mẫu
        public FormPrintInvoice()
        {
            _printData = CreateSampleData();
            InitializeComponent();
            ConfigurePrintDocument();
            LoadInvoiceList(selectInvoiceId: null);
        }

        // Constructor mở theo mã hóa đơn đã thanh toán
        public FormPrintInvoice(int invoiceId)
        {
            _printData = LoadPrintDataByInvoiceId(invoiceId) ?? CreateSampleData();
            InitializeComponent();
            ConfigurePrintDocument();
            LoadInvoiceList(selectInvoiceId: invoiceId);
        }

        // Constructor nhận dữ liệu hóa đơn/đơn thuốc trực tiếp (dùng khi xem trước lúc chưa thu tiền)
        public FormPrintInvoice(InvoicePrintData printData)
        {
            _printData = printData ?? CreateSampleData();
            InitializeComponent();
            ConfigurePrintDocument();
            LoadInvoiceList(selectInvoiceId: printData != null && printData.InvoiceId > 0 ? printData.InvoiceId : null);
        }

        private void InitializeComponent()
        {
            this.Text = $"Xem Trước & In Đơn Thuốc - Hóa Đơn #{_printData.InvoiceId}";
            this.ConfigureAsFullScreen(minBaseWidth: 1180, minBaseHeight: 680);

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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headerBanner = UiHelper.CreateHeaderBanner(
                "XEM TRƯỚC & IN ĐƠN THUỐC KÈM HÓA ĐƠN",
                "Kiểm tra thông tin bệnh nhân, chi tiết thuốc và xuất lệnh in hoặc lưu file PDF");

            // Thanh công cụ điều khiển thu phóng và in ấn phía trên
            var toolbarPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = UiHelper.Pad(14, 8, 14, 8),
                Margin = Padding.Empty
            };
            toolbarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbarPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblDocSummary = UiHelper.CreateLabel(
                $"Phiếu #{_printData.InvoiceId}  |  Bệnh nhân: {_printData.PatientName}  |  Tổng: {_printData.TotalAmount:N0} đ",
                UiHelper.FontBold,
                UiHelper.DarkText,
                UiHelper.Pad(0, 6, 8, 6));

            var actionFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var lblInvoice = UiHelper.CreateLabel("Xem lại HĐ:", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(0, 7, 6, 0));

            _cboInvoicePicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiHelper.FontRegular,
                Width = UiHelper.S(220),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 3, 12, 0)
            };
            _cboInvoicePicker.SelectedIndexChanged += CboInvoicePicker_SelectedIndexChanged;

            var lblZoom = UiHelper.CreateLabel("Tỷ lệ:", UiHelper.FontRegular, UiHelper.DarkText, UiHelper.Pad(0, 7, 6, 0));

            _cboZoom = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiHelper.FontRegular,
                Width = UiHelper.S(115),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = UiHelper.Pad(0, 3, 10, 0)
            };
            _cboZoom.Items.AddRange(new object[] { "Vừa trang", "75%", "100%", "125%", "150%" });
            _cboZoom.SelectedIndex = 2;
            _cboZoom.SelectedIndexChanged += CboZoom_SelectedIndexChanged;

            _btnPrevPage = UiHelper.CreateTextButton("<", UiHelper.SecondaryGray, 38, 36);
            _btnPrevPage.Click += (_, _) => ChangePreviewPage(-1);

            _lblPageInfo = UiHelper.CreateLabel("Trang 1/1", UiHelper.FontBold, UiHelper.DarkText, UiHelper.Pad(6, 7, 6, 0));

            _btnNextPage = UiHelper.CreateTextButton(">", UiHelper.SecondaryGray, 38, 36);
            _btnNextPage.Click += (_, _) => ChangePreviewPage(1);

            _btnPrint = UiHelper.CreateButton("In Phiếu / Xuất PDF", IconChar.Print, UiHelper.PrimaryBlue, 185, 36);
            _btnPrint.Click += BtnPrint_Click;

            _btnClose = UiHelper.CreateTextButton("Đóng", UiHelper.SecondaryGray, 90, 36);
            _btnClose.Click += (_, _) => this.Close();

            actionFlow.Controls.Add(lblInvoice);
            actionFlow.Controls.Add(_cboInvoicePicker);
            actionFlow.Controls.Add(lblZoom);
            actionFlow.Controls.Add(_cboZoom);
            actionFlow.Controls.Add(_btnPrevPage);
            actionFlow.Controls.Add(_lblPageInfo);
            actionFlow.Controls.Add(_btnNextPage);
            actionFlow.Controls.Add(_btnPrint);
            actionFlow.Controls.Add(_btnClose);

            toolbarPanel.Controls.Add(_lblDocSummary, 0, 0);
            toolbarPanel.Controls.Add(actionFlow, 1, 0);

            // Khung xem trước bản in nhúng trực tiếp lên Form
            var previewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = UiHelper.Pad(12),
                BackColor = UiHelper.BgMain
            };

            _previewControl = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                Document = _printDocument,
                AutoZoom = false,
                Zoom = 1.0,
                UseAntiAlias = true,
                BackColor = Color.FromArgb(90, 98, 106)
            };

            previewContainer.Controls.Add(_previewControl);

            rootLayout.Controls.Add(headerBanner, 0, 0);
            rootLayout.Controls.Add(toolbarPanel, 0, 1);
            rootLayout.Controls.Add(previewContainer, 0, 2);

            this.Controls.Add(rootLayout);
        }

        private void ConfigurePrintDocument()
        {
            UpdatePrintHeaderSummary();
            _printDocument.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
            _printDocument.BeginPrint += PrintDocument_BeginPrint;
            _printDocument.PrintPage += PrintDocument_PrintPage;
        }

        // Đặt lại trạng thái phân trang trước mỗi lần xem trước hoặc in
        private void PrintDocument_BeginPrint(object sender, PrintEventArgs e)
        {
            _currentItemIndex = 0;
            _currentPageNumber = 0;
            _totalPages = 1;
        }

        private void ChangePreviewPage(int delta)
        {
            int nextPage = _previewControl.StartPage + delta;
            if (nextPage < 0 || nextPage >= _totalPages)
            {
                return;
            }

            _previewControl.StartPage = nextPage;
            UpdatePageNavigation();
        }

        private void UpdatePageNavigation()
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            if (_previewControl.StartPage >= _totalPages)
            {
                _previewControl.StartPage = Math.Max(0, _totalPages - 1);
            }

            _lblPageInfo.Text = $"Trang {_previewControl.StartPage + 1}/{_totalPages}";
            _btnPrevPage.Enabled = _previewControl.StartPage > 0;
            _btnNextPage.Enabled = _previewControl.StartPage + 1 < _totalPages;
        }

        // Tải danh sách hóa đơn đã thu để người dùng chọn xem lại hoặc in lại
        private void LoadInvoiceList(int? selectInvoiceId)
        {
            _isLoadingInvoices = true;
            try
            {
                _recentInvoices = _controller.GetInvoicesHistory(30);
                _cboInvoicePicker.Items.Clear();

                if (_printData.InvoiceId == 0)
                {
                    _cboInvoicePicker.Items.Add($"Đơn #{_printData.PrescriptionId} (Chưa thu)");
                }

                int selectedIndex = _printData.InvoiceId == 0 ? 0 : -1;
                foreach (var inv in _recentInvoices)
                {
                    int idx = _cboInvoicePicker.Items.Add($"HĐ #{inv.InvoiceId} - {inv.PatientName}");
                    if (selectInvoiceId.HasValue && inv.InvoiceId == selectInvoiceId.Value)
                    {
                        selectedIndex = idx;
                    }
                }

                if (selectedIndex >= 0)
                {
                    _cboInvoicePicker.SelectedIndex = selectedIndex;
                }
                else if (_cboInvoicePicker.Items.Count > 0 && _printData.InvoiceId > 0)
                {
                    _cboInvoicePicker.SelectedIndex = 0;
                    if (_recentInvoices.Count > 0)
                    {
                        var loaded = LoadPrintDataByInvoiceId(_recentInvoices[0].InvoiceId);
                        if (loaded != null)
                        {
                            _printData = loaded;
                            _previewControl.StartPage = 0;
                            UpdatePrintHeaderSummary();
                            _previewControl.InvalidatePreview();
                        }
                    }
                }
            }
            finally
            {
                _isLoadingInvoices = false;
            }
        }

        private void CboInvoicePicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isLoadingInvoices || _cboInvoicePicker.SelectedIndex < 0)
            {
                return;
            }

            int offset = _cboInvoicePicker.Items.Count - _recentInvoices.Count;
            int listIndex = _cboInvoicePicker.SelectedIndex - offset;
            if (listIndex < 0 || listIndex >= _recentInvoices.Count)
            {
                return;
            }

            var chosen = _recentInvoices[listIndex];
            var loaded = LoadPrintDataByInvoiceId(chosen.InvoiceId);
            if (loaded != null)
            {
                _printData = loaded;
                _previewControl.StartPage = 0;
                UpdatePrintHeaderSummary();
                _previewControl.InvalidatePreview();
            }
        }

        private InvoicePrintData? LoadPrintDataByInvoiceId(int invoiceId)
        {
            var inv = _controller.GetInvoiceById(invoiceId);
            if (inv == null)
            {
                return null;
            }

            var details = inv.PrescriptionId.HasValue
                ? _controller.GetPrescriptionDetails(inv.PrescriptionId.Value)
                : new List<PrescriptionDetail>();

            return new InvoicePrintData
            {
                InvoiceId = inv.InvoiceId,
                PrescriptionId = inv.PrescriptionId ?? 0,
                PatientName = inv.PatientName,
                Phone = inv.Phone,
                DoctorName = inv.DoctorName,
                CashierName = inv.CashierName,
                Diagnosis = inv.Diagnosis,
                ExamFee = inv.ExamFee,
                MedicineFeeOverride = inv.MedicineFee,
                PaidAt = inv.PaidAt,
                Items = details
            };
        }

        private void UpdatePrintHeaderSummary()
        {
            string docLabel = _printData.InvoiceId > 0
                ? $"HĐ #{_printData.InvoiceId}"
                : $"Đơn #{_printData.PrescriptionId} (Tạm tính)";

            this.Text = $"Xem Trước & In Đơn Thuốc - {docLabel}";
            _printDocument.DocumentName = _printData.InvoiceId > 0
                ? $"HoaDon_DonThuoc_{_printData.InvoiceId}"
                : $"TamTinh_DonThuoc_{_printData.PrescriptionId}";
            _lblDocSummary.Text = $"{docLabel}  |  BN: {_printData.PatientName}  |  Tổng: {_printData.TotalAmount:N0} đ";
        }

        private void CboZoom_SelectedIndexChanged(object? sender, EventArgs e)
        {
            switch (_cboZoom.SelectedIndex)
            {
                case 0:
                    _previewControl.AutoZoom = true;
                    break;
                case 1:
                    _previewControl.AutoZoom = false;
                    _previewControl.Zoom = 0.75;
                    break;
                case 2:
                    _previewControl.AutoZoom = false;
                    _previewControl.Zoom = 1.0;
                    break;
                case 3:
                    _previewControl.AutoZoom = false;
                    _previewControl.Zoom = 1.25;
                    break;
                case 4:
                    _previewControl.AutoZoom = false;
                    _previewControl.Zoom = 1.5;
                    break;
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            using var printDialog = new PrintDialog
            {
                Document = _printDocument,
                UseEXDialog = true
            };

            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                _printDocument.Print();
            }
        }

        // Vẽ nội dung Đơn thuốc và Hóa đơn thanh toán, tự động ngắt sang trang mới nếu đơn dài
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (e.Graphics == null)
            {
                return;
            }

            _currentPageNumber++;

            var g = e.Graphics;
            var left = (float)e.MarginBounds.Left;
            var right = (float)e.MarginBounds.Right;
            var width = (float)e.MarginBounds.Width;
            var y = (float)e.MarginBounds.Top;
            var bottomLimit = (float)e.MarginBounds.Bottom - 26f;

            using var fontClinic = new Font("Segoe UI", 11F, FontStyle.Bold);
            using var fontTitle = new Font("Segoe UI", 15F, FontStyle.Bold);
            using var fontSection = new Font("Segoe UI", 10F, FontStyle.Bold);
            using var fontRegular = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            using var fontBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var fontSmall = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var fontTotal = new Font("Segoe UI", 11.5F, FontStyle.Bold);

            using var brushPrimary = new SolidBrush(UiHelper.PrimaryBlue);
            using var brushDark = new SolidBrush(UiHelper.DarkText);
            using var brushMuted = new SolidBrush(UiHelper.SecondaryGray);
            using var brushDanger = new SolidBrush(UiHelper.DangerRed);
            using var penLine = new Pen(Color.FromArgb(189, 195, 199), 1F);
            using var penHeader = new Pen(UiHelper.PrimaryBlue, 1.5F);
            using var brushTableHeader = new SolidBrush(Color.FromArgb(235, 243, 250));

            using var fmtCenter = new StringFormat { Alignment = StringAlignment.Center };
            using var fmtRight = new StringFormat { Alignment = StringAlignment.Far };

            string ticketCode = _printData.InvoiceId > 0
                ? $"Số phiếu: #{_printData.InvoiceId}"
                : $"Đơn tạm tính: #{_printData.PrescriptionId}";

            if (_currentPageNumber == 1)
            {
                // 1. Header phòng khám
                g.DrawString("PHÒNG KHÁM ĐA KHOA TÂM AN", fontClinic, brushPrimary, left, y);
                g.DrawString(ticketCode, fontBold, brushDark, right, y, fmtRight);
                y += 20;

                g.DrawString("Địa chỉ: 123 Đường Nguyễn Huệ, TP.HCM  |  ĐT: (028) 3812 3456", fontSmall, brushMuted, left, y);
                g.DrawString($"Ngày lập: {_printData.PaidAt:dd/MM/yyyy HH:mm}", fontSmall, brushMuted, right, y, fmtRight);
                y += 22;

                g.DrawLine(penHeader, left, y, right, y);
                y += 14;

                // 2. Tiêu đề phiếu
                g.DrawString("ĐƠN THUỐC & HÓA ĐƠN THANH TOÁN", fontTitle, brushDark, left + width / 2f, y, fmtCenter);
                y += 34;

                // 3. Thông tin bệnh nhân & chẩn đoán
                g.DrawString($"Bệnh nhân: {_printData.PatientName}", fontBold, brushDark, left, y);
                g.DrawString($"SĐT: {(string.IsNullOrWhiteSpace(_printData.Phone) ? "---" : _printData.Phone)}", fontRegular, brushDark, left + width * 0.62f, y);
                y += 20;

                g.DrawString($"Mã đơn thuốc: #{_printData.PrescriptionId}", fontRegular, brushDark, left, y);
                g.DrawString($"Bác sĩ khám: {(string.IsNullOrWhiteSpace(_printData.DoctorName) ? "Bác sĩ trực" : _printData.DoctorName)}", fontRegular, brushDark, left + width * 0.62f, y);
                y += 20;

                var diagnosisText = string.IsNullOrWhiteSpace(_printData.Diagnosis) ? "Chưa ghi nhận" : _printData.Diagnosis;
                var diagnosisRect = new RectangleF(left, y, width, 38);
                g.DrawString($"Chẩn đoán: {diagnosisText}", fontRegular, brushDark, diagnosisRect);
                y += 34;

                g.DrawString("CHI TIẾT ĐƠN THUỐC", fontSection, brushPrimary, left, y);
                y += 22;
            }
            else
            {
                // Tiêu đề rút gọn cho trang tiếp theo
                g.DrawString("PHÒNG KHÁM ĐA KHOA TÂM AN - ĐƠN THUỐC & HÓA ĐƠN (TIẾP THEO)", fontSection, brushPrimary, left, y);
                g.DrawString(ticketCode, fontBold, brushDark, right, y, fmtRight);
                y += 20;

                g.DrawString($"Bệnh nhân: {_printData.PatientName}", fontRegular, brushDark, left, y);
                g.DrawString($"Trang {_currentPageNumber}", fontSmall, brushMuted, right, y, fmtRight);
                y += 20;

                g.DrawLine(penHeader, left, y, right, y);
                y += 14;
            }

            // 4. Bảng kê đơn thuốc & liều dùng
            float colStt = 35f;
            float colName = width * 0.27f;
            float colUnit = 50f;
            float colQty = 40f;
            float colPrice = 75f;
            float colSubTotal = 85f;
            float colDosage = width - (colStt + colName + colUnit + colQty + colPrice + colSubTotal);

            float xStt = left;
            float xName = xStt + colStt;
            float xUnit = xName + colName;
            float xQty = xUnit + colUnit;
            float xPrice = xQty + colQty;
            float xSubTotal = xPrice + colPrice;
            float xDosage = xSubTotal + colSubTotal;

            bool shouldDrawTable = _currentItemIndex < _printData.Items.Count || (_currentPageNumber == 1 && _printData.Items.Count == 0);
            if (shouldDrawTable)
            {
                float headerHeight = 28f;
                g.FillRectangle(brushTableHeader, left, y, width, headerHeight);
                g.DrawRectangle(penLine, left, y, width, headerHeight);

                float textY = y + 6f;
                g.DrawString("STT", fontBold, brushDark, xStt + colStt / 2f, textY, fmtCenter);
                g.DrawString("Tên thuốc", fontBold, brushDark, xName + 4f, textY);
                g.DrawString("ĐVT", fontBold, brushDark, xUnit + colUnit / 2f, textY, fmtCenter);
                g.DrawString("SL", fontBold, brushDark, xQty + colQty / 2f, textY, fmtCenter);
                g.DrawString("Đơn giá", fontBold, brushDark, xPrice + colPrice - 4f, textY, fmtRight);
                g.DrawString("Thành tiền", fontBold, brushDark, xSubTotal + colSubTotal - 4f, textY, fmtRight);
                g.DrawString("Liều dùng", fontBold, brushDark, xDosage + 6f, textY);
                y += headerHeight;

                if (_printData.Items.Count == 0)
                {
                    float emptyHeight = 28f;
                    g.DrawRectangle(penLine, left, y, width, emptyHeight);
                    g.DrawString("Không có thuốc kê trong đơn (chỉ thu phí khám).", fontRegular, brushMuted, left + width / 2f, y + 6f, fmtCenter);
                    y += emptyHeight;
                }
                else
                {
                    while (_currentItemIndex < _printData.Items.Count)
                    {
                        var item = _printData.Items[_currentItemIndex];
                        var dosage = string.IsNullOrWhiteSpace(item.Dosage) ? "---" : item.Dosage;
                        var medName = string.IsNullOrWhiteSpace(item.MedicineName) ? "---" : item.MedicineName;

                        var nameSize = g.MeasureString(medName, fontRegular, (int)(colName - 8f));
                        var dosageSize = g.MeasureString(dosage, fontSmall, (int)(colDosage - 10f));
                        float rowHeight = Math.Max(26f, Math.Max(nameSize.Height, dosageSize.Height) + 10f);

                        if (y + rowHeight > bottomLimit)
                        {
                            g.DrawString($"Trang {_currentPageNumber}", fontSmall, brushMuted, right, e.MarginBounds.Bottom - 16f, fmtRight);
                            e.HasMorePages = true;
                            return;
                        }

                        g.DrawRectangle(penLine, left, y, width, rowHeight);

                        float cellY = y + 5f;
                        g.DrawString((_currentItemIndex + 1).ToString(), fontRegular, brushDark, xStt + colStt / 2f, cellY, fmtCenter);
                        g.DrawString(medName, fontRegular, brushDark, new RectangleF(xName + 4f, cellY, colName - 8f, rowHeight - 6f));
                        g.DrawString(item.Unit ?? "Viên", fontRegular, brushDark, xUnit + colUnit / 2f, cellY, fmtCenter);
                        g.DrawString(item.Quantity.ToString(), fontRegular, brushDark, xQty + colQty / 2f, cellY, fmtCenter);
                        g.DrawString($"{item.UnitPrice:N0}", fontRegular, brushDark, xPrice + colPrice - 4f, cellY, fmtRight);
                        g.DrawString($"{item.SubTotal:N0}", fontBold, brushDark, xSubTotal + colSubTotal - 4f, cellY, fmtRight);
                        g.DrawString(dosage, fontSmall, brushDark, new RectangleF(xDosage + 6f, cellY, colDosage - 10f, rowHeight - 6f));

                        y += rowHeight;
                        _currentItemIndex++;
                    }
                }
            }

            // Kiểm tra còn đủ chỗ cho khối Tổng tiền + Chữ ký không, nếu thiếu thì đẩy sang trang sau
            const float summaryAndSignatureHeight = 205f;
            if (y + summaryAndSignatureHeight > bottomLimit)
            {
                g.DrawString($"Trang {_currentPageNumber}", fontSmall, brushMuted, right, e.MarginBounds.Bottom - 16f, fmtRight);
                e.HasMorePages = true;
                return;
            }

            y += 16;

            // 5. Tổng kết tiền khám và tiền thuốc
            float summaryLeft = left + width * 0.52f;
            float summaryRight = right;

            g.DrawString("Tiền khám bệnh:", fontRegular, brushDark, summaryLeft, y);
            g.DrawString($"{_printData.ExamFee:N0} đ", fontBold, brushDark, summaryRight, y, fmtRight);
            y += 22;

            g.DrawString("Tiền thuốc theo đơn:", fontRegular, brushDark, summaryLeft, y);
            g.DrawString($"{_printData.MedicineFee:N0} đ", fontBold, brushDark, summaryRight, y, fmtRight);
            y += 22;

            g.DrawLine(penLine, summaryLeft, y, summaryRight, y);
            y += 8;

            g.DrawString("TỔNG THANH TOÁN:", fontTotal, brushDark, summaryLeft, y);
            g.DrawString($"{_printData.TotalAmount:N0} đ", fontTotal, brushDanger, summaryRight, y, fmtRight);
            y += 42;

            // 6. Khu vực chữ ký xác nhận
            float leftSignCenter = left + width * 0.25f;
            float rightSignCenter = left + width * 0.75f;

            g.DrawString("BÁC SĨ KHÁM BỆNH", fontBold, brushDark, leftSignCenter, y, fmtCenter);
            g.DrawString("THU NGÂN / DƯỢC SĨ", fontBold, brushDark, rightSignCenter, y, fmtCenter);
            y += 18;

            g.DrawString("(Ký, ghi rõ họ tên)", fontSmall, brushMuted, leftSignCenter, y, fmtCenter);
            g.DrawString("(Ký, ghi rõ họ tên)", fontSmall, brushMuted, rightSignCenter, y, fmtCenter);
            y += 56;

            if (!string.IsNullOrWhiteSpace(_printData.DoctorName))
            {
                g.DrawString(_printData.DoctorName, fontBold, brushDark, leftSignCenter, y, fmtCenter);
            }

            if (!string.IsNullOrWhiteSpace(_printData.CashierName))
            {
                g.DrawString(_printData.CashierName, fontBold, brushDark, rightSignCenter, y, fmtCenter);
            }

            g.DrawString($"Trang {_currentPageNumber}", fontSmall, brushMuted, right, e.MarginBounds.Bottom - 16f, fmtRight);

            _totalPages = _currentPageNumber;
            e.HasMorePages = false;

            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(UpdatePageNavigation));
            }
        }

        // Tạo dữ liệu mẫu để xem trước bố cục phiếu in khi mở Form độc lập
        private static InvoicePrintData CreateSampleData()
        {
            return new InvoicePrintData
            {
                InvoiceId = 1,
                PrescriptionId = 1,
                PatientName = "Nguyễn Văn Mẫu",
                Phone = "0901234567",
                DoctorName = "BS.CKI Nguyễn Văn An",
                CashierName = "Dược Sĩ Lê Thu Ngân",
                Diagnosis = "Viêm họng cấp, sốt nhẹ",
                ExamFee = 50000,
                PaidAt = DateTime.Now,
                Items = new List<PrescriptionDetail>
                {
                    new()
                    {
                        MedicineName = "Paracetamol 500mg",
                        Unit = "Viên",
                        Quantity = 10,
                        UnitPrice = 1500,
                        Dosage = "Sáng 1 viên, Tối 1 viên sau ăn"
                    },
                    new()
                    {
                        MedicineName = "Amoxicillin 500mg",
                        Unit = "Viên",
                        Quantity = 10,
                        UnitPrice = 5000,
                        Dosage = "Sáng 1 viên, Chiều 1 viên sau ăn"
                    }
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _printDocument.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Dữ liệu tổng hợp dùng để hiển thị và in Đơn thuốc kèm Hóa đơn
    public class InvoicePrintData
    {
        public int InvoiceId { get; set; }
        public int PrescriptionId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? DoctorName { get; set; }
        public string? CashierName { get; set; }
        public string? Diagnosis { get; set; }
        public double ExamFee { get; set; }
        public double MedicineFeeOverride { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public List<PrescriptionDetail> Items { get; set; } = new();

        public double MedicineFee
        {
            get
            {
                if (Items.Count == 0)
                {
                    return MedicineFeeOverride;
                }

                double sum = 0;
                foreach (var item in Items)
                {
                    sum += item.SubTotal;
                }
                return sum;
            }
        }

        public double TotalAmount => ExamFee + MedicineFee;
    }
}
