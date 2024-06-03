using CNAS.Repository.Implementation;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using SpRepo.Abstraction;
using System.Diagnostics;

namespace CNAS.Repository.Extensions;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, string connectionString,
        bool logQueryOnConsole = false)
    {
        services.AddSingleton<IMongoClient>(_ =>
        {
            var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
            var settings = MongoClientSettings.FromUrl(mongoUrlBuilder.ToMongoUrl());

            if (logQueryOnConsole)
            {
                settings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(e => { Debug.WriteLine($"{e.CommandName} - {e.Command}"); });
                };
            }

            return new MongoClient(settings);
        });

        services.AddScoped<IClientSessionHandle>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();

            return client.StartSession();
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();

            var databaseName = MongoUrl.Create(connectionString).DatabaseName;

            return client.GetDatabase(databaseName);
        });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        return services;
    }
}