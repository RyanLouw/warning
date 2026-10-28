using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0004)]
public class _0004_WarningDummyData : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        Execute.Script(Path.Combine("Migrations", "Scripts", "0004.sql"));
        Execute.Sql("EXEC ws.sp_EnsureDefaultCategoryQuestions @CreatedBy = 'seed';");
    }
}
