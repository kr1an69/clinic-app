using System;

namespace ClinicManagement.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin, Doctor, Receptionist, Cashier
    }

    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public int BirthYear { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QueueItem
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int QueueNumber { get; set; }
        public string QueueDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Waiting"; // Waiting, Examining, PendingPayment, Completed, Cancelled
        public DateTime CreatedAt { get; set; }

        // Thuộc tính JOIN hiển thị danh sách trực quan
        public string? PatientName { get; set; }
        public string? Phone { get; set; }
    }

    public class Medicine
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "Viên";
        public double UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockAlert { get; set; } = 10;
        public string? ExpiryDate { get; set; }
    }

    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public int? QueueId { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public double ExamFee { get; set; } = 50000;
        public DateTime CreatedAt { get; set; }
    }

    public class Prescription
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public double TotalAmount { get; set; }
        public int IsDispensed { get; set; } // 0: Chưa phát, 1: Đã xuất kho
        public DateTime CreatedAt { get; set; }
    }

    public class PrescriptionDetail
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
        public double UnitPrice { get; set; }

        // Tên thuốc & thành tiền hiển thị trên DataGridView
        public string? MedicineName { get; set; }
        public double SubTotal => Quantity * UnitPrice;
    }

    public class Invoice
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public int? PrescriptionId { get; set; }
        public double ExamFee { get; set; }
        public double MedicineFee { get; set; }
        public double TotalAmount { get; set; }
        public int? CashierId { get; set; }
        public DateTime PaidAt { get; set; }
    }

    public class DailyPatientReportItem
    {
        public string ExamDate { get; set; } = string.Empty;
        public int TotalPatients { get; set; }
        public int CompletedCount { get; set; }
        public int WaitingCount { get; set; }
    }
}
