/* Database-driven issue types, subtypes, statuses, and next steps. */
IF OBJECT_ID('ws.Warning', 'U') IS NULL
    THROW 50011, 'The ws.Warning table must exist before adding issue lookups.', 1;

IF OBJECT_ID('ws.LookupIssueStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE ws.LookupIssueStatuses
    (
        IssueStatusId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LookupIssueStatuses PRIMARY KEY,
        IssueStatusName VARCHAR(100) NOT NULL,
        IssueStatusGroup TINYINT NOT NULL,
        NextIssueStatusId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_LookupIssueStatuses_IsActive DEFAULT (1),
        CONSTRAINT UQ_LookupIssueStatuses_Name UNIQUE (IssueStatusName),
        CONSTRAINT FK_LookupIssueStatuses_Next FOREIGN KEY (NextIssueStatusId)
            REFERENCES ws.LookupIssueStatuses(IssueStatusId)
    );
END;

SET IDENTITY_INSERT ws.LookupIssueStatuses ON;
MERGE ws.LookupIssueStatuses AS target
USING (VALUES
    (1, 'Draft',         1, CAST(2 AS INT), 1),
    (2, 'New',           2, CAST(3 AS INT), 1),
    (3, 'In Progress',   3, CAST(NULL AS INT), 1),
    (4, 'Pending',       4, CAST(5 AS INT), 1),
    (5, 'Issued',        5, CAST(6 AS INT), 1),
    (6, 'Validated',     6, CAST(NULL AS INT), 1),
    (7, 'Invalid',       6, CAST(NULL AS INT), 1)
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

IF OBJECT_ID('ws.LookupIssueTypes', 'U') IS NULL
BEGIN
    CREATE TABLE ws.LookupIssueTypes
    (
        IssueTypeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LookupIssueTypes PRIMARY KEY,
        IssueTypeName VARCHAR(100) NOT NULL,
        SubTypeSelectionMode TINYINT NOT NULL,
        ResultIssueStatusId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_LookupIssueTypes_IsActive DEFAULT (1),
        CONSTRAINT UQ_LookupIssueTypes_Name UNIQUE (IssueTypeName),
        CONSTRAINT FK_LookupIssueTypes_ResultStatus FOREIGN KEY (ResultIssueStatusId)
            REFERENCES ws.LookupIssueStatuses(IssueStatusId)
    );
END;

SET IDENTITY_INSERT ws.LookupIssueTypes ON;
MERGE ws.LookupIssueTypes AS target
USING (VALUES
    (1, 'Invalid',    0, 7, 1),
    (2, 'Warning',    2, 4, 1),
    (3, 'Discussion', 0, 4, 1),
    (4, 'Hearing',    0, 4, 1)
) AS source (IssueTypeId, IssueTypeName, SubTypeSelectionMode, ResultIssueStatusId, IsActive)
ON target.IssueTypeId = source.IssueTypeId
WHEN MATCHED THEN UPDATE SET
    IssueTypeName = source.IssueTypeName,
    SubTypeSelectionMode = source.SubTypeSelectionMode,
    ResultIssueStatusId = source.ResultIssueStatusId,
    IsActive = source.IsActive
WHEN NOT MATCHED THEN INSERT
    (IssueTypeId, IssueTypeName, SubTypeSelectionMode, ResultIssueStatusId, IsActive)
    VALUES (source.IssueTypeId, source.IssueTypeName, source.SubTypeSelectionMode,
            source.ResultIssueStatusId, source.IsActive);
SET IDENTITY_INSERT ws.LookupIssueTypes OFF;

IF OBJECT_ID('ws.LookupIssueSubTypes', 'U') IS NULL
BEGIN
    CREATE TABLE ws.LookupIssueSubTypes
    (
        IssueSubTypeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LookupIssueSubTypes PRIMARY KEY,
        IssueTypeId INT NOT NULL,
        IssueSubTypeName VARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_LookupIssueSubTypes_IsActive DEFAULT (1),
        CONSTRAINT UQ_LookupIssueSubTypes_TypeName UNIQUE (IssueTypeId, IssueSubTypeName),
        CONSTRAINT FK_LookupIssueSubTypes_Type FOREIGN KEY (IssueTypeId)
            REFERENCES ws.LookupIssueTypes(IssueTypeId)
    );
END;

SET IDENTITY_INSERT ws.LookupIssueSubTypes ON;
MERGE ws.LookupIssueSubTypes AS target
USING (VALUES
    (1, 2, 'Verbal warning',      1),
    (2, 2, '1st written warning', 1),
    (3, 2, '2nd written warning', 1),
    (4, 2, 'Final warning',       1)
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

IF COL_LENGTH('ws.Warning', 'IssueStatusId') IS NULL ALTER TABLE ws.Warning ADD IssueStatusId INT NULL;
IF COL_LENGTH('ws.Warning', 'IssueTypeId') IS NULL ALTER TABLE ws.Warning ADD IssueTypeId INT NULL;
IF COL_LENGTH('ws.Warning', 'IssueSubTypeId') IS NULL ALTER TABLE ws.Warning ADD IssueSubTypeId INT NULL;

ALTER TABLE ws.Warning ALTER COLUMN Status VARCHAR(100) NOT NULL;
ALTER TABLE ws.Warning ALTER COLUMN Type VARCHAR(100) NOT NULL;
ALTER TABLE ws.Warning ALTER COLUMN WarningSubtype VARCHAR(100) NULL;

-- Completed was an accidental intermediate value; Issued is the configured workflow state.
UPDATE ws.Warning SET Status = 'Issued' WHERE Status = 'Completed';

UPDATE warning
SET IssueStatusId = statusLookup.IssueStatusId
FROM ws.Warning warning
JOIN ws.LookupIssueStatuses statusLookup
  ON statusLookup.IssueStatusName = warning.Status
WHERE warning.IssueStatusId IS NULL;

UPDATE warning
SET IssueTypeId = typeLookup.IssueTypeId
FROM ws.Warning warning
JOIN ws.LookupIssueTypes typeLookup
  ON typeLookup.IssueTypeName = CASE WHEN warning.Status = 'Invalid' THEN 'Invalid' ELSE warning.Type END
WHERE warning.IssueTypeId IS NULL;

UPDATE warning
SET IssueSubTypeId = subTypeLookup.IssueSubTypeId
FROM ws.Warning warning
JOIN ws.LookupIssueSubTypes subTypeLookup
  ON subTypeLookup.IssueTypeId = warning.IssueTypeId
 AND subTypeLookup.IssueSubTypeName = warning.WarningSubtype
WHERE warning.IssueSubTypeId IS NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Warning_IssueStatus')
    ALTER TABLE ws.Warning ADD CONSTRAINT FK_Warning_IssueStatus FOREIGN KEY (IssueStatusId)
        REFERENCES ws.LookupIssueStatuses(IssueStatusId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Warning_IssueType')
    ALTER TABLE ws.Warning ADD CONSTRAINT FK_Warning_IssueType FOREIGN KEY (IssueTypeId)
        REFERENCES ws.LookupIssueTypes(IssueTypeId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Warning_IssueSubType')
    ALTER TABLE ws.Warning ADD CONSTRAINT FK_Warning_IssueSubType FOREIGN KEY (IssueSubTypeId)
        REFERENCES ws.LookupIssueSubTypes(IssueSubTypeId);
