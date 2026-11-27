using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Data;
using System.Data.SQLite;

namespace ClinicManagement.Data
{
    public static class DbContext
    {
        private const string DbFileName = "clinic.db";
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

            // Khởi tạo bảng CSDL & dữ liệu mẫu từ Embedded Resource (fallback đọc file đĩa)
            string? sql = LoadSeedSql();
            if (!string.IsNullOrWhiteSpace(sql))
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static string? LoadSeedSql()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("seed_clinic_data.sql", StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }

            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "seed_clinic_data.sql");
            return File.Exists(scriptPath) ? File.ReadAllText(scriptPath) : null;
        }
    }
}
