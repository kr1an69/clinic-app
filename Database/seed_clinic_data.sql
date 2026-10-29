-- =======================================================================
-- FILE: seed_clinic_data.sql
-- MỤC ĐÍCH: Dữ liệu mẫu khởi tạo độc lập cho Hệ Thống Quản Lý Phòng Khám
-- CÔNG NGHỆ: SQLite Database (clinic.db)
-- CÁCH DÙNG:
--   1. Tự động chạy khi ứng dụng WinForms khởi động lần đầu (qua DbContext.cs)
--   2. Hoặc import trực tiếp vào phần mềm 'DB Browser for SQLite' / Rider Database Tool
-- =======================================================================

PRAGMA foreign_keys = ON;

-- -----------------------------------------------------------------------
-- 1. TẠO CÁC BẢNG CSDL (TABLE SCHEMA)
-- -----------------------------------------------------------------------

-- 1.1 Bảng Người dùng & Phân quyền (Thành viên 1)
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL -- 'Admin', 'Doctor', 'Receptionist', 'Cashier'
);

-- 1.2 Bảng Bệnh nhân (Thành viên 1)
CREATE TABLE IF NOT EXISTS Patients (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT,
    Gender TEXT,
    BirthYear INTEGER,
    Address TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 1.3 Bảng Hàng đợi khám bệnh theo ngày (Thành viên 1)
CREATE TABLE IF NOT EXISTS Queues (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL,
    QueueNumber INTEGER NOT NULL,
    QueueDate DATE NOT NULL,
    Status TEXT DEFAULT 'Waiting', -- 'Waiting', 'Examining', 'PendingPayment', 'Completed', 'Cancelled'
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

-- 1.4 Bảng Danh mục thuốc & Kho tồn (Thành viên 3)
CREATE TABLE IF NOT EXISTS Medicines (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Unit TEXT NOT NULL, -- 'Viên', 'Vỉ', 'Hộp', 'Chai', 'Gói', 'Tuýp', 'Lọ'
    UnitPrice REAL NOT NULL,
    StockQuantity INTEGER NOT NULL DEFAULT 0,
    MinStockAlert INTEGER DEFAULT 10,
    ExpiryDate DATE
);

-- 1.5 Bảng Hồ sơ bệnh án / Phiếu khám (Thành viên 2)
CREATE TABLE IF NOT EXISTS MedicalRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL,
    DoctorId INTEGER,
    QueueId INTEGER,
    Symptoms TEXT, -- Triệu chứng lâm sàng
    Diagnosis TEXT, -- Chẩn đoán bệnh
    ExamFee REAL DEFAULT 50000,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES Users(Id)
);

-- 1.6 Bảng Đơn thuốc (Thành viên 2)
CREATE TABLE IF NOT EXISTS Prescriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicalRecordId INTEGER NOT NULL,
    TotalAmount REAL DEFAULT 0,
    IsDispensed INTEGER DEFAULT 0, -- 0: Chưa phát/chưa trừ kho, 1: Đã xuất thuốc
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id)
);

-- 1.7 Bảng Chi tiết từng loại thuốc trong đơn (Thành viên 2)
CREATE TABLE IF NOT EXISTS PrescriptionDetails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PrescriptionId INTEGER NOT NULL,
    MedicineId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    Dosage TEXT, -- Cách dùng: Sáng 1v, Tối 1v sau ăn
    UnitPrice REAL NOT NULL,
    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id),
    FOREIGN KEY (MedicineId) REFERENCES Medicines(Id)
);

-- 1.8 Bảng Hóa đơn & Thu phí (Thành viên 3)
CREATE TABLE IF NOT EXISTS Invoices (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicalRecordId INTEGER NOT NULL,
    PrescriptionId INTEGER,
    ExamFee REAL DEFAULT 0,
    MedicineFee REAL DEFAULT 0,
    TotalAmount REAL NOT NULL,
    CashierId INTEGER,
    PaidAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id),
    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id),
    FOREIGN KEY (CashierId) REFERENCES Users(Id)
);

-- -----------------------------------------------------------------------
-- 2. DỮ LIỆU MẪU ĐỂ 3 THÀNH VIÊN TEST ĐỘC LẬP (SEED DATA)
-- -----------------------------------------------------------------------

-- 2.1 Tài khoản đăng nhập mẫu cho 4 vai trò
INSERT OR IGNORE INTO Users (Id, Username, Password, FullName, Role) VALUES
(1, 'admin', 'admin123', 'Quản Trị Viên Hệ Thống', 'Admin'),
(2, 'letan', '123', 'Lễ Tân - Trần Thị Mai', 'Receptionist'),
(3, 'bacsi', '123', 'Bác Sĩ - BS.CKI Nguyễn Văn An', 'Doctor'),
(4, 'duocsi', '123', 'Dược Sĩ - Lê Thu Ngân', 'Cashier');

