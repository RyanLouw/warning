using FluentMigrator;

namespace Database.Migrations;

/// <summary>
/// Repairs databases where migrations 2-8 were recorded in VersionInfo but
/// their scripts were skipped because the unrelated dbo.Benchmark table did
/// not exist. The scripts are idempotent and can safely rebuild the missing
/// Warning System schema in dependency order.
/// </summary>
[Tags(TagNames.Pitstop)]
[Migration(0009)]
public class _0009_RepairSkippedWarningSchema : Migration
{
    public override void Down()
    {
        // No down migration. Drop and recreate the database for local resets.
    }

    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0002.sql");
        Execute.Script(@"Migrations\Scripts\0003.sql");
        Execute.Script(@"Migrations\Scripts\0004.sql");
        Execute.Sql("EXEC ws.sp_EnsureDefaultCategoryQuestions @CreatedBy = 'seed';");
        Execute.Script(@"Migrations\Scripts\0005.sql");
        Execute.Script(@"Migrations\Scripts\0006.sql");
        Execute.Script(@"Migrations\Scripts\0007.sql");
        Execute.Script(@"Migrations\Scripts\0008.sql");
    }
}
