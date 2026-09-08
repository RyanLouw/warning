using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0011)]
public class _0011_IssueLookups : Migration
{
    public override void Down()
    {
        // Lookup data and migrated relationships are intentionally retained.
    }

    public override void Up()
    {
        Execute.Script("Migrations\\Scripts\\0011.sql");
    }
}
