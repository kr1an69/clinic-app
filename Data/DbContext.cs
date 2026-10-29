using System;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace ClinicManagement.Data
{
    public static class DbContext
    {
        private static readonly string DbFileName = "clinic.db";
        public static string ConnectionString => $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName)};Version=3;";

        public static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(ConnectionString);
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return conn;
        }

        public static void InitializeDatabase()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            // Khởi tạo bảng CSDL & dữ liệu mẫu từ script chuẩn
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "seed_clinic_data.sql");
            if (File.Exists(scriptPath))
            {
                cmd.CommandText = File.ReadAllText(scriptPath);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
