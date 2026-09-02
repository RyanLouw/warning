using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0005)]
public class _0005_Categorys : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        if (Schema.Table("Benchmark").Exists())
            Execute.Script(@"Migrations\Scripts\0005.sql");
    }
}