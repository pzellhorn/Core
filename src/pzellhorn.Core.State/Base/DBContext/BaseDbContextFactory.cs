using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace pzellhorn.Core.State.Base.DBContext
{
    public class BaseDbContextFactory : IDesignTimeDbContextFactory<BaseDbContext>
    {
        //Manages MessagingDbContext against Local connection string
        public BaseDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string conn =
                config.GetConnectionString("Local")
                ?? throw new ArgumentException("Local Db Connection string not found");

            DbContextOptions<BaseDbContext> options = new DbContextOptionsBuilder<BaseDbContext>()
                .UseNpgsql(conn)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new BaseDbContext(options);
        }

    }
}
