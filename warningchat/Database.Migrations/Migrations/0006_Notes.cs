using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0006)]
public class _0006_Notes : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0006.sql");
    }
}
