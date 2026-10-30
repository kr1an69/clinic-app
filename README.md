# HỆ THỐNG QUẢN LÝ PHÒNG KHÁM & ĐƠN THUỐC (WinForms - MVC - SQLite)

Dự án đồ án môn Lập trình .NET (Nhóm 2 - 3 thành viên), xây dựng theo kiến trúc **MVC** kết hợp cơ sở dữ liệu **SQLite** và **Dapper** trên nền tảng **.NET 10 (`net10.0-windows`)**.

---

## 🚀 Cách Mở & Chạy Dự Án

1. **Yêu cầu môi trường:**
   - Hệ điều hành Windows (x64)
   - **.NET 10 SDK** (`dotnet --version` $\ge$ `10.0.x`)
   - **JetBrains Rider** (2025.3+ / 2026.x) hoặc Visual Studio 2026

2. **Khởi chạy:**
   - Mở file **`ClinicManagement.sln`** bằng JetBrains Rider và nhấn **Run** (`Shift + F10`).
   - Hoặc mở Terminal tại thư mục dự án và chạy:
     ```bash
     dotnet run
     ```
   - File cơ sở dữ liệu `clinic.db` cùng dữ liệu mẫu (`Database/seed_clinic_data.sql`) sẽ tự động được khởi tạo trong thư mục chạy chương trình ở lần chạy đầu tiên.
