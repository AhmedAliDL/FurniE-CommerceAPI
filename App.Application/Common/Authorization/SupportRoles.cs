namespace App.Application.Common.Authorization
{
    public static class SupportRoles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
        public const string SupportAgent = "SupportAgent";

        public static bool IsSupportStaff(IEnumerable<string> roles) =>
            roles.Contains(Admin) || roles.Contains(SupportAgent);
    }
}
