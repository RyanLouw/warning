UPDATE ws.LookupIssueTypes
SET SubTypeSelectionMode = 2
WHERE IssueTypeId = 3;

SET IDENTITY_INSERT ws.LookupIssueSubTypes ON;
MERGE ws.LookupIssueSubTypes AS target
USING (VALUES
    (5, 3, 'Absenteeism', 1),
    (6, 3, 'Training',    1),
    (7, 3, 'Other',       1)
) AS source (IssueSubTypeId, IssueTypeId, IssueSubTypeName, IsActive)
ON target.IssueSubTypeId = source.IssueSubTypeId
WHEN MATCHED THEN UPDATE SET
    IssueTypeId = source.IssueTypeId,
    IssueSubTypeName = source.IssueSubTypeName,
    IsActive = source.IsActive
WHEN NOT MATCHED THEN INSERT
    (IssueSubTypeId, IssueTypeId, IssueSubTypeName, IsActive)
    VALUES (source.IssueSubTypeId, source.IssueTypeId, source.IssueSubTypeName, source.IsActive);
SET IDENTITY_INSERT ws.LookupIssueSubTypes OFF;
