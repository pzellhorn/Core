using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pzellhorn.Core.State.Storage.Blob;
using pzellhorn.Core.State.Storage.Disk;
using pzellhorn.Core.State.Storage.S3;

namespace pzellhorn.Core.State.Storage
{
    public static class StorageExtensions
    {
        public static IServiceCollection AddGoogleBlobStorage(this IServiceCollection services)
        {
            services.AddSingleton<StorageClient>(_ => StorageClient.Create());  //Google Cloud Storage client

            services.AddSingleton<IStorageManager, GoogleBlobStorage>();
            return services;
        }

        public static IServiceCollection AddDiskStorage(this IServiceCollection services, string baseStoragePath)
        {
            services.AddSingleton<IStorageManager>(_ => new DiskStorage(baseStoragePath));
            return services;
        }

        /// <summary>
        /// Used by MiniO (our local storage manager), but can do basics for all S3 compatible storage providers
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static IServiceCollection AddS3Storage(this IServiceCollection services, IConfiguration configuration, string sectionName = "S3")
        {
            services.Configure<S3Options>(configuration.GetSection(sectionName));

            services.AddSingleton<S3StorageManager>();
            services.AddSingleton<IStorageManager>(sp => sp.GetRequiredService<S3StorageManager>());
            services.AddSingleton<ISignedUrlProvider>(sp => sp.GetRequiredService<S3StorageManager>());
            services.AddSingleton<IMultipartStorage>(sp => sp.GetRequiredService<S3StorageManager>());
            return services;
        }
    }
}
