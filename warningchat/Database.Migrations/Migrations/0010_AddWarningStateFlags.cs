using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0010)]
public class _0010_AddWarningStateFlags : Migration
{
    public override void Down()
    {
        // Retain state flags to avoid data loss during rollback.
    }

    public override void Up()
    {
        Execute.Script("Migrations\\Scripts\\0010.sql");
    }
}
