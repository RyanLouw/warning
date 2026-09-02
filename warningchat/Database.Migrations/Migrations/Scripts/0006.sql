IF OBJECT_ID('ws.WarningNote', 'U') IS NULL
BEGIN
    CREATE TABLE ws.WarningNote
    (
        NoteId BIGINT IDENTITY(1,1) PRIMARY KEY,
        WarningId BIGINT NOT NULL,
        EvidenceId BIGINT NULL,
        NoteText VARCHAR(MAX) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_WarningNote_Warning
            FOREIGN KEY (WarningId) REFERENCES ws.Warning(WarningId),
        CONSTRAINT FK_WarningNote_Evidence
            FOREIGN KEY (EvidenceId) REFERENCES ws.WarningEvidence(EvidenceId)
    );
END
GO