
namespace WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;

public class ADUser
{
    public long ADUserID { get; set; }

    public string? ID { get; set; }

    public Guid? ID_Guid { get; set; }

    public string? DisplayName { get; set; }

    public string? GivenName { get; set; }

    public string? Mail { get; set; }

    public string? Surname { get; set; }

    public string? UserPrincipalName { get; set; }

    public string? EmployeeId { get; set; }

    public string? Department { get; set; }

    public string? JobTitle { get; set; }

    public string? Extension { get; set; }

    public bool? AccountEnabled { get; set; }

    public string? Manager_ID { get; set; }
}
