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
            
            // Vòng lập phiên làm việc
            while (true)
            {
                using var loginForm = new FormLogin();
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.LoggedInUser == null)
                {
                    break;
                }
                
                AppSession.CurrentUser = loginForm.LoggedInUser;
                
                var mainForm = new FormMain();
                Application.Run(mainForm);

                if (!mainForm.IsLoggedOut)
                {
                    break;
                }
                
                AppSession.SignOut();
            }
        }
    }
}
