using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    public class PharmacyController
    {
        // Lấy danh sách toàn bộ thuốc trong kho
        public List<Medicine> GetAllMedicines()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT Id, Code, Name, Unit, UnitPrice, StockQuantity, MinStockAlert, ExpiryDate
                FROM Medicines
                ORDER BY Name ASC;";
            return conn.Query<Medicine>(sql).ToList();
        }

        // Thêm thuốc mới vào kho
        public void AddMedicine(Medicine medicine)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                INSERT INTO Medicines (Code, Name, Unit, UnitPrice, StockQuantity, MinStockAlert, ExpiryDate)
                VALUES (@Code, @Name, @Unit, @UnitPrice, @StockQuantity, @MinStockAlert, @ExpiryDate);";
            conn.Execute(sql, medicine);
        }

        // Lấy danh sách thuốc sắp hết hàng hoặc cận date (<= 60 ngày)
        public List<Medicine> GetAlertMedicines()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT Id, Code, Name, Unit, UnitPrice, StockQuantity, MinStockAlert, ExpiryDate
                FROM Medicines
                WHERE StockQuantity <= MinStockAlert 
                   OR (ExpiryDate IS NOT NULL AND date(ExpiryDate) <= date('now', '+60 day'))
                ORDER BY StockQuantity ASC, ExpiryDate ASC;";
            return conn.Query<Medicine>(sql).ToList();
        }

        // Nhập bổ sung số lượng tồn kho và cập nhật giá/hạn dùng mới (nếu có)
        public void RestockMedicine(int medicineId, int addQuantity, double? newPrice = null, string? newExpiryDate = null)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                UPDATE Medicines
                SET StockQuantity = StockQuantity + @AddQuantity,
                    UnitPrice = COALESCE(@NewPrice, UnitPrice),
                    ExpiryDate = COALESCE(@NewExpiryDate, ExpiryDate)
                WHERE Id = @MedicineId;";
            conn.Execute(sql, new { MedicineId = medicineId, AddQuantity = addQuantity, NewPrice = newPrice, NewExpiryDate = newExpiryDate });
        }

        // Kiểm tra mã thuốc đã tồn tại trong CSDL chưa
        public bool IsMedicineCodeExists(string code, int? excludeId = null)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT COUNT(1)
                FROM Medicines
                WHERE UPPER(Code) = UPPER(@Code) AND (@ExcludeId IS NULL OR Id <> @ExcludeId);";
            return conn.ExecuteScalar<int>(sql, new { Code = code.Trim(), ExcludeId = excludeId }) > 0;
        }

        // Lấy danh sách đơn thuốc đang chờ thu phí và xuất kho
        public List<PendingPrescriptionItem> GetPendingPrescriptions(string keyword = "")
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT
                    p.Id AS PrescriptionId,
                    p.MedicalRecordId,
                    mr.QueueId,
                    q.QueueNumber,
                    pat.FullName AS PatientName,
                    pat.Phone,
                    mr.Diagnosis,
                    COALESCE(mr.ExamFee, 0) AS ExamFee,
                    COALESCE(p.TotalAmount, 0) AS MedicineFee,
                    p.CreatedAt
                FROM Prescriptions p
                INNER JOIN MedicalRecords mr ON p.MedicalRecordId = mr.Id
                INNER JOIN Patients pat ON mr.PatientId = pat.Id
                LEFT JOIN Queues q ON mr.QueueId = q.Id
                WHERE p.IsDispensed = 0
                  AND (@Keyword = '' OR pat.FullName LIKE @LikePattern OR COALESCE(pat.Phone, '') LIKE @LikePattern)
                ORDER BY p.CreatedAt ASC, p.Id ASC;";

            var trimmed = keyword.Trim();
            return conn.Query<PendingPrescriptionItem>(sql, new
            {
                Keyword = trimmed,
                LikePattern = $"%{trimmed}%"
            }).ToList();
        }

        // Lấy chi tiết các loại thuốc trong một đơn thuốc kèm tồn kho hiện tại
        public List<PrescriptionDetail> GetPrescriptionDetails(int prescriptionId)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT
                    pd.Id,
                    pd.PrescriptionId,
                    pd.MedicineId,
                    pd.Quantity,
                    pd.Dosage,
                    pd.UnitPrice,
                    m.Name AS MedicineName,
                    m.Unit,
                    m.StockQuantity
                FROM PrescriptionDetails pd
                INNER JOIN Medicines m ON pd.MedicineId = m.Id
                WHERE pd.PrescriptionId = @PrescriptionId
                ORDER BY pd.Id ASC;";
            return conn.Query<PrescriptionDetail>(sql, new { PrescriptionId = prescriptionId }).ToList();
        }

        // Thu phí, trừ tồn kho thuốc, tạo hóa đơn và hoàn tất lượt khám bằng Transaction
        public int ProcessPaymentAndDispense(int prescriptionId, int? cashierId)
        {
            if (prescriptionId <= 0)
            {
                throw new ArgumentException("Mã đơn thuốc không hợp lệ.", nameof(prescriptionId));
            }

            using var conn = DbContext.GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string selectPrescriptionSql = @"
                    SELECT
                        p.Id AS PrescriptionId,
                        p.MedicalRecordId,
                        p.IsDispensed,
                        mr.QueueId,
                        COALESCE(mr.ExamFee, 0) AS ExamFee
                    FROM Prescriptions p
                    INNER JOIN MedicalRecords mr ON p.MedicalRecordId = mr.Id
                    WHERE p.Id = @PrescriptionId;";

                var prescription = conn.QueryFirstOrDefault<PrescriptionCheckoutInfo>(
                    selectPrescriptionSql,
                    new { PrescriptionId = prescriptionId },
                    transaction);

                if (prescription == null)
                {
                    throw new InvalidOperationException("Không tìm thấy đơn thuốc cần thanh toán.");
                }

                if (prescription.IsDispensed == 1)
                {
                    throw new InvalidOperationException("Đơn thuốc này đã được thanh toán và xuất kho trước đó.");
                }

                const string selectDetailsSql = @"
                    SELECT
                        pd.MedicineId,
                        pd.Quantity,
                        pd.UnitPrice,
                        m.Name AS MedicineName,
                        m.StockQuantity
                    FROM PrescriptionDetails pd
                    INNER JOIN Medicines m ON pd.MedicineId = m.Id
                    WHERE pd.PrescriptionId = @PrescriptionId;";

                var details = conn.Query<PrescriptionDetail>(
                    selectDetailsSql,
                    new { PrescriptionId = prescriptionId },
                    transaction).ToList();

                const string deductStockSql = @"
                    UPDATE Medicines
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE Id = @MedicineId AND StockQuantity >= @Quantity;";

                foreach (var item in details)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new InvalidOperationException($"Số lượng xuất của thuốc '{item.MedicineName}' phải lớn hơn 0.");
                    }

                    var affectedRows = conn.Execute(
                        deductStockSql,
                        new { item.MedicineId, item.Quantity },
                        transaction);

                    if (affectedRows == 0)
                    {
                        throw new InvalidOperationException(
                            $"Thuốc '{item.MedicineName}' không đủ tồn kho (cần {item.Quantity}, hiện còn {item.StockQuantity}).");
                    }
                }

                var medicineFee = details.Sum(d => d.Quantity * d.UnitPrice);
                var totalAmount = prescription.ExamFee + medicineFee;

                const string insertInvoiceSql = @"
                    INSERT INTO Invoices (MedicalRecordId, PrescriptionId, ExamFee, MedicineFee, TotalAmount, CashierId)
                    VALUES (@MedicalRecordId, @PrescriptionId, @ExamFee, @MedicineFee, @TotalAmount, @CashierId);
                    SELECT last_insert_rowid();";

                var invoiceId = conn.ExecuteScalar<int>(
                    insertInvoiceSql,
                    new
                    {
                        prescription.MedicalRecordId,
                        PrescriptionId = prescriptionId,
                        prescription.ExamFee,
                        MedicineFee = medicineFee,
                        TotalAmount = totalAmount,
                        CashierId = cashierId
                    },
                    transaction);

                const string updatePrescriptionSql = @"
                    UPDATE Prescriptions
                    SET IsDispensed = 1,
                        TotalAmount = @MedicineFee
                    WHERE Id = @PrescriptionId;";

                conn.Execute(
                    updatePrescriptionSql,
                    new { PrescriptionId = prescriptionId, MedicineFee = medicineFee },
                    transaction);

                if (prescription.QueueId.HasValue)
                {
                    const string updateQueueSql = @"
                        UPDATE Queues
                        SET Status = 'Completed'
                        WHERE Id = @QueueId;";

                    conn.Execute(updateQueueSql, new { QueueId = prescription.QueueId.Value }, transaction);
                }

                transaction.Commit();
                return invoiceId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private class PrescriptionCheckoutInfo
        {
            public int PrescriptionId { get; set; }
            public int MedicalRecordId { get; set; }
            public int IsDispensed { get; set; }
            public int? QueueId { get; set; }
            public double ExamFee { get; set; }
        }
    }
}
