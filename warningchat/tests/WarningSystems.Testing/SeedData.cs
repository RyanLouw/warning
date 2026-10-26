using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Moq;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Testing;

public static class SeedData
{
    public const string ManagerId = "test-manager";
    public static readonly User[] Employees = [
        new() { Id = "alice", DisplayName = "Alice Adams", Department = "Sales", Mail = "alice@example.test" },
        new() { Id = "bob", DisplayName = "Bob Brown", Department = "Support", Mail = "bob@example.test" },
        new() { Id = "cara", DisplayName = "Cara Clark", Department = "Sales", Mail = "cara@example.test" }
    ];
    public static readonly User Manager = new() { Id = ManagerId, DisplayName = "Test Manager", Mail = "manager@example.test" };
    public static readonly User Outsider = new() { Id = "outsider", DisplayName = "Outside Employee", Department = "Finance", Mail = "outside@example.test" };

    public static Mock<IGraphUserDataAccess> CreateGraph()
    {
        var graph = new Mock<IGraphUserDataAccess>(MockBehavior.Strict);
        graph.Setup(x => x.GetMeAsync()).ReturnsAsync(Manager);
        graph.Setup(x => x.GetUsersBelowMyLevelAsync()).ReturnsAsync(Employees.ToList());
        graph.Setup(x => x.GetUsersUnderMeAsync()).ReturnsAsync(Employees.ToList());
        graph.Setup(x => x.GetDirectReportsUsersAsync(It.IsAny<string>())).ReturnsAsync(Employees.ToList());
        graph.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync((string id) =>
            Employees.Append(Manager).Append(Outsider).FirstOrDefault(u => u.Id == id || u.Mail == id));
        graph.Setup(x => x.GetManagerAsync(It.IsAny<string>())).ReturnsAsync(Manager);
        graph.Setup(x => x.GetMatchedGroupIdsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(Array.Empty<string>());
        // Sending mail is deliberately not configured: an unexpected send fails the test.
        return graph;
    }

    public static async Task InsertAsync(WarningSystemDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var statuses = new[] {
                (1, "Draft", IssueStatusGroup.Draft), (2, "New", IssueStatusGroup.LegalReview),
                (3, "In Progress", IssueStatusGroup.LegalDecision), (4, "Pending", IssueStatusGroup.EmployeeAction),
                (5, "Completed", IssueStatusGroup.LegalValidation), (6, "Validated", IssueStatusGroup.Closed),
                (7, "More Information Required", IssueStatusGroup.RequestMoreInformation), (8, "Added More Information", IssueStatusGroup.AddedMoreInformation)
            };
            await InsertIdentityAsync(db, "LookupIssueStatuses", statuses.Select(s => new LookupIssueStatus { IssueStatusId = s.Item1, IssueStatusName = s.Item2, IssueStatusGroup = s.Item3 }));
            foreach (var (id, next) in new[] { (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (7, 8), (8, 3) })
            {
                var status = await db.LookupIssueStatuses.FindAsync(id);
                status!.NextIssueStatusId = next;
            }
            await db.SaveChangesAsync();
            await InsertIdentityAsync(db, "LookupIssueTypes", new[] {
                new LookupIssueType { IssueTypeId = 1, IssueTypeName = "Invalid", ResultIssueStatusId = 6 },
                new LookupIssueType { IssueTypeId = 2, IssueTypeName = "Warning", ResultIssueStatusId = 4, SubTypeSelectionMode = SubTypeSelectionMode.Required },
                new LookupIssueType { IssueTypeId = 3, IssueTypeName = "Discussion", ResultIssueStatusId = 4, SubTypeSelectionMode = SubTypeSelectionMode.Required },
                new LookupIssueType { IssueTypeId = 4, IssueTypeName = "Hearing Notice", ResultIssueStatusId = 4 }
            });
            await InsertIdentityAsync(db, "LookupIssueSubTypes", new[] {
                new LookupIssueSubType { IssueSubTypeId = 1, IssueTypeId = 2, IssueSubTypeName = "Written warning" },
                new LookupIssueSubType { IssueSubTypeId = 2, IssueTypeId = 3, IssueSubTypeName = "Absence discussion" }
            });
            await InsertIdentityAsync(db, "TransgressionCategory", new[] {
                new TransgressionCategory { CategoryId = 1, Name = "Safety", IsActive = true, CreatedBy = ManagerId },
                new TransgressionCategory { CategoryId = 2, Name = "Attendance", IsActive = true, CreatedBy = ManagerId },
                new TransgressionCategory { CategoryId = 31, Name = "Other/Unknown", IsActive = true, CreatedBy = ManagerId }
            });
            await InsertIdentityAsync(db, "Question", new[] {
                new Question { QuestionId = 1, QuestionText = "Dates", ControlType = "date", IsActive = true, CreatedBy = ManagerId },
                new Question { QuestionId = 2, QuestionText = "Description", ControlType = "textarea", IsActive = true, CreatedBy = ManagerId },
                new Question { QuestionId = 3, QuestionText = "SOP", ControlType = "text", IsActive = true, CreatedBy = ManagerId }
            });
            await InsertIdentityAsync(db, "NoteTypeLookup", new[] {
                new NoteTypeLookup { NoteTypeId = 1, Name = "General", IsActive = true, CreatedBy = ManagerId },
                new NoteTypeLookup { NoteTypeId = 2, Name = "Issued to Employee", IsActive = true, CreatedBy = ManagerId },
                new NoteTypeLookup { NoteTypeId = 3, Name = "Legal review", IsActive = true, CreatedBy = ManagerId }
            });
            var records = new[] {
                (1L, "alice", 1, 2, "2026-01-01", false, false),
                (2L, "alice", 2, 2, "2026-01-02", false, false),
                (3L, "bob", 3, 3, "2026-01-03", false, false),
                (4L, "bob", 5, 3, "2026-01-04", false, true),
                (5L, "cara", 6, 4, "2026-02-01", false, false),
                (6L, "alice", 5, 2, "2026-02-02", false, false),
                (7L, "bob", 4, 1, "2026-02-03", false, false),
                (8L, "outsider", 2, 2, "2026-02-04", false, false),
                (9L, "cara", 3, 3, "2026-03-01", false, true),
                (10L, "cara", 6, 1, "2026-03-02", false, false),
                (11L, "alice", 4, 4, "2026-03-03", false, false),
                (12L, "bob", 7, 2, "2026-03-04", false, false),
                (13L, "cara", 8, 2, "2026-03-05", false, false),
                (14L, "alice", 2, 2, "2026-01-02", true, false)
            };
            await InsertIdentityAsync(db, "Warning", records.Select(r => new Warning {
                WarningId = r.Item1, EmployeeId = r.Item2, IssueStatusId = r.Item3,
                Status = statuses.Single(s => s.Item1 == r.Item3).Item2, IssueTypeId = r.Item4,
                IssueSubTypeId = r.Item4 == 2 ? 1 : r.Item4 == 3 ? 2 : null,
                CategoryId = r.Item2 == "bob" ? 2 : 1, CreatedBy = ManagerId,
                CreatedOn = DateTime.Parse(r.Item5, System.Globalization.CultureInfo.InvariantCulture),
                IsDeleted = r.Item6, LegalExpiryDate = r.Item7 ? null : new DateOnly(2027, 1, 15),
                LastStatusChangedBy = ManagerId, LastStatusChangedOn = DateTime.Parse(r.Item5, System.Globalization.CultureInfo.InvariantCulture)
            }));
            foreach (var record in records)
            {
                db.WarningCategories.Add(new WarningCategory { WarningId = record.Item1, CategoryId = record.Item2 == "bob" ? 2 : 1, CreatedBy = ManagerId });
                db.WarningNotes.Add(new WarningNote { WarningId = record.Item1, NoteTypeId = record.Item1 % 2 == 0 ? 2 : 3,
                    NoteText = $"Seed note for issue {record.Item1}", CreatedBy = ManagerId, CreatedOn = new DateTime(2026, 4, 1).AddMinutes(record.Item1) });
            }
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync($"CREATE TABLE {(db.Database.IsSqlServer() ? "ws." : "")}TestDirectory (EmployeeId varchar(50) PRIMARY KEY, DisplayName varchar(100) NOT NULL, Department varchar(100) NOT NULL, IsDirectReport bit NOT NULL)");
            foreach (var user in Employees.Append(Manager).Append(Outsider))
            {
                var direct = Employees.Any(employee => employee.Id == user.Id);
                var table = db.Database.IsSqlServer() ? "ws.TestDirectory" : "TestDirectory";
                await db.Database.ExecuteSqlRawAsync($"INSERT INTO {table} VALUES ({{0}}, {{1}}, {{2}}, {{3}})", user.Id!, user.DisplayName!, user.Department ?? string.Empty, direct);
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static async Task InsertIdentityAsync<T>(WarningSystemDbContext db, string table, IEnumerable<T> records) where T : class
    {
        // table is always a source-code constant above, never request input.
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [ws].[{table}] ON");
        try { db.Set<T>().AddRange(records); await db.SaveChangesAsync(); }
        finally { if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [ws].[{table}] OFF"); }
    }
}
