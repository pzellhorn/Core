using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace pzellhorn.Core.Messaging.RabbitMq
{
    public static class RabbitMqExtensions
    { 
        public static IServiceCollection AddDistributedQueueRabbitMq(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "RabbitMq")
        {
            services.Configure<RabbitMqOptions>(configuration.GetSection(sectionName));

            services.AddSingleton<RabbitMqDistributedQueue>();
            services.AddSingleton<IQueuePublisher>(sp => sp.GetRequiredService<RabbitMqDistributedQueue>());
            services.AddSingleton<IQueueConsumer>(sp => sp.GetRequiredService<RabbitMqDistributedQueue>());
            services.AddSingleton<IQueueInspector>(sp => sp.GetRequiredService<RabbitMqDistributedQueue>());

            return services;
        }
    }
}
