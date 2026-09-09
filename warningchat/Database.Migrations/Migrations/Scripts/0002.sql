

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ws')
    EXEC('CREATE SCHEMA ws');

/* =========================
   ADMIN AUDIT LOG
   ========================= */
IF OBJECT_ID('ws.AdminAuditLog','U') IS NULL
BEGIN
    CREATE TABLE ws.AdminAuditLog
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntityName VARCHAR(100) NOT NULL,
        EntityId   VARCHAR(50)  NOT NULL,
        Action     VARCHAR(20)  NOT NULL,
        ChangedOn  DATETIME2(0) NOT NULL,
        ChangedBy  VARCHAR(100) NOT NULL,
        Summary    VARCHAR(400) NULL,
        OldValues  VARCHAR(MAX) NULL,
        NewValues  VARCHAR(MAX) NULL
    );
END


IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc
               JOIN sys.columns c ON c.default_object_id = dc.object_id
               WHERE c.object_id = OBJECT_ID('ws.AdminAuditLog') AND c.name='ChangedOn')
BEGIN
    ALTER TABLE ws.AdminAuditLog
    ADD CONSTRAINT DF_AdminAuditLog_ChangedOn DEFAULT (SYSUTCDATETIME()) FOR ChangedOn;
END


/* =========================
   TRANSGRESSION CATEGORY
   ========================= */
IF OBJECT_ID('ws.TransgressionCategory','U') IS NULL
BEGIN
    CREATE TABLE ws.TransgressionCategory
    (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        [Name] VARCHAR(MAX) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        UpdatedOn DATETIME2(0) NULL,
        UpdatedBy VARCHAR(100) NULL
    );
END


IF COL_LENGTH('ws.TransgressionCategory','IsActive') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc
               JOIN sys.columns c ON c.default_object_id=dc.object_id
               WHERE c.object_id=OBJECT_ID('ws.TransgressionCategory') AND c.name='IsActive')
    ALTER TABLE ws.TransgressionCategory ADD CONSTRAINT DF_TC_IsActive DEFAULT ((1)) FOR IsActive;


/* =========================
   QUESTION
   ========================= */
IF OBJECT_ID('ws.Question','U') IS NULL
BEGIN
    CREATE TABLE ws.Question
    (
        QuestionId INT IDENTITY(1,1) PRIMARY KEY,
        QuestionText VARCHAR(400) NOT NULL,
        ControlType VARCHAR(50) NOT NULL,
        DefaultConfigJson VARCHAR(MAX) NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        UpdatedOn DATETIME2(0) NULL,
        UpdatedBy VARCHAR(100) NULL
    );
END


IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name='UQ_Question' AND parent_object_id=OBJECT_ID('ws.Question')
)
BEGIN
    ALTER TABLE ws.Question
    ADD CONSTRAINT UQ_Question UNIQUE (QuestionText, ControlType);
END


/* =========================
   CATEGORY QUESTION
   ========================= */
IF OBJECT_ID('ws.CategoryQuestion','U') IS NULL
BEGIN
    CREATE TABLE ws.CategoryQuestion
    (
        CategoryId INT NOT NULL,
        QuestionId INT NOT NULL,
        IsRequired BIT NOT NULL,
        SortOrder INT NOT NULL,
        ConfigJson VARCHAR(MAX) NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        UpdatedOn DATETIME2(0) NULL,
        UpdatedBy VARCHAR(100) NULL,
        CONSTRAINT PK_CategoryQuestion PRIMARY KEY (CategoryId, QuestionId)
    );
END


/* FKs */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_CategoryQuestion_Category')
    ALTER TABLE ws.CategoryQuestion
    ADD CONSTRAINT FK_CategoryQuestion_Category
    FOREIGN KEY (CategoryId) REFERENCES ws.TransgressionCategory(CategoryId);


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_CategoryQuestion_Question')
    ALTER TABLE ws.CategoryQuestion
    ADD CONSTRAINT FK_CategoryQuestion_Question
    FOREIGN KEY (QuestionId) REFERENCES ws.Question(QuestionId);


/* =========================
   WARNING
   ========================= */
IF OBJECT_ID('ws.Warning','U') IS NULL
BEGIN
    CREATE TABLE ws.Warning
    (
        WarningId BIGINT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId VARCHAR(50) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL,
        Status VARCHAR(20) NOT NULL,
         [Type] VARCHAR(20) NOT NULL CONSTRAINT DF_Warning_Type DEFAULT ('Issue'),
        WarningSubtype VARCHAR(30) NULL,
        CategoryId INT NOT NULL,
        SubmittedOn DATETIME2(0) NULL,
        LegalExpiryDate DATE NULL,
        LastStatusChangedOn DATETIME2(0) NULL,
        LastStatusChangedBy VARCHAR(100) NULL,
        Completed BIT NOT NULL CONSTRAINT DF_Warning_Completed DEFAULT ((0)),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Warning_IsDeleted DEFAULT ((0)),
        HideFromTeamLead BIT NOT NULL CONSTRAINT DF_Warning_HideFromTeamLead DEFAULT ((0))
    );
END


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Warning_Category')
    ALTER TABLE ws.Warning
    ADD CONSTRAINT FK_Warning_Category
    FOREIGN KEY (CategoryId) REFERENCES ws.TransgressionCategory(CategoryId);


/* =========================
   WARNING ANSWER
   ========================= */
IF OBJECT_ID('ws.WarningAnswer','U') IS NULL
BEGIN
    CREATE TABLE ws.WarningAnswer
    (
        AnswerId BIGINT IDENTITY(1,1) PRIMARY KEY,
        WarningId BIGINT NOT NULL,
        QuestionId INT NOT NULL,
        AnswerText VARCHAR(MAX) NULL,
        AnswerJson VARCHAR(MAX) NULL,
        CreatedOn DATETIME2(0) NOT NULL
    );
END


IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name='UQ_WarningAnswer'
)
    ALTER TABLE ws.WarningAnswer
    ADD CONSTRAINT UQ_WarningAnswer UNIQUE (WarningId, QuestionId);


/* =========================
   WARNING EVIDENCE
   ========================= */
IF OBJECT_ID('ws.WarningEvidence','U') IS NULL
BEGIN
    CREATE TABLE ws.WarningEvidence
    (
        EvidenceId BIGINT IDENTITY(1,1) PRIMARY KEY,
        WarningId BIGINT NOT NULL,
        FileName VARCHAR(260) NOT NULL,
        FileType VARCHAR(20) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        StorageUrl VARCHAR(2048) NOT NULL,
        UploadedBy VARCHAR(100) NOT NULL,
        UploadedOn DATETIME2(0) NOT NULL
    );
END


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_WarningEvidence_Warning')
    ALTER TABLE ws.WarningEvidence
    ADD CONSTRAINT FK_WarningEvidence_Warning
    FOREIGN KEY (WarningId) REFERENCES ws.Warning(WarningId);
