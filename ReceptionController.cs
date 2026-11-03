using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    // TIẾP NHÂN, XẾP HÀNG & THỐNG KÊ BỆNH NHÂN 
    
    public class ReceptionController
    {
        // Lấy danh sách tất cả bệnh nhân từ bảng Patients
        public List<Patient> GetAllPatients()
        {
            using var conn = DbContext.GetConnection();
            const string sql = "SELECT * FROM Patient ORDER BY Id DESC";
            return conn.Query<Patient>(sql).ToList();
        }

        // Tìm kiếm bệnh nhân theo Họ tên hoặc Số điện thoại
        public List<Patient> SearchPatients(string keyword)
        {
            using var conn = DbContext.GetConnection();
            const string sql = @"
                 SELECT * FROM Patient
                 WHERE FullName LIKE @keyword OR Phone LIKE @keyword
                 ORDER BY Id DESC;";
            return conn.Query<Patient>(sql, new { keyword = $"%{keyword}%"}).ToList();
        }

        // Thêm mới bệnh nhân vào CSDL và trả về Id vừa tạo
        public int AddPatient(Patient patient)
        {
            using var conn = DbContext.GetConnection();
            const string sql =@"
                 INSERT INTO Patient (FullName, Phone, Gender, BirthYear, Address)
                 VALUES (@FullName, @Phone, @Gender, @BirthYear, @Address);
                 SELECT last_insert_rowid();";
            return conn.ExecuteScalar<int>(sql, patient);
        }

        // Cấp số thứ tự khám (QueueNumber tự tăng theo ngày hiện tại)
        public int GenerateQueueNumber(int patientId)
        {
            using var conn = DbContext.GetConnection();
            // Lấy STT lớn nhất trong ngày
            const string getMaxSql = @"
                 SELECT IFNULL(MAX(QueueNumber), 0)
                 FROM Queue
                 Where QueueDate = datetime('now');";
            int currentMaxNumber = conn.ExecuteScalar<int>(getMaxSql);
            int nextQueueNumber = currentMaxNumber + 1;
            // Thêm bản ghi vào hàng đợi
            const string insertSql =@"
                  INSERT INTO Queue (PatientId, QueueNumber, QueueDate, Status)
                  VALUES (@PatientId, @QueueNumber, date('now'), 'Waiting');";
            conn.Execute(insertSql, new { PatientId = patientId, QueueNumber = nextQueueNumber });
            return nextQueueNumber;
        }

        // Lấy danh sách bệnh nhân đang chờ/đang khám trong ngày cho màn hình phòng chờ
        public List<QueueItem> GetCurrentWaitingQueues()
        {
            using var conn = DbContext.GetConnection();
            const string sql =@"
                 SELECT q.Id, q.PatientId, q.QueueNumber, q.QueueDate, q.Status, q.CreatedAt, p.FullName AS PatientFullName, p.Phone
                 FROM Queue q
                 INNER JOIN Patient p ON q.PatientId = p.Id
                 WHERE q.QueueDate = date('now') AND q.Status IN ('Waiting', 'Examining')
                 ORDER BY q.QueueNumber ASC;";
            return conn.Query<QueueItem>(sql).ToList();
        }

        // Thống kê số lượng bệnh nhân khám theo ngày
        public List<DailyPatientReportItem> GetDailyPatientStatistics()
        {
            using var conn = DbContext.GetConnection();
            const string sql =@"
                  SELECT q.QueueDate AS ExamDate,
                         COUNT(*) AS TotalPatients,
                         SUM(CASE WHEN q.Status = 'Completed' THEN 1 ELSE O END) AS CompletedCount,
                         SUM(CASE WHEN q.Status = 'Wating' THEN 1 ELSE O END) AS WaitingCount
                  FROM Queue q 
                  GROUP BY q.QueueDate
                  ORDER BY q.QueueDate DESC;";
            return conn.Query<DailyPatientReportItem>(sql).ToList();
        }
    }
}
