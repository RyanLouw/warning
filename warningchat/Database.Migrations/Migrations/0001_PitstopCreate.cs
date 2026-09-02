using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.Pitstop)]
[Migration(0001)]
public class _0001_Schema01 : Migration
{
    public override void Down()
    {
        //No down. Drop db
    }

    public override void Up()
    {
        if (Schema.Table("Benchmark").Exists())
        {
            //Execute.Script(@"Migrations\Scripts\0001.sql");
        }
    }
}
