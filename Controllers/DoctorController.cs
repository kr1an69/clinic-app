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
            currentStock = 0;
            return false;
        }

        //Lưu bệnh án (MedicalRecords), đơn thuốc (Prescriptions) và chi tiết thuốc (PrescriptionDetails) bằng Transaction
        public int SaveMedicalRecordAndPrescription(MedicalRecord record, List<PrescriptionDetail> details)
        {
            return 0;
        }
    }
}
