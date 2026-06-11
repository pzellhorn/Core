# Implementing Core Repo into C# Project

1. Add a project reference to `pzellhorn.Core.State` in `PhotoProcessor.State.csproj`.

2. Add `Microsoft.EntityFrameworkCore.Design` and `Microsoft.EntityFrameworkCore.Tools` directly to `PhotoProcessor.State.csproj` (both with `PrivateAssets="all"`). 

3. Pin `Microsoft.EntityFrameworkCore` to `9.0.9` explicitly in `pzellhorn.Core.State.csproj`. 

4. Create `<YourProjectName>DbContext` inheriting from `BaseDbContext`.

5. Create `<YourProjectName>DbContextFactory` implementing `IDesignTimeDbContextFactory<<YourProjectName>DbContext>`. 

6. Add `appsettings.json` to `<YourProjectName>.State` containing the `"Local"` connection string. The factory reads this file at design time via `Directory.GetCurrentDirectory()`.

7. Add a project reference to `<YourProjectName>.State` in `<YourProjectName>.API.csproj`, and finally, register the context in your project's `Program.cs` with `AddDbContext`, `UseNpgsql`, and `UseSnakeCaseNamingConvention`. 
