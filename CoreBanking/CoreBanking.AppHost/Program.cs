var builder = DistributedApplication.CreateBuilder(args);

// Initialize database server
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("latest")
    .WithVolume("corebanking-db", "/var/lib/postgresql/data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(rbuilder =>
    {
        rbuilder.WithImageTag("latest"); }
    );

// Get database in server and add migration service
var corebankingDb = postgres.AddDatabase("corebanking-db", "corebanking");
var migrationService = builder.AddProject<Projects.CoreBanking_MigrationService>("corebanking-migrationservice");

// Initialize API
builder.AddProject<Projects.CoreBanking_API>("corebanking-api")
    .WithReference(corebankingDb)
    .WaitFor(postgres)
    .WaitForCompletion(migrationService);

builder.Build().Run();
