using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0008)]
public class _0008_IssueWorkflow : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        Execute.Script(Path.Combine("Migrations", "Scripts", "0008.sql"));
    }
}
