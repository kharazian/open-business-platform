namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class PlatformPermissions
{
    public static readonly IReadOnlyCollection<string> AllBuiltInPermissions = new[]
    {
        Menu.Dashboard,
        Menu.Forms,
        Menu.Reports,
        Menu.UsersAccess,
        Menu.Settings,
        Menu.Profile,
        Users.Manage,
        Roles.Manage,
        Forms.Create,
        Forms.ManageAll,
        Reports.Manage
    };

    public static readonly IReadOnlyCollection<string> FormActions = new[]
    {
        Form.Submit,
        Form.View,
        Form.Edit,
        Form.Delete,
        Form.Manage
    };

    public static class Menu
    {
        public const string Dashboard = "menu.dashboard";
        public const string Forms = "menu.forms";
        public const string Reports = "menu.reports";
        public const string UsersAccess = "menu.users_access";
        public const string Settings = "menu.settings";
        public const string Profile = "menu.profile";
    }

    public static class Users
    {
        public const string Manage = "users.manage";
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";
    }

    public static class Forms
    {
        public const string Create = "forms.create";
        public const string ManageAll = "forms.manage_all";
    }

    public static class Reports
    {
        public const string Manage = "reports.manage";
    }

    public static class Form
    {
        public const string Submit = "submit";
        public const string View = "view";
        public const string Edit = "edit";
        public const string Delete = "delete";
        public const string Manage = "manage";
    }
}
