using Microsoft.Data.SqlClient;

namespace WarningSystems.Testing;

public sealed record ReportFilter(
    string? Department = null, string? Manager = null, string? Employee = null,
    string? Status = null, string? IssueType = null, string? RecordKind = null,
    string? LatestAction = null, string? Search = null,
    string? StartDate = null, string? EndDate = null);

public sealed record ReportRecord(long Id, string Employee, string Department, string Manager,
    string Creator, string Status, string IssueType, string Subtype, string Category,
    DateTime Created, DateTime? Due, string LatestAction, DateTime? LatestActionDate,
    bool IsAbsenceDiscussion, bool Issued);

public static class ReportOracle
{
    // This runs SQL against a separate connection, not the app's manager, mapper,
    // API response, browser attributes, or JavaScript filter implementation.
    public static async Task<List<ReportRecord>> QueryAsync(WarningApplication app, ReportFilter filter, string roles = "User,Legal")
    {
        const string sql = """
            WITH Report AS (
                SELECT w.WarningId,
                    e.DisplayName AS Employee,
                    CASE WHEN e.IsDirectReport = 1 THEN e.Department ELSE '' END AS Department,
                    CASE WHEN e.IsDirectReport = 1 THEN 'Test Manager' ELSE '' END AS Manager,
                    creator.DisplayName AS Creator, w.Status,
                    COALESCE(t.IssueTypeName, '') AS IssueType,
                    COALESCE(st.IssueSubTypeName, '') AS Subtype,
                    c.Name AS Category, w.CreatedOn,
                    CAST(w.LegalExpiryDate AS datetime2) AS LegalExpiryDate,
                    COALESCE(latest.Name, '') AS LatestAction, latest.CreatedOn AS LatestActionDate,
                    CAST(CASE WHEN w.IssueTypeId = 3 AND w.LegalExpiryDate IS NULL THEN 1 ELSE 0 END AS bit) AS IsAbsenceDiscussion,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM ws.WarningNote n JOIN ws.NoteTypeLookup nt ON nt.NoteTypeId = n.NoteTypeId
                        WHERE n.WarningId = w.WarningId AND nt.Name = 'Issued to Employee'
                    ) THEN 1 ELSE 0 END AS bit) AS Issued
                FROM ws.Warning w
                JOIN ws.TestDirectory e ON e.EmployeeId = w.EmployeeId
                JOIN ws.TestDirectory creator ON creator.EmployeeId = w.CreatedBy
                JOIN ws.TransgressionCategory c ON c.CategoryId = w.CategoryId
                LEFT JOIN ws.LookupIssueTypes t ON t.IssueTypeId = w.IssueTypeId
                LEFT JOIN ws.LookupIssueSubTypes st ON st.IssueSubTypeId = w.IssueSubTypeId
                OUTER APPLY (
                    SELECT TOP (1) nt.Name, n.CreatedOn
                    FROM ws.WarningNote n LEFT JOIN ws.NoteTypeLookup nt ON nt.NoteTypeId = n.NoteTypeId
                    WHERE n.WarningId = w.WarningId
                    ORDER BY n.CreatedOn DESC, n.NoteId DESC
                ) latest
                WHERE w.IsDeleted = 0 AND (
                    @Combined = 1 OR
                    (@Legal = 1 AND w.Status <> 'Draft') OR
                    (@Legal = 0 AND @User = 1 AND e.IsDirectReport = 1)
                )
            )
            SELECT * FROM Report
            WHERE (@Department IS NULL OR Department = @Department)
                AND (@Manager IS NULL OR Manager = @Manager)
                AND (@Employee IS NULL OR Employee = @Employee)
                AND (@Status IS NULL OR Status = @Status)
                AND (@IssueType IS NULL OR IssueType = @IssueType)
                AND (@Kind IS NULL OR IsAbsenceDiscussion = CASE WHEN @Kind = 'absence-discussion' THEN 1 ELSE 0 END)
                AND (@Action IS NULL OR LatestAction = @Action)
                AND (@Start IS NULL OR CAST(CreatedOn AS date) >= CAST(@Start AS date))
                AND (@End IS NULL OR CAST(CreatedOn AS date) <= CAST(@End AS date))
                AND (@Search IS NULL OR CHARINDEX(LOWER(@Search), LOWER(CONCAT(Employee, ' ', Department, ' ', Manager, ' ', Creator, ' ', Status, ' ', LatestAction, ' ', IssueType))) > 0)
            ORDER BY WarningId
            """;
        var query = sql;
        if (!app.IsSqlServer)
        {
            var applyStart = query.IndexOf("OUTER APPLY", StringComparison.Ordinal);
            var applyEnd = query.IndexOf(") latest", applyStart, StringComparison.Ordinal) + ") latest".Length;
            query = query[..applyStart] + """
                LEFT JOIN (
                    SELECT n.WarningId, n.NoteId, nt.Name, n.CreatedOn
                    FROM WarningNote n LEFT JOIN NoteTypeLookup nt ON nt.NoteTypeId = n.NoteTypeId
                ) latest ON latest.NoteId = (
                    SELECT n2.NoteId FROM WarningNote n2 WHERE n2.WarningId = w.WarningId
                    ORDER BY n2.CreatedOn DESC, n2.NoteId DESC LIMIT 1
                )
                """ + query[applyEnd..];
            query = query.Replace("ws.", "")
                .Replace("CAST(w.LegalExpiryDate AS datetime2)", "w.LegalExpiryDate")
                .Replace("CAST(CreatedOn AS date)", "date(CreatedOn)")
                .Replace("CAST(@Start AS date)", "date(@Start)")
                .Replace("CAST(@End AS date)", "date(@End)")
                .Replace("CHARINDEX(LOWER(@Search), LOWER(CONCAT(Employee, ' ', Department, ' ', Manager, ' ', Creator, ' ', Status, ' ', LatestAction, ' ', IssueType)))", "instr(LOWER(Employee || ' ' || Department || ' ' || Manager || ' ' || Creator || ' ' || Status || ' ' || LatestAction || ' ' || IssueType), LOWER(@Search))");
        }
        await using var connection = app.OpenSqlConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = query;
        void Parameter(string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        var user = roles.Split(',').Contains("User");
        var legal = roles.Split(',').Contains("Legal");
        Parameter("@Combined", user && legal);
        Parameter("@Legal", legal);
        Parameter("@User", user);
        foreach (var (name, value) in new[] {
            ("@Department", filter.Department), ("@Manager", filter.Manager), ("@Employee", filter.Employee),
            ("@Status", filter.Status), ("@IssueType", filter.IssueType), ("@Kind", filter.RecordKind),
            ("@Action", filter.LatestAction), ("@Search", filter.Search), ("@Start", filter.StartDate), ("@End", filter.EndDate)
        }) Parameter(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());
        var records = new List<ReportRecord>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            records.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
                reader.GetDateTime(9), reader.IsDBNull(10) ? null : reader.GetDateTime(10), reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetDateTime(12), reader.GetBoolean(13), reader.GetBoolean(14)));
        return records;
    }
}
