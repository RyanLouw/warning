IF OBJECT_ID('ws.NoteTypeLookup', 'U') IS NULL
BEGIN
    CREATE TABLE ws.NoteTypeLookup
    (
        NoteTypeId INT IDENTITY(1,1) NOT NULL,
        Name VARCHAR(100) NOT NULL,

        CreatedBy VARCHAR(100) NOT NULL
            CONSTRAINT DF_NoteTypeLookup_CreatedBy DEFAULT ('system'),

        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_NoteTypeLookup_CreatedOn DEFAULT (SYSUTCDATETIME()),

        IsActive BIT NOT NULL
            CONSTRAINT DF_NoteTypeLookup_IsActive DEFAULT ((1)),

        IsSystemOnly BIT NOT NULL
            CONSTRAINT DF_NoteTypeLookup_IsSystemOnly DEFAULT ((0)),

        CONSTRAINT PK_NoteTypeLookup 
            PRIMARY KEY CLUSTERED (NoteTypeId),

        CONSTRAINT UQ_NoteTypeLookup_Name 
            UNIQUE (Name)
    );
END
GO

IF COL_LENGTH('ws.NoteTypeLookup', 'IsSystemOnly') IS NULL
BEGIN
    ALTER TABLE ws.NoteTypeLookup
    ADD IsSystemOnly BIT NOT NULL
        CONSTRAINT DF_NoteTypeLookup_IsSystemOnly DEFAULT ((0));
END
GO

IF COL_LENGTH('ws.WarningNote', 'NoteTypeId') IS NULL
BEGIN
    ALTER TABLE ws.WarningNote
    ADD NoteTypeId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_WarningNote_NoteTypeLookup'
)
BEGIN
    ALTER TABLE ws.WarningNote
    ADD CONSTRAINT FK_WarningNote_NoteTypeLookup
        FOREIGN KEY (NoteTypeId)
        REFERENCES ws.NoteTypeLookup(NoteTypeId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_WarningNote_NoteTypeId'
      AND object_id = OBJECT_ID('ws.WarningNote')
)
BEGIN
    CREATE INDEX IX_WarningNote_NoteTypeId
    ON ws.WarningNote(NoteTypeId);
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'Submitted to Legal')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'Submitted to Legal',
        'system',
        SYSUTCDATETIME(),
        1,
        1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'Submitted to Team Leader')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'Submitted to Team Leader',
        'system',
        SYSUTCDATETIME(),
        1,
        1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'Investigation')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'Investigation',
        'system',
        SYSUTCDATETIME(),
        1,
        0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'Need More Info')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'Need More Info',
        'system',
        SYSUTCDATETIME(),
        1,
        0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'In Review')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'In Review',
        'system',
        SYSUTCDATETIME(),
        1,
        0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM ws.NoteTypeLookup WHERE Name = 'Issued to Employee')
BEGIN
    INSERT INTO ws.NoteTypeLookup
    (
        Name,
        CreatedBy,
        CreatedOn,
        IsActive,
        IsSystemOnly
    )
    VALUES
    (
        'Issued to Employee',
        'system',
        SYSUTCDATETIME(),
        1,
        0
    );
END
GO