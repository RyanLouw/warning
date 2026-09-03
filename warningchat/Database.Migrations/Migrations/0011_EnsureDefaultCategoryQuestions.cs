using FluentMigrator;

namespace Database.Migrations;

/// <summary>
/// Re-deploys the default category-question procedure for databases where the
/// original migration was already recorded without creating the procedure,
/// then backfills the first three questions for every active category.
/// </summary>
[Tags(TagNames.Pitstop)]
[Migration(0011)]
public class _0011_EnsureDefaultCategoryQuestions : Migration
{
    public override void Down()
    {
        // Keep the shared procedure and its links during rollback.
    }

    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0011.sql");
        Execute.Sql("EXEC ws.sp_EnsureDefaultCategoryQuestions @CreatedBy = 'migration-0011';");
    }
}
