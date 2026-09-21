SET IDENTITY_INSERT ws.LookupIssueStatuses ON;

MERGE ws.LookupIssueStatuses AS target
USING (VALUES
    (8, 'Request More Information', 7, CAST(NULL AS INT), 1),
    (9, 'Added More Information',   8, CAST(NULL AS INT), 1)
) AS source (IssueStatusId, IssueStatusName, IssueStatusGroup, NextIssueStatusId, IsActive)
ON target.IssueStatusId = source.IssueStatusId
WHEN MATCHED THEN UPDATE SET
    IssueStatusName = source.IssueStatusName,
    IssueStatusGroup = source.IssueStatusGroup,
    NextIssueStatusId = source.NextIssueStatusId,
    IsActive = source.IsActive
WHEN NOT MATCHED THEN INSERT
    (IssueStatusId, IssueStatusName, IssueStatusGroup, NextIssueStatusId, IsActive)
    VALUES (source.IssueStatusId, source.IssueStatusName, source.IssueStatusGroup,
            source.NextIssueStatusId, source.IsActive);

SET IDENTITY_INSERT ws.LookupIssueStatuses OFF;