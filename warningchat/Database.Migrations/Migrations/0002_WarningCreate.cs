using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0002)]
public class _0002_WarningCreate : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0002.sql");
    }
}
