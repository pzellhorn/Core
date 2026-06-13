# Running EFCore migrations to your PostgresDB in Docker pod

# Migration script, run in project root:
dotnet ef migrations add <Migration Name> --project src/<YourProjectName>.State --startup-project src/<YourProjectName> && dotnet ef database update --project src/<YourProjectName>.State --startup-project src/<YourProjectName>
