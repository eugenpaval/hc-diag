using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace hc_diag
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // If you need dependency injection with your query object add your query type as a services.
            // services.AddSingleton<Query>();
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddDiagnosticEventListener<GraphQlEventObserver>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // By default the GraphQL server is mapped to /graphql
                // This route also provides you with our GraphQL IDE. In order to configure the
                // the GraphQL IDE use endpoints.MapGraphQL().WithToolOptions(...).
                endpoints.MapGraphQL();
            });
        }
    }

    public class GraphQlEventObserver : DiagnosticEventListener
    {
        private readonly Stopwatch _stopWatch = new();

        public override bool EnableResolveFieldValue => false;

        public override IActivityScope ExecuteRequest(IRequestContext context)
        {
            Log.Logger.Information($"New query {context.Request.QueryId}");
            Log.Logger.Debug($"{context.Request.QueryId}: {context.Request.Query?.ToString()}");

            _stopWatch.Start();
            return new QuestApiActivityScope(_stopWatch, context.Request.QueryId);
        }
    }

    public sealed class QuestApiActivityScope : IActivityScope
    {
        private bool _disposed;
        private readonly Stopwatch _stopWatch;
        private readonly string? _queryId;

        public QuestApiActivityScope(Stopwatch stopWatch, string? queryId)
        {
            _stopWatch = stopWatch;
            _queryId = queryId;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopWatch.Stop();
                Log.Logger.Information($"Query {_queryId} executed in {_stopWatch.ElapsedMilliseconds}ms");
                _stopWatch.Reset();

                _disposed = true;
            }
        }
    }

    public class Query
    {
        public string Test => "Luke SkyWalker";
    }
}
