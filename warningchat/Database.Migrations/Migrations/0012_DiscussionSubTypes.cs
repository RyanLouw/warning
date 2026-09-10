using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0012)]
public class _0012_DiscussionSubTypes : Migration
{
    public override void Down()
    {
        // Lookup values may already be referenced, so retain them during rollback.
    }

    public override void Up()
    {
        Execute.Script("Migrations\\Scripts\\0012.sql");
    }
}
