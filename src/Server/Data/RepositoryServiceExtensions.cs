using SuperDeck.Core.Data.Repositories;
using SuperDeck.Server.Data.Repositories;
using SuperDeck.Server.Data.Repositories.MariaDB;

namespace SuperDeck.Server.Data;

public static class RepositoryServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"]?.ToLowerInvariant() ?? "sqlite";

        switch (provider)
        {
            case "mariadb":
            case "mysql":
                services.AddSingleton<ICharacterRepository, MariaDBCharacterRepository>();
                services.AddSingleton<IGhostRepository, MariaDBGhostRepository>();
                services.AddSingleton<IPlayerRepository, MariaDBPlayerRepository>();
                services.AddSingleton<IAIProfileRepository, MariaDBProfileRepository>();
                break;

            case "sqlite":
            default:
                services.AddSingleton<ICharacterRepository, SQLiteCharacterRepository>();
                services.AddSingleton<IGhostRepository, SQLiteGhostRepository>();
                services.AddSingleton<IPlayerRepository, SQLitePlayerRepository>();
                services.AddSingleton<IAIProfileRepository, SQLiteAIProfileRepository>();
                break;
        }

        return services;
    }
}
