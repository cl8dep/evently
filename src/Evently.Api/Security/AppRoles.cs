namespace Evently.Api;

public static class AppRoles
{
    public const string Admin = "Admin", Editor = "Editor", Viewer = "Viewer";
    public static readonly string[] All = [Admin, Editor, Viewer];
}
