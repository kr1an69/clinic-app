using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ClinicManagement.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    /// <summary>
    /// PHÂN HỆ 3: THU PHÍ, XUẤT THUỐC, IN ĐƠN & BÁO CÁO KHO DƯỢC (Thành viên 3 phụ trách)
    /// </summary>
    public class PharmacyController
    {
        // TODO (Thành viên 3): Lấy toàn bộ danh mục thuốc trong kho
        public List<Medicine> GetAllMedicines()
        {
            return new List<Medicine>();
        }

        // TODO (Thành viên 3): Thêm thuốc mới vào kho
        public void AddMedicine(Medicine medicine)
        {
        }

        // TODO (Thành viên 3): Báo cáo các loại thuốc sắp hết hạn sử dụng hoặc sắp hết hàng trong kho
        public List<Medicine> GetAlertMedicines()
        {
            return new List<Medicine>();
        }

        // TODO (Thành viên 3): Lấy danh sách đơn thuốc đang chờ thu phí & xuất thuốc (IsDispensed = 0)
        public List<dynamic> GetPendingPrescriptions()
        {
            return new List<dynamic>();
        }

        // TODO (Thành viên 3): Thu phí + Xuất thuốc tự động trừ số lượng tồn kho tương ứng (dùng SQLiteTransaction)
        public bool ProcessPaymentAndDispense(int prescriptionId, int cashierId)
        {
            return false;
        }
    }
}
