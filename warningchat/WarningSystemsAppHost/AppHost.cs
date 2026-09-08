var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("WarningSystems-sql", port: 2433)
                  .WithLifetime(ContainerLifetime.Persistent)
                  .WithDataVolume("WarningSystems-sql")
                  .AddDatabase("PitstopDb", "PitStop");

var migrations = builder.AddProject<Projects.Database_Migrations>("database-migrations")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Staging")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.WarningSystems>("WarningSystems").WithExplicitStart();//.WithReference(sql);

await builder.Build().RunAsync();
