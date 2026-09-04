using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0008)]
public class _0008_IssueWorkflow : Migration
{
    public override void Down()
    {
        // No down migration. Drop and recreate the database for local resets.
    }

    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0008.sql");
    }
}
