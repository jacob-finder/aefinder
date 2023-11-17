using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Grains;
using AElfIndexer.MongoDB;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AElfIndexer.Dapp;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerMongoDbModule))]
public class AElfIndexerDappBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureCors(context, configuration);
        ConfigureOrleans(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddGraphQL(b => b
            .AddAutoClrMappings()
            .AddSystemTextJson()
            .AddErrorInfoProvider(e => e.ExposeExceptionDetails = true));

        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                // .UseRedisClustering(opt =>
                // {
                //     opt.ConnectionString = configuration["Orleans:ClusterDbConnection"];
                //     opt.Database = Convert.ToInt32(configuration["Orleans:ClusterDbNumber"]);
                // })
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(AElfIndexerGrainsModule).Assembly).WithReferences())
                // .AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .AddKafka(AElfIndexerApplicationConsts.MessageStreamName)
                .WithOptions(options =>
                {
                    options.BrokerList = configuration.GetSection("Kafka:Brokers").Get<List<string>>();
                    options.ConsumerGroupId = "AElfIndexer";
                    options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                    options.AddTopic(AElfIndexerApplicationConsts.MessageStreamNamespace,new TopicCreationConfig { AutoCreate = true });
                    options.MessageMaxBytes = configuration.GetSection("Kafka:MessageMaxBytes").Get<int>();
                })
                .AddJson()
                .Build()
                .Build();
        });
    }
    
    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // AsyncHelper.RunSync(async ()=> await client.Connect());
        var app = context.GetApplicationBuilder();
        app.UseRouting();
        app.UseCors();
        app.UseConfiguredEndpoints();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
    
    
}