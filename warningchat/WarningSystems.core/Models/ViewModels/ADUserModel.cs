using WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;

namespace WarningSystems.core.Models.ViewModels;

public class ADUserModel
{
    public ADUserModel()
    {
    }

    public ADUserModel(ADUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ADUserID = user.ADUserID;
        ID = user.ID;
        ID_Guid = user.ID_Guid;
        DisplayName = user.DisplayName;
        GivenName = user.GivenName;
        Mail = user.Mail;
        Surname = user.Surname;
        UserPrincipalName = user.UserPrincipalName;
        EmployeeId = user.EmployeeId;
        Department = user.Department;
        JobTitle = user.JobTitle;
        Extension = user.Extension;
        AccountEnabled = user.AccountEnabled;
        Manager_ID = user.Manager_ID;
    }

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