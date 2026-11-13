using System;
using System.Linq;
using ClinicManagement.Models;

namespace ClinicManagement.Helpers
{
    public static class AppSession
    {
        public const string RoleAdmin = "Admin";
        public const string RoleReceptionist = "Receptionist";
        public const string RoleDoctor = "Doctor";
        public const string RoleCashier = "Cashier";
        
        public static User? CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static void SignIn(User user) => CurrentUser = user;
        public static void SignOut() => CurrentUser = null;
        
        // Kiểm tra vai trò
        public static bool IsAdmin => IsInRole(RoleAdmin);
        public static bool IsReceptionist => IsInRole(RoleReceptionist);
        public static bool IsDoctor => IsInRole(RoleDoctor);
        public static bool IsCashier => IsInRole(RoleCashier);
        
        // Kiểm tra vai trò có nằm trong danh sách không
        public static bool IsInRole(params string[] roles) => 
            CurrentUser != null && roles.Contains(CurrentUser.Role, StringComparer.OrdinalIgnoreCase);
        
        // Hiển thị tên vai trò tiếng Việt
        public static string GetRoleDisplayName(string role) => role switch
        {
            RoleAdmin => "Quản trị viên",
            RoleReceptionist => "Lễ tân",
            RoleDoctor => "Bác sĩ",
            RoleCashier => "Dược sĩ",
            _ => role
        };
    }
}