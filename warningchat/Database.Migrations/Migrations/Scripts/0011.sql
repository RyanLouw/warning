CREATE OR ALTER PROCEDURE ws.sp_EnsureDefaultCategoryQuestions
    @CreatedBy VARCHAR(100) = 'system',
    @SetIsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('ws.TransgressionCategory', 'U') IS NULL RETURN;
    IF OBJECT_ID('ws.CategoryQuestion', 'U') IS NULL RETURN;
    IF OBJECT_ID('ws.Question', 'U') IS NULL RETURN;

    ;WITH RequiredQuestions AS
    (
        SELECT QuestionId, IsRequired, SortOrder
        FROM
        (
            VALUES
                (1, CAST(1 AS bit), 1),
                (2, CAST(1 AS bit), 2),
                (3, CAST(0 AS bit), 3)
        ) defaults (QuestionId, IsRequired, SortOrder)
        WHERE EXISTS
        (
            SELECT 1
            FROM ws.Question question
            WHERE question.QuestionId = defaults.QuestionId
        )
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
            category.CategoryId,
            question.QuestionId,
            question.IsRequired,
            question.SortOrder
        FROM CategoryList category
        CROSS JOIN RequiredQuestions question
    )
    MERGE ws.CategoryQuestion AS target
    USING Desired AS source
        ON target.CategoryId = source.CategoryId
       AND target.QuestionId = source.QuestionId
    WHEN MATCHED THEN
        UPDATE SET
            target.IsRequired = source.IsRequired,
            target.SortOrder = source.SortOrder,
            target.IsActive = @SetIsActive,
            target.UpdatedOn = SYSUTCDATETIME(),
            target.UpdatedBy = @CreatedBy
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            CategoryId, QuestionId, IsRequired, SortOrder,
            ConfigJson, IsActive, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
        )
        VALUES
        (
            source.CategoryId, source.QuestionId, source.IsRequired,
            source.SortOrder, NULL, @SetIsActive, SYSUTCDATETIME(),
            @CreatedBy, NULL, NULL
        );
END
