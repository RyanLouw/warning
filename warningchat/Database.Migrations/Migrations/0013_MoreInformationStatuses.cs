using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0013)]
public class _0013_MoreInformationStatuses : Migration
{
    public override void Down()
    {
        // Statuses may be referenced by existing warnings, so retain them on rollback.
    }

    public override void Up()
    {
        Execute.Script("Migrations\\Scripts\\0013.sql");
    }
}