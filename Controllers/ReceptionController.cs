using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    // LỄ TÂN, XẾP HÀNG & THỐNG KÊ BỆNH NHÂN 
    public class ReceptionController
    {
        // Lấy danh sách tất cả bệnh nhân từ bảng Patients
        public List<Patient> GetAllPatients()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, FullName, Phone, Gender, BirthDate, Address, CreatedAt
                 FROM Patients
                 ORDER BY Id DESC";
            return conn.Query<Patient>(sql).ToList();
        }

        // Tìm kiếm bệnh nhân theo Họ tên hoặc Số điện thoại
        public List<Patient> SearchPatients(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return GetAllPatients();
            }
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, FullName, Phone, Gender, BirthDate, Address, CreatedAt
                 FROM Patients
                 WHERE FullName LIKE @kw OR Phone LIKE @kw
                 ORDER BY Id DESC";
            return conn.Query<Patient>(sql, new { kw = $"%{keyword.Trim()}%"}).ToList();
        }

        // Thêm mới bệnh nhân vào CSDL và trả về Id vừa tạo
        public int AddPatient(Patient patient)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 INSERT INTO Patients (FullName, Phone, Gender, BirthDate, Address, CreatedAt)
                 VALUES (@FullName, @Phone, @Gender, @BirthDate, @Address, @CreatedAt)
                 SELECT last_insert_rowid();";
            return conn.ExecuteScalar<int>(sql, patient);
        }

        // Cấp số thứ tự khám (QueueNumber tự tăng theo ngày hiện tại)
        public int GenerateQueueNumber(int patientId)
        {
            using var conn = DbContext.GetConnection();
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            // Lấy STT lớn nhất trong ngày
            const string sqlGetMax = @"
                 SELECT IFNULL(MAX(QueueNumber), 0)
                 FROM Patients
                 WHERE QueueDate = @QueueDate";
            int maxNumber = conn.ExecuteScalar<int>(sqlGetMax, new { QueueDate = today });
            int nextNumber = maxNumber + 1;
            // Đưa vào hàng đợi
            const string sqlInsert = @"
                 INSERT INTO Queues (PatientId, QueueNumber, QueueDate, Status)
                 VALUES (@PatientId, @QueueNumber, @QueueDate, 'Waiting')";
            conn.Execute(sqlInsert, new { PatientId = patientId, QueueNumber = nextNumber, QueueDate = today });
            return nextNumber;
        }

        // Lấy danh sách bệnh nhân đang chờ/đang khám trong ngày cho màn hình phòng chờ
        public List<QueueItem> GetCurrentWaitingQueues()
        {
            using var conn = DbContext.GetConnection();
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            const string sql = @"
                 SELECT q.Id, q.QueueNumber, q.QueueDate, q.Status,q.CreatedAt,
                        p.FullName AS PatientName, p.Phone
                 FROM Queues q 
                 INNER JOIN Patients p ON q.PatientId = p.Id
                 WHERE q.QueueDate = @today AND q.Status IN('Waiting', 'Examining')
                 ORDER BY 
                     CASE WHEN q.Status = 'Waiting' THEN 0 ELSE 1 END, q.QueueNumber ASC";
            return conn.Query<QueueItem>(sql, new { today }).ToList();
        }

        // Thống kê số lượng bệnh nhân khám theo ngày
        public List<DailyPatientReportItem> GetDailyPatientStatistics()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT QueueDate AS ExamDate,
                        COUNT(*) AS TotalPatients,
                        SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
                        SUM(CASE WHEN Status = 'Waiting' THEN 1 ELSE 0 END) AS WaitingCount
                 FROM Queues
                 GROUP BY QueueDate
                 ORDER BY QueueDate  DESC";
            return conn.Query<DailyPatientReportItem>(sql).ToList();
        }
    }
}
