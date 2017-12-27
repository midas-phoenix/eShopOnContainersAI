﻿using System;
using ArtificialIntelligence.API.Infrastructure.Filters;
using ArtificialIntelligence.API.Services.ComputerVision;
using ArtificialIntelligence.API.Services.ComputerVision.Client;
using ArtificialIntelligence.API.Services.ComputerVision.Models;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopOnContainers.BuildingBlocks.Resilience.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtificialIntelligence.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<ArtificialIntelligenceSettings>(Configuration);
            services.AddTransient<IComputerVisionServices, ComputerVisionServices>();
            services.AddTransient<IModelManagementClient, ModelManagementClient>();
            services.AddTransient<IModelManagementService, ModelManagementService>();

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
            })
            .AddControllersAsServices();

            // Add framework services.
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "eShopOnContainersAI - AI HTTP API",
                    Version = "v1",
                    Description = "The Artifical Intelligence Microservice HTTP API. This is a Data-Driven/CRUD microservice sample",
                    TermsOfService = "Terms Of Service"
                });

                options.OperationFilter<FormFileOperationFilter>();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //Configure logs

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddAzureWebAppDiagnostics();
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                loggerFactory.CreateLogger("init").LogDebug($"Using PATH BASE '{pathBase}'");
                app.UsePathBase(pathBase);
            }

            app.UseCors("CorsPolicy");

            app.UseMvcWithDefaultRoute();

            app.UseSwagger()
              .UseSwaggerUI(c =>
              {
                  c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "ArtificialIntelligence.API V1");
              });
        }
    }
}
