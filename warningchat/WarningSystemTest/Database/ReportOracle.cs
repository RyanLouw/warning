namespace WarningSystemTest.Database;

public sealed record ReportFilter(string? Department = null, string? Manager = null, string? Employee = null,
    string? Status = null, string? IssueType = null, string? RecordKind = null, string? LatestAction = null,
    string? Search = null, string? StartDate = null, string? EndDate = null);

public static class ReportOracle
{
    // Independent SQL, separate connection. No application manager, mapper,
    // API response, DOM attributes, or JavaScript is used to calculate expected rows.
    public static Task<List<Dictionary<string, object?>>> QueryAsync(TestDatabase database, ReportFilter filter, string role = "User") => database.QueryAsync("""
        WITH Directory(EmployeeId,Employee,Department) AS (
            SELECT * FROM (VALUES
                ('test-employee-alice','UI Alice','Sales'),
                ('test-employee-bob','UI Bob','Support'),
                ('test-employee-cara','UI Cara','Sales')
            ) e(EmployeeId,Employee,Department)
        ), Report AS (
            SELECT w.WarningId,e.Employee,e.Department,@ManagerName AS Manager,'Test User' AS Creator,
                w.Status,COALESCE(t.IssueTypeName,'') AS IssueType,COALESCE(st.IssueSubTypeName,'') AS Subtype,
                COALESCE(categories.Names,c.Name,'Other/Unknown') AS Category,w.CreatedOn,w.LegalExpiryDate,
                COALESCE(latest.Name,'') AS LatestAction,latest.CreatedOn AS LatestActionDate,
                CAST(CASE WHEN w.IssueTypeId=3 AND w.LegalExpiryDate IS NULL THEN 1 ELSE 0 END AS bit) AS IsAbsenceDiscussion
            FROM ws.Warning w JOIN Directory e ON e.EmployeeId=w.EmployeeId
            LEFT JOIN ws.LookupIssueTypes t ON t.IssueTypeId=w.IssueTypeId
            LEFT JOIN ws.LookupIssueSubTypes st ON st.IssueSubTypeId=w.IssueSubTypeId
            LEFT JOIN ws.TransgressionCategory c ON c.CategoryId=w.CategoryId
            OUTER APPLY (
                SELECT STRING_AGG(category.Name,', ') WITHIN GROUP (ORDER BY category.Name) AS Names
                FROM ws.WarningCategory link JOIN ws.TransgressionCategory category ON category.CategoryId=link.CategoryId
                WHERE link.WarningId=w.WarningId
            ) categories
            OUTER APPLY (
                SELECT TOP (1) n.CreatedOn,nt.Name FROM ws.WarningNote n
                LEFT JOIN ws.NoteTypeLookup nt ON nt.NoteTypeId=n.NoteTypeId
                WHERE n.WarningId=w.WarningId ORDER BY n.CreatedOn DESC,n.NoteId DESC
            ) latest
            WHERE w.IsDeleted=0 AND (@Legal=0 OR LTRIM(RTRIM(w.Status))<>'Draft')
        )
        SELECT * FROM Report
        WHERE (@Department IS NULL OR Department=@Department)
            AND (@Manager IS NULL OR Manager=@Manager)
            AND (@Employee IS NULL OR Employee=@Employee)
            AND (@Status IS NULL OR Status=@Status)
            AND (@IssueType IS NULL OR IssueType=@IssueType)
            AND (@Kind IS NULL OR IsAbsenceDiscussion=CASE WHEN @Kind='absence-discussion' THEN 1 ELSE 0 END)
            AND (@Action IS NULL OR LatestAction=@Action)
            AND (@Start IS NULL OR CAST(CreatedOn AS date)>=CAST(@Start AS date))
            AND (@End IS NULL OR CAST(CreatedOn AS date)<=CAST(@End AS date))
            AND (@Search IS NULL OR CHARINDEX(LOWER(@Search),LOWER(CONCAT(Employee,' ',Department,' ',Manager,' ',Creator,' ',Status,' ',LatestAction,' ',IssueType)))>0)
        ORDER BY WarningId;
        """, ("@ManagerName", $"Test {role}"), ("@Legal", role == "Legal"),
        ("@Department", Clean(filter.Department)), ("@Manager", Clean(filter.Manager)), ("@Employee", Clean(filter.Employee)),
        ("@Status", Clean(filter.Status)), ("@IssueType", Clean(filter.IssueType)), ("@Kind", Clean(filter.RecordKind)),
        ("@Action", Clean(filter.LatestAction)), ("@Search", Clean(filter.Search)), ("@Start", Clean(filter.StartDate)), ("@End", Clean(filter.EndDate)));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
