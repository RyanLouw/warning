using Microsoft.Graph.Models;

namespace WarningSystems.Core.ViewModels;

public class TaskListVM
{
    public List<TransgretionsVM> TaskList { get; set; } = [];

    public List<TransgretionsVM> ReportRows { get; set; } = [];

    public List<User> UnderME { get; set; } = [];
    public List<EmployeeRowVm> EmployeeRows { get; set; } = [];

    public int DraftCount { get; set; }
    public int New { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Validated { get; set; }
    public int Invalid { get; set; }
    public int Due { get; set; }
    public int OverDue { get; set; }

    public List<CategoryVM> Category { get; set; } = [];

    public List<IssueSubTypeLookupVm> DiscussionSubTypes { get; set; } = [];

    public IReadOnlyList<string> CurrentUserRoles { get; set; }
        = [];

    public string CurrentUserId { get; set; }
        = string.Empty;

    public TaskListVM()
    {
    }

    public TaskListVM(
        List<TransgretionsVM> rows,
        List<User> underme)
    {
        TaskList = rows;
        UnderME = underme;
    }
}
