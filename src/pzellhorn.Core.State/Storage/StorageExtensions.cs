using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyInjection;
using pzellhorn.Core.State.Storage.Blob;

namespace pzellhorn.Core.State.Storage
{
    public static class StorageExtensions
    {
        public static IServiceCollection AddStorageExtensions(this IServiceCollection services)
        {
            services.AddSingleton<StorageClient>(_ => StorageClient.Create());  //Google Cloud Storage client
            
            services.AddSingleton<IStorageManager, GoogleBlobStorage>();
            return services;
        }
    }
}
