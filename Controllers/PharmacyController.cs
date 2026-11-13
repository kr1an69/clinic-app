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
    }
}
