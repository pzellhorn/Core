using Microsoft.Extensions.DependencyInjection;
using pzellhorn.Core.ClientAPI.Base;

namespace pzellhorn.Core.ClientAPI.ServiceExtensions
{
    public static class ClientApiBaseExtensions
    {
        public static IServiceCollection AddClientApiBaseExtensions(this IServiceCollection services, Uri baseAddress) 
        {
            services.AddHttpClient<ApiTransport>(x => x.BaseAddress = baseAddress);

            return services;
        }
    }
}