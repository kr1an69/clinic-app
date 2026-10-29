using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    /// <summary>
    /// PHÂN HỆ 1: LỄ TÂN, XẾP HÀNG & THỐNG KÊ BỆNH NHÂN (Thành viên 1 phụ trách)
    /// </summary>
    public class ReceptionController
    {
        // TODO (Thành viên 1): Lấy danh sách tất cả bệnh nhân từ bảng Patients
        public List<Patient> GetAllPatients()
        {
            return new List<Patient>();
        }

        // TODO (Thành viên 1): Tìm kiếm bệnh nhân theo Họ tên hoặc Số điện thoại
        public List<Patient> SearchPatients(string keyword)
        {
            return new List<Patient>();
        }

        // TODO (Thành viên 1): Thêm mới bệnh nhân vào CSDL và trả về Id vừa tạo
        public int AddPatient(Patient patient)
        {
            return 0;
        }

        // TODO (Thành viên 1): Cấp số thứ tự khám (QueueNumber tự tăng theo ngày hiện tại)
        public int GenerateQueueNumber(int patientId)
        {
            return 0;
        }

        // TODO (Thành viên 1): Lấy danh sách bệnh nhân đang chờ/đang khám trong ngày cho màn hình phòng chờ
        public List<QueueItem> GetCurrentWaitingQueues()
        {
            return new List<QueueItem>();
        }

        // TODO (Thành viên 1): Thống kê số lượng bệnh nhân khám theo ngày
        public List<DailyPatientReportItem> GetDailyPatientStatistics()
        {
            return new List<DailyPatientReportItem>();
        }
    }
}
