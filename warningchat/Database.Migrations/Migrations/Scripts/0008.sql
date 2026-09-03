/* Store the outcome separately from the workflow status. */
IF COL_LENGTH('ws.Warning', 'Type') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning
        ADD [Type] VARCHAR(20) NOT NULL
            CONSTRAINT DF_Warning_Type DEFAULT (''Issue'');
    ');
END;

IF COL_LENGTH('ws.Warning', 'WarningSubtype') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning
        ADD WarningSubtype VARCHAR(30) NULL;
    ');
END;

/* Convert outcomes previously stored in Status into the new model. */
EXEC(N'
    UPDATE ws.Warning
    SET [Type] = CASE
            WHEN [Status] IN (''Warning'', ''Discussion'', ''Hearing'') THEN [Status]
            ELSE ''Issue''
        END,
        [Status] = CASE
            WHEN [Status] IN (''Warning'', ''Discussion'', ''Hearing'') THEN ''Pending''
            WHEN [Status] = ''Complete'' THEN ''Completed''
            ELSE [Status]
        END
    WHERE [Status] IN (''Warning'', ''Discussion'', ''Hearing'', ''Complete'');
');

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Warning_Type'
      AND parent_object_id = OBJECT_ID('ws.Warning'))
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning ADD CONSTRAINT CK_Warning_Type
            CHECK ([Type] IN (''Issue'', ''Warning'', ''Discussion'', ''Hearing''));
    ');
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Warning_WarningSubtype'
      AND parent_object_id = OBJECT_ID('ws.Warning'))
BEGIN
    EXEC(N'
        ALTER TABLE ws.Warning ADD CONSTRAINT CK_Warning_WarningSubtype
            CHECK (
                ([Type] = ''Warning'' AND (WarningSubtype IS NULL OR WarningSubtype IN
                    (''Verbal warning'', ''1st written warning'', ''2nd written warning'', ''Final warning'')))
                OR ([Type] <> ''Warning'' AND WarningSubtype IS NULL)
            );
    ');
END;
