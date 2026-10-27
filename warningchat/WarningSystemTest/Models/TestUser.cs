namespace WarningSystemTest.Models;

public sealed record TestUser(string Profile, string ObjectId, string DisplayName, string Role);

public static class TestUsers
{
    public static TestUser ForRole(string role) => role switch
    {
        "User" => new("User", "test-user", "Test User", "User"),
        "Legal" => new("Legal", "test-legal", "Test Legal", "Legal"),
        "Admin" => new("Admin", "test-admin", "Test Admin", "Admin"),
        "Chairperson" => new("Chairperson", "test-chairperson", "Test Chairperson", "Chairperson"),
        _ => throw new ArgumentException("Unknown test role.", nameof(role))
    };
}
