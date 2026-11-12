using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;
using Microsoft.VisualBasic.CompilerServices;

namespace ClinicManagement.Controllers
{
    // LỄ TÂN, XẾP HÀNG & THỐNG KÊ BỆNH NHÂN 
    public class ReceptionController
    {
        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        
        // Xác thực đăng nhập 
        public User? Login(string username, string password)
        {
            if(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;
            
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, Username, FullName, Role
                 FROM Users
                 WHERE Username = @Username AND Password = @Password
                 LIMIT 1";
            return conn.QueryFirstOrDefault<User>(sql, new { Username = username.Trim(), Password = password });
        }

        // Lấy danh sách tất cả bệnh nhân 
        public List<Patient> GetAllPatients()
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, FullName, Phone, Gender, BirthYear, Address, CreatedAt
                 FROM Patients
                 ORDER BY Id DESC";
            return conn.Query<Patient>(sql).ToList();
        }
        
        // Lấy 1 bệnh nhân theo Id
        public Patient? GetPatientById(int id)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, FullName, Phone, Gender, BirthYear, Address, CreatedAt
                 FROM Patients
                 WHERE Id = @Id";
            return conn.QueryFirstOrDefault<Patient>(sql, new { Id = id });
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
                 SELECT Id, FullName, Phone, Gender, BirthYear, Address, CreatedAt
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
                 INSERT INTO Patients (FullName, Phone, Gender, BirthYear, Address)
                 VALUES (@FullName, @Phone, @Gender, @BirthYear, @Address);
                 SELECT last_insert_rowid();";
            return conn.ExecuteScalar<int>(sql, patient);
        }
        
        //Cập nhật thông tin bệnh nhân
        public bool UpdatePatient(Patient patient)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 UPDATE Patients
                 SET FullName = @FullName, Phone = @Phone, Gender = @Gender, BirthYear = @BirthYear, Address = @Address
                 WHERE Id = @Id";
            return conn.Execute(sql, patient) > 0;
        }
        
        // Xóa bệnh nhân 
        public bool DeletePatient(int id)
        {
            using var conn = DbContext.GetConnection();
            const string sqlCheck = @"
                 SELECT (SELECT COUNT(*) FROM Queues WHERE PatientId = @Id)
                      + (SELECT COUNT(*) FROM MedicalRecords WHERE PatientId = @Id)";
            int refs = conn.ExecuteScalar<int>(sqlCheck, new { Id = id });
            if (refs > 0)
            {
                throw new InvalidOperationException("Bệnh nhân này đã có lượt khám/bệnh án nên không thể xóa.");
            }
            return conn.Execute("DELETE FROM Patients WHERE Id = @Id", new { Id = id }) > 0;
        }
        
        // Lịch sử khám của 1 bệnh nhân
        public List<QueueItem> GetPatientVisitHistory(int patientId)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT Id, PatientId, QueueNumber, QueueDate, Status, datetime(CreatedAt, 'localtime') AS CreatedAt
                 FROM Queues
                 WHERE PatientId = @PatientId
                 ORDER BY QueueDate DESC, QueueNumber ASC";
            return conn.Query<QueueItem>(sql, new { PatientId = patientId }).ToList();
        }
        
        // Cấp số thứ tự khám (QueueNumber tự tăng theo ngày hiện tại)
        public int GenerateQueueNumber(int patientId)
        {
            using var conn = DbContext.GetConnection();
            using var tran = conn.BeginTransaction();
            var today = Today;
            
            // Chặn cấp trùng số cho cùng 1 bệnh nhân 
            const string sqlCheck = @"
                 SELECT COUNT(*) 
                 FROM Queues
                 WHERE PatientId = @PatientId AND QueueDate = @QueueDate AND Status IN( 'Waiting', 'Examining', 'PendingPayment')"; 
            int active = conn.ExecuteScalar<int>(sqlCheck, new { PatientId = patientId, QueueDate = today }, tran);
            if (active > 0)
            {
                throw new InvalidOperationException("Bệnh nhân này đã có lượt khám đang xử lý trong ngày hôm nay.");
            }
            
            // Lấy STT lớn nhất trong ngày
            const string sqlGetMax = @"
                 SELECT IFNULL(MAX(QueueNumber), 0)
                 FROM Queues 
                 WHERE QueueDate = @QueueDate";
            int nextNumber = conn.ExecuteScalar<int>(sqlGetMax, new { QueueDate = today }, tran) + 1;
            
            // Đưa vào hàng đợi
            const string sqlInsert = @"
                 INSERT INTO Queues (PatientId, QueueNumber, QueueDate, Status)
                 VALUES (@PatientId, @QueueNumber, @QueueDate, 'Waiting')";
            conn.Execute(sqlInsert, new { PatientId = patientId, QueueNumber = nextNumber, QueueDate = today }, tran);
            tran.Commit();
            return nextNumber;
        }

        // Lấy danh sách bệnh nhân đang chờ/đang khám trong ngày cho màn hình phòng chờ
        public List<QueueItem> GetCurrentWaitingQueues()
        {
            using var conn = DbContext.GetConnection();
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            const string sql = @"
                 SELECT q.Id, q.PatientId, q.QueueNumber, q.QueueDate, q.Status, datetime(q.CreatedAt, 'localtime') AS CreatedAt,
                        p.FullName AS PatientName, p.Phone
                 FROM Queues q 
                 INNER JOIN Patients p ON q.PatientId = p.Id
                 WHERE q.QueueDate = @today AND q.Status IN('Waiting', 'Examining')
                 ORDER BY q.QueueNumber ASC";
            return conn.Query<QueueItem>(sql, new { today = Today }).ToList();
        }

        // Thống kê số lượng bệnh nhân khám theo ngày
        public List<DailyPatientReportItem> GetDailyPatientStatistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT QueueDate AS ExamDate,
                        COUNT(*) AS TotalPatients,
                        SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
                        SUM(CASE WHEN Status IN( 'Waiting', 'Examining', 'PendingPayment') THEN 1 ELSE 0 END) AS WaitingCount
                 FROM Queues
                 WHERE Status <> 'Canceled' 
                   AND (@FromDate IS NULL OR QueueDate >= @FromDate)
                   AND (@ToDate IS NULL OR QueueDate <= @ToDate)
                 GROUP BY QueueDate
                 ORDER BY QueueDate  DESC";
            return conn.Query<DailyPatientReportItem>(sql, new
            {
                FromDate = fromDate?.ToString("yyyy-MM-dd"), 
                ToDate = toDate?.ToString("yyyy-MM-dd")
            }).ToList();
        }
    }
}
