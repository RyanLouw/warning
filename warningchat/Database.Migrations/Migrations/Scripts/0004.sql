
/* =========================
   SEED: TransgressionCategory
   ========================= */
IF OBJECT_ID('ws.TransgressionCategory','U') IS NOT NULL
BEGIN
    MERGE ws.TransgressionCategory AS T
    USING (VALUES
    ('Tardiness'),
    ('Arriving late or leaving early without permission'),
    ('Negligence / Non-compliance with SOP'),
    ('Failing to notify your relevant Manager when booked off sick'),
    ('Unprofessional behaviour'),
    ('Not following the call script'),
    ('Refusal to carry out legitimate/ direct instructions'),
    ('Use of cell phones during working hours'),
    ('Failure to meet the call quality standards for three consecutive months'),
    ('Absent from work without permission'),
    ('Failure to ask follow-up consent (when AE is mentioned)'),
    ('Failure to ask follow-up questions (when AE is mentioned)'),
    ('Failure to correctly log that follow up consent was received (when AE is mentioned)'),
    ('Loss of, or damage to property of Health Window /clients'),
    ('Faking a disease'),
    ('Sleeping on duty'),
    ('Unruly/disruptive behaviour'),
    ('Unauthorized use of property belonging to Health Window or any of its clients for private/personal use or gain'),
    ('Providing inaccurate/ false information / dishonesty / misrepresentation'),
    ('Creating disharmony'),
    ('Failure to confirm DOB'),
    ('Spreading gossip / false rumours'),
    ('Theft / Fraud'),
    ('Assault or violence'),
    ('Misuse of drugs or alcohol or being under the influence of either whilst on duty'),
    ('Any form of intimidation/ discrimination/ harassment'),
    ('Breach of confidentiality'),
    ('Missed AE/PTC'),
    ('Placing an order without making a call or without the necessary confirmation'),
    ('Sharing personal login details/ allowing another employee to use your 3CX extension or System without permission from your line Manager'),
    ('Other/Unknown')
    ) AS S([Name])
    ON T.[Name] = S.[Name]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Name], IsActive, CreatedOn, CreatedBy)
        VALUES (S.[Name], 1, SYSUTCDATETIME(), 'seed');
END



/* =========================
   SEED: Question
   ========================= */
IF OBJECT_ID('ws.Question','U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT ws.Question ON;

    MERGE ws.Question AS T
    USING (VALUES
    (1, 'When Did the transgression happen', 'Date', NULL, 1, CAST('2026-02-10 10:32:47' AS datetime2(0)), 'seed', NULL, NULL),
    (2, 'Please describe the transgression', 'Text', NULL, 1, CAST('2026-02-10 10:50:42' AS datetime2(0)), 'seed', CAST('2026-02-10 13:29:05' AS datetime2(0)), 'seed'),
    (3, 'If the employee failed to comply with an SOP, please select the SOP name (if relevant).', 'Text', NULL, 1, CAST('2026-02-10 10:53:53' AS datetime2(0)), 'seed', CAST('2026-02-11 08:20:53' AS datetime2(0)), 'seed')
    ) AS S
    (
        QuestionId, QuestionText, ControlType, DefaultConfigJson, IsActive,
        CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
    )
    ON T.QuestionId = S.QuestionId
    WHEN MATCHED THEN
        UPDATE SET
            QuestionText      = S.QuestionText,
            ControlType       = S.ControlType,
            DefaultConfigJson = S.DefaultConfigJson,
            IsActive          = S.IsActive,
            UpdatedOn         = S.UpdatedOn,
            UpdatedBy         = S.UpdatedBy
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            QuestionId, QuestionText, ControlType, DefaultConfigJson, IsActive,
            CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
        )
        VALUES
        (
            S.QuestionId, S.QuestionText, S.ControlType, S.DefaultConfigJson, S.IsActive,
            S.CreatedOn, S.CreatedBy, S.UpdatedOn, S.UpdatedBy
        );

    SET IDENTITY_INSERT ws.Question OFF;
END

