﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using Sales.Messages.Commands;

namespace store_web
{
    public class Startup
    {
	    private static readonly ILog Log = LogManager.GetLogger<Startup>();

		public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
			AddEndpont(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

	    private void AddEndpont(IServiceCollection services)
	    {
		    Console.WriteLine("Starting Endpoint");

		    var endpointConfiguration = new EndpointConfiguration("store-web");

			// setup general
		    endpointConfiguration.UsePersistence<LearningPersistence>();
			endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
		    endpointConfiguration.EnableInstallers();
		    endpointConfiguration.SendOnly();
			
		    // setup auditing
			endpointConfiguration.SendFailedMessagesTo("error");
		    endpointConfiguration.AuditProcessedMessagesTo("audit");

			// setup transport
			var connectionString = Environment.GetEnvironmentVariable("servicebus_connection_string");
			var transportExtensions = endpointConfiguration.UseTransport<RabbitMQTransport>();
		    transportExtensions.UseConventionalRoutingTopology();
		    transportExtensions.ConnectionString(connectionString);

			// setup message routing
		    var routing = transportExtensions.Routing();
			routing.RouteToEndpoint(typeof(PlaceOrder), "sales");

			// start the endpoint
			var endpointInstance = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
		    Log.Info("****************** Store website endpoint successfully started ******************");

			// register endpoint instance with the IoC framework
			services.AddSingleton<IMessageSession>(endpointInstance);
	    }
	}
}
