using System;
using System.Windows.Forms;
using ClinicManagement.Data;
using ClinicManagement.Views;

namespace ClinicManagement
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Tự động khởi tạo Database SQLite và dữ liệu mẫu nếu chưa tồn tại
            DbContext.InitializeDatabase();

            // Khởi chạy ứng dụng
            Application.Run(new FormMain());
        }
    }
}
