// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EarthML.Templates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;

namespace Mvc.RenderViewToString
{
    public class Program
    {
        public static void Main()
        {
            var serviceScopeFactory = InitializeServices();
            {
                var emailContent = RenderViewAsync(serviceScopeFactory, "Templates/EmailTemplate.cshtml").Result;

                Console.WriteLine(emailContent);

            }

            
            {
                var emailContent = RenderViewAsync(serviceScopeFactory, "Templates/WelcomeMail.cshtml").Result;

                Console.WriteLine(emailContent);

            }

            {

               
                var a = new EmailTemplateService();
                Console.WriteLine(a.RenderViewAsync("Templates/Layouts/SaltedResponsiveEmailTemplate.cshtml").Result);
            }


            var host = new WebHostBuilder()
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .ConfigureServices((builder, services) =>
              {

              }).ConfigureAppConfiguration(builder =>
              {

              }).Configure(builder =>
              {
                  builder.Use(async (ctx,next)=>
                  {
                      var a = new EmailTemplateService();

                      await ctx.Response.WriteAsync(await a.RenderViewAsync($"Templates/{ctx.Request.Path}.cshtml"));



                  });
              })
              .Build();

            host.Run();
        }

        public static IServiceScopeFactory InitializeServices(string customApplicationBasePath = null)
        {
            // Initialize the necessary services
            var services = new ServiceCollection();
            ConfigureDefaultServices(services, customApplicationBasePath);

            // Add a custom service that is used in the view.
            services.AddSingleton<EmailReportGenerator>();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        public static Task<string> RenderViewAsync(IServiceScopeFactory scopeFactory, string view)
        {
            using (var serviceScope = scopeFactory.CreateScope())
            {
                var helper = serviceScope.ServiceProvider.GetRequiredService<RazorViewToStringRenderer>();

                var model = new EmailViewModel
                {
                    UserName = "User",
                    SenderName = "Sender",
                    UserData1 = 1,
                    UserData2 = 2
                };

                return helper.RenderViewToStringAsync(view, model);
            }
        }

        private static void ConfigureDefaultServices(IServiceCollection services, string customApplicationBasePath)
        {
            string applicationName;
            IFileProvider fileProvider;
            if (!string.IsNullOrEmpty(customApplicationBasePath))
            {
                applicationName = Path.GetFileName(customApplicationBasePath);
                fileProvider = new PhysicalFileProvider(customApplicationBasePath);
            }
            else
            {
                applicationName = Assembly.GetEntryAssembly().GetName().Name;
                fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            }

            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment
            {
                ApplicationName =  applicationName,
                WebRootFileProvider = fileProvider,
            });
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(fileProvider);
            });
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddLogging();
            services.AddMvc().AddRazorPagesOptions(o => { o.RootDirectory = "/templates"; });
            services.AddTransient<RazorViewToStringRenderer>();
        }
    }
}
