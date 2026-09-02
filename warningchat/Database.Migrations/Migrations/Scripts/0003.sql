

CREATE OR ALTER PROCEDURE ws.sp_EnsureDefaultCategoryQuestions
    @CreatedBy VARCHAR(100) = 'system',
    @SetIsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('ws.TransgressionCategory', 'U') IS NULL RETURN;
    IF OBJECT_ID('ws.CategoryQuestion', 'U') IS NULL RETURN;

    ;WITH RequiredQuestions AS
    (
        SELECT 1 AS QuestionId, CAST(1 AS bit) AS IsRequired, 1 AS SortOrder
        UNION ALL SELECT 2, CAST(1 AS bit), 2
        UNION ALL SELECT 3, CAST(0 AS bit), 3
    ),
    CategoryList AS
    (
        SELECT CategoryId
        FROM ws.TransgressionCategory
        WHERE IsActive = 1
    ),
    Desired AS
    (
        SELECT
            c.CategoryId,
            rq.QuestionId,
            rq.IsRequired,
            rq.SortOrder
        FROM CategoryList c
        CROSS JOIN RequiredQuestions rq
    )
    MERGE ws.CategoryQuestion AS T
    USING Desired AS S
        ON T.CategoryId = S.CategoryId
       AND T.QuestionId = S.QuestionId

    WHEN MATCHED THEN
        UPDATE SET
            T.IsRequired = S.IsRequired,
            T.SortOrder  = S.SortOrder,
            T.IsActive   = @SetIsActive,
            T.UpdatedOn  = SYSUTCDATETIME(),
            T.UpdatedBy  = @CreatedBy

    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            CategoryId, QuestionId, IsRequired, SortOrder,
            ConfigJson, IsActive, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
        )
        VALUES
        (
            S.CategoryId, S.QuestionId, S.IsRequired, S.SortOrder,
            NULL, @SetIsActive, SYSUTCDATETIME(), @CreatedBy, NULL, NULL
        );

END
