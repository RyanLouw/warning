using System.Data;
using Microsoft.Data.SqlClient;
using WarningSystemTest.SystemUnderTest;

namespace WarningSystemTest.Database;

// All connections come from the already-running Aspire fixture. No application
// connection strings or developer credentials are read by the test project.
public sealed class TestDatabase(WarningSystemSut sut)
{
    public Dictionary<string, long> Warnings { get; } = [];
    public int AttendanceCategory { get; private set; }
    public int ConductCategory { get; private set; }
    public int ReviewNoteType { get; private set; }
    public int DiscussionSubtype { get; private set; }

    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = sut.OpenDatabaseConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, parameters);
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    public async Task ResetAsync()
    {
        await sut.EnsureStartedAsync();
        await using var connection = sut.OpenDatabaseConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        async Task<object?> Execute(string sql, params (string Name, object? Value)[] parameters)
        {
            await using var command = new SqlCommand(sql, connection, transaction);
            AddParameters(command, parameters);
            return await command.ExecuteScalarAsync();
        }
        // Delete only this suite's records. Migration seed data is preserved.
        await Execute("""
            DELETE n FROM ws.WarningNote n JOIN ws.Warning w ON w.WarningId=n.WarningId WHERE w.EmployeeId IN ('test-employee-alice','test-employee-bob','test-employee-cara');
            DELETE e FROM ws.WarningEvidence e JOIN ws.Warning w ON w.WarningId=e.WarningId WHERE w.EmployeeId IN ('test-employee-alice','test-employee-bob','test-employee-cara');
            DELETE a FROM ws.WarningAnswer a JOIN ws.Warning w ON w.WarningId=a.WarningId WHERE w.EmployeeId IN ('test-employee-alice','test-employee-bob','test-employee-cara');
            DELETE c FROM ws.WarningCategory c JOIN ws.Warning w ON w.WarningId=c.WarningId WHERE w.EmployeeId IN ('test-employee-alice','test-employee-bob','test-employee-cara');
            DELETE FROM ws.Warning WHERE EmployeeId IN ('test-employee-alice','test-employee-bob','test-employee-cara');
            """);
        async Task<int> Category(string name)
        {
            var id = Convert.ToInt32(await Execute("""
                IF NOT EXISTS (SELECT 1 FROM ws.TransgressionCategory WHERE Name=@Name)
                    INSERT ws.TransgressionCategory (Name,IsActive,CreatedOn,CreatedBy) VALUES (@Name,1,SYSUTCDATETIME(),'test-user');
                SELECT CategoryId FROM ws.TransgressionCategory WHERE Name=@Name;
                """, ("@Name", name)));
            await Execute("""
                DELETE FROM ws.CategoryQuestion WHERE CategoryId=@Category AND QuestionId NOT IN (1,2,3);
                UPDATE ws.CategoryQuestion SET IsActive=1,IsRequired=CASE WHEN QuestionId=3 THEN 0 ELSE 1 END,SortOrder=QuestionId,ConfigJson=NULL WHERE CategoryId=@Category;
                INSERT ws.CategoryQuestion (CategoryId,QuestionId,IsRequired,SortOrder,IsActive,CreatedOn,CreatedBy)
                SELECT @Category,q.QuestionId,CASE WHEN q.QuestionId=3 THEN 0 ELSE 1 END,q.QuestionId,1,SYSUTCDATETIME(),'test-user'
                FROM ws.Question q WHERE q.QuestionId IN (1,2,3) AND NOT EXISTS (
                    SELECT 1 FROM ws.CategoryQuestion cq WHERE cq.CategoryId=@Category AND cq.QuestionId=q.QuestionId);
                """, ("@Category", id));
            return id;
        }
        AttendanceCategory = await Category("UI Attendance");
        ConductCategory = await Category("UI Conduct");
        async Task<int> NoteType(string name) => Convert.ToInt32(await Execute("""
            IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name=@Name)
                INSERT ws.NoteTypeLookup (Name,CreatedBy,CreatedOn,IsActive,IsSystemOnly) VALUES (@Name,'test-user',SYSUTCDATETIME(),1,0);
            SELECT NoteTypeId FROM ws.NoteTypeLookup WHERE Name=@Name;
            """, ("@Name", name)));
        ReviewNoteType = await NoteType("UI Review");
        var issuedNote = await NoteType("Issued to Employee");
        DiscussionSubtype = Convert.ToInt32(await Execute("SELECT TOP (1) IssueSubTypeId FROM ws.LookupIssueSubTypes WHERE IssueTypeId=3 AND IsActive=1 ORDER BY IssueSubTypeId"));
        if (DiscussionSubtype <= 0) throw new InvalidOperationException("The migrated database must contain an active Discussion subtype.");
        var records = new[] {
            ("Draft", "alice", "Draft", 2, "2026-01-01", false, false),
            ("New", "alice", "New", 2, "2026-01-02", false, false),
            ("Decision", "bob", "In Progress", 2, "2026-01-03", false, false),
            ("Issued", "bob", "Issued", 2, "2026-01-04", false, false),
            ("Validated", "cara", "Validated", 4, "2026-02-01", false, false),
            ("Pending", "alice", "Pending", 2, "2026-02-02", false, false),
            ("Invalid", "bob", "Invalid", 1, "2026-02-03", false, false),
            ("Discussion", "cara", "Validated", 3, "2026-03-01", false, true),
            ("Deleted", "alice", "New", 2, "2026-01-02", true, false)
        };
        Warnings.Clear();
        foreach (var (key, employee, status, type, date, deleted, discussion) in records)
        {
            var category = employee == "bob" ? ConductCategory : AttendanceCategory;
            var id = Convert.ToInt64(await Execute("""
                INSERT ws.Warning (EmployeeId,CreatedBy,CreatedOn,Status,Type,CategoryId,IssueStatusId,IssueTypeId,IssueSubTypeId,IsDeleted,HideFromTeamLead,LegalExpiryDate,LastStatusChangedOn,LastStatusChangedBy)
                OUTPUT INSERTED.WarningId
                SELECT @Employee,'test-user',@Created,s.IssueStatusName,'Issue',@Category,s.IssueStatusId,@Type,
                    CASE WHEN @Type=3 THEN @DiscussionSubtype WHEN @Type=2 THEN (SELECT MIN(IssueSubTypeId) FROM ws.LookupIssueSubTypes WHERE IssueTypeId=2 AND IsActive=1) ELSE NULL END,
                    @Deleted,0,@Due,@Created,'test-user'
                FROM ws.LookupIssueStatuses s WHERE s.IssueStatusName=@Status;
                """, ("@Employee", $"test-employee-{employee}"), ("@Created", DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture)),
                ("@Category", category), ("@Type", type), ("@Status", status), ("@Deleted", deleted),
                ("@Due", discussion ? null : new DateTime(2027, 1, 15)), ("@DiscussionSubtype", DiscussionSubtype)));
            if (id <= 0) throw new InvalidOperationException($"Seed status '{status}' is missing from the migrated database.");
            Warnings[key] = id;
            await Execute("""
                INSERT ws.WarningCategory (WarningId,CategoryId,CreatedOn,CreatedBy) VALUES (@Id,@Category,@Created,'test-user');
                INSERT ws.WarningNote (WarningId,NoteText,CreatedBy,CreatedOn,NoteTypeId) VALUES (@Id,@Note,'test-user',@Created,@NoteType);
                INSERT ws.WarningAnswer (WarningId,QuestionId,AnswerText,AnswerJson,CreatedOn) VALUES (@Id,1,'2026-01-01','{"dates":["2026-01-01"]}',@Created);
                INSERT ws.WarningAnswer (WarningId,QuestionId,AnswerText,AnswerJson,CreatedOn) VALUES (@Id,2,@Description,NULL,@Created);
                INSERT ws.WarningAnswer (WarningId,QuestionId,AnswerText,AnswerJson,CreatedOn) VALUES (@Id,3,NULL,'{"notApplicable":true}',@Created);
                """, ("@Id", id), ("@Category", category), ("@Created", new DateTime(2026, 4, 1).AddMinutes(id % 1000)),
                ("@Note", $"UI note {key}"), ("@NoteType", key is "Issued" or "Validated" ? issuedNote : ReviewNoteType),
                ("@Description", $"UI description {key}"));
        }
        await transaction.CommitAsync();
    }

    private static void AddParameters(SqlCommand command, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
