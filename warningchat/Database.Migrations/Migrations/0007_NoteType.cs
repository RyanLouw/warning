using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0007)]
public class _0007_NoteType : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        Execute.Script(Path.Combine("Migrations", "Scripts", "0007.sql"));
    }
}
