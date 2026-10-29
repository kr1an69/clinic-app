using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    /// <summary>
    /// PHÂN HỆ 2: BÁC SĨ KHÁM BỆNH & KÊ ĐƠN THUỐC (Thành viên 2 phụ trách)
    /// </summary>
    public class DoctorController
    {
        // TODO (Thành viên 2): Lấy danh sách bệnh nhân đang chờ khám (Status = 'Waiting') theo số thứ tự
        public List<QueueItem> GetPatientsWaitingForDoctor()
        {
            return new List<QueueItem>();
        }

        // TODO (Thành viên 2): Cập nhật trạng thái hàng đợi sang 'Examining' khi bác sĩ gọi khám
        public void CallPatient(int queueId)
        {
        }

        // TODO (Thành viên 2): Lấy danh sách thuốc còn tồn kho (StockQuantity > 0) để gợi ý AutoComplete
        public List<Medicine> GetAllMedicinesForPrescription()
        {
            return new List<Medicine>();
        }

        // TODO (Thành viên 2): Kiểm tra tồn kho thuốc xem có đủ số lượng bác sĩ kê hay không
        public bool CheckMedicineStock(int medicineId, int requestedQty, out int currentStock)
        {
            currentStock = 0;
            return false;
        }

        // TODO (Thành viên 2): Lưu bệnh án (MedicalRecords), đơn thuốc (Prescriptions) và chi tiết thuốc (PrescriptionDetails) bằng Transaction
        public int SaveMedicalRecordAndPrescription(MedicalRecord record, List<PrescriptionDetail> details)
        {
            return 0;
        }
    }
}
