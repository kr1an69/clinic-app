# Hệ Thống Quản Lý Phòng Khám & Đơn Thuốc

Đồ án cuối kỳ môn **dotNet Programming** — ứng dụng desktop WinForms hỗ trợ quy trình quản lý phòng khám: tiếp nhận bệnh nhân, điều phối hàng đợi, khám bệnh, kê đơn thuốc, quản lý kho thuốc và thanh toán hóa đơn.

## Thành viên thực hiện

Nhóm gồm 3 thành viên:

- Trần Tuấn Anh
- Trần Văn Đức
- Nguyễn Ngọc Thùy Dương

## Công nghệ sử dụng

- Nền tảng: .NET 10 (`net10.0-windows`), Windows Forms
- Kiến trúc: MVC (`Models`, `Views`, `Controllers`)
- Cơ sở dữ liệu: SQLite (`System.Data.SQLite.Core`)
- Truy vấn dữ liệu: Dapper
- Giao diện: FontAwesome.Sharp

## Chức năng chính

1. **Đăng nhập & Phân quyền:** Hỗ trợ 4 vai trò (`Admin`, `Receptionist`, `Doctor`, `Cashier`), tự động hiển thị menu theo quyền hạn.
2. **Lễ tân & Phòng chờ:** Tiếp nhận bệnh nhân mới/cũ, quản lý hồ sơ bệnh nhân, cấp số thứ tự khám, màn hình TV phòng chờ và thống kê bệnh nhân theo ngày.
3. **Khám bệnh & Kê đơn:** Gọi bệnh nhân theo hàng đợi, ghi triệu chứng/chẩn đoán, kê đơn thuốc và xem lịch sử khám bệnh.
4. **Kho thuốc & Thu ngân:** Quản lý danh mục thuốc, cảnh báo tồn kho thấp/sắp hết hạn, thanh toán, trừ kho tự động và in hóa đơn.

## Cách chạy ứng dụng

### Cách 1: Chạy trực tiếp từ bản Release (.exe)

Phù hợp để kiểm thử nhanh trên bất kỳ máy Windows 64-bit nào mà không cần cài đặt .NET SDK hay IDE.

1. Vào mục **Releases** của repository và tải file nén bản phát hành (`.zip`).
2. **Giải nén toàn bộ file `.zip`** ra một thư mục riêng (ví dụ: `Desktop\QuanLyPhongKham`) rồi chạy file `QuanLyPhongKham.exe`.
   - Không mở trực tiếp file `.exe` từ cửa sổ xem trước bên trong file `.zip`.
   - Không đặt trong thư mục yêu cầu quyền quản trị như `C:\Program Files\`.
   - Nếu Windows Defender SmartScreen hiện cảnh báo bảo mật, chọn **More info** -> **Run anyway**.

### Cách 2: Clone mã nguồn và chạy từ source code

**Yêu cầu môi trường:**

- Hệ điều hành Windows (x64)
- .NET 10 SDK (`dotnet --version` >= `10.0.x`)
- JetBrains Rider hoặc Visual Studio hỗ trợ .NET 10

**Các bước thực hiện:**

1. Clone repository về máy:
   ```bash
   git clone https://github.com/kr1an69/clinic-app
   cd clinic-app
   ```
2. Mở file `ClinicManagement.sln` bằng IDE và nhấn Run, hoặc chạy trực tiếp bằng terminal:
   ```bash
   dotnet run
   ```

## Cơ chế hoạt động của file cơ sở dữ liệu (`clinic.db`)

- Kịch bản khởi tạo bảng và dữ liệu mẫu (`Database/seed_clinic_data.sql`) đã được nhúng sẵn vào trong ứng dụng (Embedded Resource).
- **Khi chạy bản Release (`.exe`):** Ở lần mở đầu tiên, ứng dụng tự động tạo file `clinic.db` cùng đầy đủ bảng và dữ liệu mẫu nằm **ngay cùng thư mục với file `.exe`**. Mọi thay đổi dữ liệu trong quá trình sử dụng sẽ được lưu vào file `clinic.db` này.
- **Khi muốn reset dữ liệu về mặc định:** Đóng ứng dụng, xóa file `clinic.db` cạnh file `.exe`, sau đó mở lại ứng dụng để tạo mới database mẫu.
- **Khi chạy từ source code:** File `clinic.db` được tạo tự động trong thư mục build output (`bin/Debug/net10.0-windows/`).

## Tài khoản đăng nhập mẫu

| Vai trò | Tên đăng nhập | Mật khẩu | Phạm vi truy cập |
| :--- | :--- | :--- | :--- |
| Quản trị viên | `admin` | `admin123` | Toàn bộ chức năng hệ thống |
| Lễ tân | `letan` | `123` | Tiếp nhận bệnh nhân, quản lý hồ sơ, TV phòng chờ |
| Bác sĩ | `bacsi` | `123` | Khám bệnh, kê đơn thuốc, xem lịch sử khám |
| Dược sĩ / Thu ngân | `duocsi` | `123` | Kho thuốc, thu phí, phát thuốc và in hóa đơn |

## Hướng dẫn sử dụng chi tiết

Xem tài liệu hướng dẫn thao tác từng màn hình dành cho người dùng cuối tại [docs/USER_GUIDE.md](docs/USER_GUIDE.md).