-- 2.2 Danh sách bệnh nhân mẫu (để TV1 test tìm kiếm, cấp số; TV2 test gọi khám)
INSERT OR IGNORE INTO Patients (Id, FullName, Phone, Gender, BirthYear, Address) VALUES
(1, 'Nguyễn Văn Hùng', '0901234567', 'Nam', 1988, '123 Nguyễn Trãi, Q.5, TP.HCM'),
(2, 'Trần Thị Bích Ngọc', '0912345678', 'Nữ', 1995, '45 Lê Lợi, Q.1, TP.HCM'),
(3, 'Lê Hoàng Long', '0987654321', 'Nam', 2002, '78 CMT8, Q.10, TP.HCM'),
(4, 'Phạm Minh Tuấn', '0933445566', 'Nam', 1975, '89 Hai Bà Trưng, Q.3, TP.HCM'),
(5, 'Đỗ Thị Kim Oanh', '0977889900', 'Nữ', 1990, '12 Hoàng Hoa Thám, Bình Thạnh');

-- 2.3 Kho thuốc phong phú (Đủ các case: Còn nhiều, Sắp hết hàng, Sắp hết date)
INSERT OR IGNORE INTO Medicines (Id, Code, Name, Unit, UnitPrice, StockQuantity, MinStockAlert, ExpiryDate) VALUES
-- Thuốc tồn nhiều bình thường
(1, 'PARA500', 'Paracetamol 500mg', 'Viên', 2000, 200, 20, '2027-12-31'),
(2, 'AMOX500', 'Amoxicillin 500mg', 'Viên', 4500, 150, 20, '2027-08-15'),
(3, 'PANADOL', 'Panadol Extra Đỏ', 'Vỉ', 18000, 80, 15, '2027-10-30'),
(4, 'CEFIX200', 'Cefixim 200mg', 'Hộp', 120000, 45, 10, '2027-05-20'),
(5, 'EFFER', 'Efferalgan Sủi 500mg', 'Tuýp', 65000, 60, 10, '2027-11-10'),
-- Case kiểm thử 1: Thuốc sắp hết hàng trong kho (Stock <= MinStockAlert -> TV3 test cảnh báo hết hàng)
(6, 'BERBERIN', 'Berberin Vàng 100mg', 'Lọ', 25000, 4, 10, '2027-06-30'),
(7, 'DECGEN', 'Decolgen Cảm Cúm', 'Vỉ', 12000, 3, 10, '2027-09-01'),
-- Case kiểm thử 2: Thuốc sắp hết hạn sử dụng cận date (TV3 test cảnh báo hạn dùng)
(8, 'VITAC', 'Vitamin C 1000mg', 'Tuýp', 45000, 50, 10, '2026-11-15');

-- 2.4 Hàng đợi mẫu hôm nay (Để TV1 test TV Monitor, TV2 test Bác sĩ gọi khám)
INSERT OR IGNORE INTO Queues (Id, PatientId, QueueNumber, QueueDate, Status) VALUES
(1, 1, 1, date('now'), 'Examining'),      -- Đang khám (STT 1)
(2, 2, 2, date('now'), 'Waiting'),        -- Đang chờ (STT 2)
(3, 3, 3, date('now'), 'Waiting');        -- Đang chờ (STT 3)

-- 2.5 Bệnh án & Đơn thuốc mẫu đã kê (Để TV3 test Thu ngân & Xuất kho trừ tồn)
INSERT OR IGNORE INTO MedicalRecords (Id, PatientId, DoctorId, QueueId, Symptoms, Diagnosis, ExamFee) VALUES
(1, 1, 3, 1, 'Sốt 38.5 độ, đau đầu, ho khan', 'Viêm họng cấp / Cảm sốt', 50000);

INSERT OR IGNORE INTO Prescriptions (Id, MedicalRecordId, TotalAmount, IsDispensed) VALUES
(1, 1, 65000, 0); -- Đơn thuốc giá 65k, IsDispensed = 0 (chưa xuất)

INSERT OR IGNORE INTO PrescriptionDetails (PrescriptionId, MedicineId, Quantity, Dosage, UnitPrice) VALUES
(1, 1, 10, 'Sáng 1 viên, tối 1 viên sau ăn', 2000),   -- 10 viên Para = 20k
(1, 2, 10, 'Sáng 1 viên, chiều 1 viên sau ăn', 4500); -- 10 viên Amox = 45k
