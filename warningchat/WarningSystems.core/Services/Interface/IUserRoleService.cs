namespace WarningSystems.Core.Services.Interface;

public interface IUserRoleService
{
    Task<IReadOnlyList<string>> GetCurrentUserRolesAsync();

    Task<bool> CurrentUserIsInRoleAsync(string role);
}