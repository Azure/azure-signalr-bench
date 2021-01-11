using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portal.Controllers;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Portal.Cron;

namespace Portal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            // AAD auth

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireRole(PerfConstants.Roles.Contributor)
                    .Build();
            });
            services
                .AddRazorPages()
                .AddMicrosoftIdentityUI();

            services.AddSingleton(
                sp => new SecretClient(
                    new Uri(Configuration[PerfConstants.ConfigurationKeys.KeyVaultUrlKey]), new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions()
                        {
                            ManagedIdentityClientId = Configuration[PerfConstants.ConfigurationKeys.MsiAppId]
                        })
                ));
            services.AddSingleton<IPerfStorage>(sp =>
                {
                    var secretClient = sp.GetService<SecretClient>();
                    try
                    {
                        var connectionString = secretClient.GetSecretAsync("sa-accessKey").GetAwaiter().GetResult()
                            .Value.Value;
                        return new PerfStorage(connectionString);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Connection error:{e}");
                    }

                    return null;
                }
            );
            services.AddSingleton<ICronScheduler,CronScheduler>();
            services.AddSingleton<ClusterState>();
            services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                //  options.JsonSerializerOptions.IgnoreNullValues =true;
            });
            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseForwardedHeaders();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseForwardedHeaders();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //  app.UseHttpsRedirection();
            app.ApplicationServices.GetRequiredService<ICronScheduler>().Start();
            _=app.ApplicationServices.GetRequiredService<ClusterState>().Init();
            app.UseRouting();

            app.UseSpaStaticFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            if (env.IsProduction())
                app.UseMiddleware<ReverseProxyMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}