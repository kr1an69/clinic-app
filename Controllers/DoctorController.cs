using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    //BÁC SĨ KHÁM BỆNH & KÊ ĐƠN THUỐC
    public class DoctorController
    {
        // Lấy danh sách bệnh nhân đang chờ khám (Status = 'Waiting') theo số thứ tự
        public List<QueueItem> GetPatientsWaitingForDoctor()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT 
                    q.Id,
                    q.PatientId,
                    q.QueueNumber,
                    q.QueueDate,
                    q.Status,
                    q.CreatedAt,
                    p.FullName AS PatientName,
                    p.Phone
                FROM Queues q
                INNER JOIN Patients p ON q.PatientId = p.Id
                WHERE q.QueueDate = date('now')
                  AND q.Status IN ('Waiting', 'Examining')
                ORDER BY q.QueueNumber ASC;";
            return conn.Query<QueueItem>(sql).ToList();
        }

        //Cập nhật trạng thái hàng đợi sang 'Examining' khi bác sĩ gọi khám
        public void CallPatient(int queueId)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                UPDATE Queues
                SET Status = 'Examining'
               WHERE Id = @QueueId;";
            conn.Execute(sql, new { QueueID = queueId });
        }

        //Lấy danh sách thuốc còn tồn kho (StockQuantity > 0) để gợi ý AutoComplete
        public List<Medicine> GetAllMedicinesForPrescription()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT Id, Code, Name, Unit, UnitPrice, StockQuantity, MinStockAlert, ExpiryDate 
                FROM Medicines
                WHERE StockQuantity  >0 
                ORDER BY Name ASC;";
            return conn.Query<Medicine>(sql).ToList();
        }

        //Kiểm tra tồn kho thuốc xem có đủ số lượng bác sĩ kê hay không
        public bool CheckMedicineStock(int medicineId, int requestedQty, out int currentStock)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT StockQuantity 
                FROM Medicines 
                WHERE Id = @MedicineId;";
            // Lấy số lượng tồn kho hiện tại (nếu không tìm thấy thuốc thì mặc định là 0)
            currentStock = conn.ExecuteScalar<int?>(sql, new { MedicineId = medicineId }) ?? 0;
            return requestedQty > 0 && currentStock >= requestedQty;
        }

        //Lưu bệnh án (MedicalRecords), đơn thuốc (Prescriptions) và chi tiết thuốc (PrescriptionDetails) bằng Transaction
        public int SaveMedicalRecordAndPrescription(MedicalRecord record, List<PrescriptionDetail> details)
        {
            using var conn = DbContext.GetConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                //Thêm mới Hồ sơ bệnh án (MedicalRecords) và lấy Id vừa tạo
                const string insertRecordSql = @"
                    INSERT INTO MedicalRecords (PatientId, DoctorId, QueueId, Symptoms, Diagnosis, ExamFee)
                    VALUES (@PatientId, @DoctorId, @QueueId, @Symptoms, @Diagnosis, @ExamFee);
                    SELECT last_insert_rowid();";
                int medicalRecordId = conn.ExecuteScalar<int>(insertRecordSql, new
                {
                    record.PatientId,
                    DoctorId = record.DoctorId ?? 3, // Mặc định ID=3 là Bác sĩ trong seed data nếu chưa truyền
                    record.QueueId,
                    record.Symptoms,
                    record.Diagnosis,
                    ExamFee = record.ExamFee > 0 ? record.ExamFee : 50000
                }, transaction);
                //Nếu có kê đơn thuốc thì lưu bảng Prescriptions và PrescriptionDetails
                if (details != null && details.Count > 0)
                {
                    double totalMedicineAmount = details.Sum(d => d.Quantity * d.UnitPrice);
                    const string insertPrescriptionSql = @"
                        INSERT INTO Prescriptions (MedicalRecordId, TotalAmount, IsDispensed)
                        VALUES (@MedicalRecordId, @TotalAmount, 0);
                        SELECT last_insert_rowid();";
                    int prescriptionId = conn.ExecuteScalar<int>(insertPrescriptionSql, new
                    {
                        MedicalRecordId = medicalRecordId,
                        TotalAmount = totalMedicineAmount
                    }, transaction);
                    const string insertDetailSql = @"
                        INSERT INTO PrescriptionDetails (PrescriptionId, MedicineId, Quantity, Dosage, UnitPrice)
                        VALUES (@PrescriptionId, @MedicineId, @Quantity, @Dosage, @UnitPrice);";
                    foreach (var item in details)
                    {
                        conn.Execute(insertDetailSql, new
                        {
                            PrescriptionId = prescriptionId,
                            item.MedicineId,
                            item.Quantity,
                            item.Dosage,
                            item.UnitPrice
                        }, transaction);
                    }
                }

                //Cập nhật trạng thái hàng đợi sang 'PendingPayment' (Chờ thu ngân thanh toán & phát thuốc)
                if (record.QueueId.HasValue && record.QueueId.Value > 0)
                {
                    const string updateQueueSql = @"
                        UPDATE Queues 
                        SET Status = 'PendingPayment' 
                        WHERE Id = @QueueId;";
                    conn.Execute(updateQueueSql, new { QueueId = record.QueueId.Value }, transaction);
                }

                // Xác nhận lưu toàn bộ vào CSDL
                transaction.Commit();
                return medicalRecordId;
            }
            catch
            {
                // Nếu có bất kỳ lỗi nào xảy ra thì hoàn tác toàn bộ
                transaction.Rollback();
                throw;
            }
        }

        //Hủy lượt khám / Đánh dấu vắng mặt (Status = 'Cancelled')
        public void CancelPatientQueue(int queueId)
        {
            using var conn = DbContext.GetConnection();
            const string spl = @"
                UPDATE Queues
                SET Status = 'Cancelled'
                WHERE Id = @QueueId;";
            conn.Execute(spl, new { QueueId = queueId });
        }

        //Tra cứu lịch sử khám & hồ sơ bệnh án theo Tên hoặc SĐT bệnh nhân
        public List<dynamic> GetMedicalRecordsHistory(string keyword = "")
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT 
                    mr.Id,
                    'BA-' || printf('%03d', mr.Id) AS RecordCode,
                    mr.PatientId,
                    p.FullName AS PatientName,
                    p.Phone,
                    p.Gender,
                    p.BirthYear,
                    mr.Symptoms,
                    mr.Diagnosis,
                    mr.ExamFee,
                    mr.CreatedAt,
                    COALESCE(u.FullName, 'BS.CKI Nguyễn Văn An') AS DoctorName
                FROM MedicalRecords mr
                INNER JOIN Patients p ON mr.PatientId = p.Id
                LEFT JOIN Users u ON mr.DoctorId = u.Id
                WHERE (@Keyword = '' 
                       OR p.FullName LIKE '%' || @Keyword || '%' 
                       OR p.Phone LIKE '%' || @Keyword || '%')
                ORDER BY mr.Id DESC;";
            return conn.Query<dynamic>(sql, new { Keyword = keyword.Trim() }).ToList();
        }
        //Lấy danh sách các đơn thuốc đã kê (Master)
        public List<dynamic> GetAllPrescriptions(string keyword = "")
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT 
                    pr.Id,
                    'DT-' || printf('%03d', pr.Id) AS PrescriptionCode,
                    pr.MedicalRecordId,
                    p.FullName AS PatientName,
                    p.Phone,
                    mr.Diagnosis,
                    pr.TotalAmount,
                    pr.IsDispensed,
                    CASE WHEN pr.IsDispensed = 1 THEN 'Đã xuất thuốc' ELSE 'Chờ thu phí' END AS DispensedStatus,
                    pr.CreatedAt
                FROM Prescriptions pr
                INNER JOIN MedicalRecords mr ON pr.MedicalRecordId = mr.Id
                INNER JOIN Patients p ON mr.PatientId = p.Id
                WHERE (@Keyword = '' 
                       OR p.FullName LIKE '%' || @Keyword || '%' 
                       OR mr.Diagnosis LIKE '%' || @Keyword || '%')
                ORDER BY pr.Id DESC;";
            return conn.Query<dynamic>(sql, new { Keyword = keyword.Trim() }).ToList();
        }
        //Lấy chi tiết các loại thuốc trong 1 đơn thuốc cụ thể (Detail)
        public List<PrescriptionDetail> GetPrescriptionDetailsById(int prescriptionId)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                SELECT 
                    pd.Id,
                    pd.PrescriptionId,
                    pd.MedicineId,
                    m.Name AS MedicineName,
                    pd.Quantity,
                    pd.Dosage,
                    pd.UnitPrice
                FROM PrescriptionDetails pd
                INNER JOIN Medicines m ON pd.MedicineId = m.Id
                WHERE pd.PrescriptionId = @PrescriptionId
                ORDER BY pd.Id ASC;";
            return conn.Query<PrescriptionDetail>(sql, new { PrescriptionId = prescriptionId }).ToList();
        }
    }
}