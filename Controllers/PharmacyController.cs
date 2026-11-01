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
    }
}
