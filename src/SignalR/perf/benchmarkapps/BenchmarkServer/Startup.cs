// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkServer.Hubs;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenchmarkServer
{
    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var signalrBuilder = services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
            })
            // TODO: Json vs NewtonsoftJson option
            .AddMessagePackProtocol();

            var redisConnectionString = _config["SignalRRedis"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                signalrBuilder.AddStackExchangeRedis(redisConnectionString);
            }

            TelemetryConfiguration.Active.InstrumentationKey = _config["APPLICATIONINSIGHTS_INSTRUMENTATIONKEY"];

            services.AddSingleton<TelemetryClient>();

            services.AddSingleton<HostMetrics>();
            services.AddSingleton<ConnectionCounter>();

            services.AddHostedService<ConnectionCounterService>();
            services.AddHostedService<HostMetricsService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<EchoHub>("/echo", o =>
                {
                    // Remove backpressure for benchmarking
                    o.TransportMaxBufferSize = 0;
                    o.ApplicationMaxBufferSize = 0;
                });
            });
        }
    }
}
