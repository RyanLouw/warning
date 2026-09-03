/*
    Add state flags expected by the Warning entity. Earlier databases could be
    created without these columns because they were never included in the
    Warning System migrations.
*/
IF OBJECT_ID('ws.Warning', 'U') IS NULL
    THROW 50010, 'The ws.Warning table must exist before adding state flags.', 1;

IF COL_LENGTH('ws.Warning', 'Completed') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning
        ADD Completed BIT NOT NULL
            CONSTRAINT DF_Warning_Completed DEFAULT ((0)) WITH VALUES;
    ');
END;

IF COL_LENGTH('ws.Warning', 'IsDeleted') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning
        ADD IsDeleted BIT NOT NULL
            CONSTRAINT DF_Warning_IsDeleted DEFAULT ((0)) WITH VALUES;
    ');
END;

IF COL_LENGTH('ws.Warning', 'HideFromTeamLead') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning
        ADD HideFromTeamLead BIT NOT NULL
            CONSTRAINT DF_Warning_HideFromTeamLead DEFAULT ((0)) WITH VALUES;
    ');
END;
