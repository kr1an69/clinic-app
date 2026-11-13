using System;
using System.Windows.Forms;
using ClinicManagement.Data;
using ClinicManagement.Views;
using ClinicManagement.Helpers;

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

            using var loginForm = new FormLogin();
            if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoggedInUser != null)
            {
                AppSession.CurrentUser = loginForm.LoggedInUnser;
                Application.Run(new FormMain());
            }
        }
    }
}
