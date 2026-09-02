using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0003)]
public class _0003_StoreProc : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        if (!Schema.Schema("ws").Exists())
        {
            Create.Schema("ws");
        }
        if (Schema.Table("Benchmark").Exists())
            Execute.Script(@"Migrations\Scripts\0003.sql");
    }
}