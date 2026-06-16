using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyInjection;
using pzellhorn.Core.State.Storage.Blob;
using pzellhorn.Core.State.Storage.Disk;

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
    }
}
