using PstnBot;
using Sample.IncidentBot.Bot;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BotBuilderExtensions
    {
        public static IServiceCollection AddBot(this IServiceCollection services)
            => services.AddBot(_ => { });

        public static IServiceCollection AddBot(this IServiceCollection services, Action<BotOptions> botOptionsAction)
        {
            var options = new BotOptions();
            botOptionsAction(options);
            services.AddSingleton(options);

            return services.AddSingleton<CallingBot>();
        }
    }
}
