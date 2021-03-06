// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;

namespace BenchmarkServer
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly bool useAzureSignalR = true;
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
            useAzureSignalR = configuration.GetSection("Azure:SignalR:ConnectionString").Exists();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var signalrBuilder = services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
            });
            if (useAzureSignalR)
                signalrBuilder.AddAzureSignalR();

            // TODO: Json vs NewtonsoftJson option
            signalrBuilder.AddMessagePackProtocol();

            var redisConnectionString = _config["SignalRRedis"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                signalrBuilder.AddStackExchangeRedis(redisConnectionString);
            }

            services.AddSingleton<ConnectionCounter>();

            services.AddHostedService<HostedCounterService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (useAzureSignalR)
            {
                app.UseAzureSignalR(routes =>
                {
                    routes.MapHub<EchoHub>("/echo");
                });
            } else
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
}
