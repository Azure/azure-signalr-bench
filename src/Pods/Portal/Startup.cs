using System;
using System.Linq;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.BasicAuth;
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

            services.AddSingleton<IUserService, UserService>();

            // AAD auth
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));
            // Basic auth
            services.AddAuthentication(PerfConstants.AuthSchema.BasicAuth)
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>
                    (PerfConstants.AuthSchema.BasicAuth, null);

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PerfConstants.Policy.RoleLogin, policy =>
                {
                    policy.AddAuthenticationSchemes(PerfConstants.AuthSchema.BasicAuth);
                    policy.AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme);
                    policy.RequireAssertion(context =>
                    {
                        return context.Requirements.Count() > 1 ||
                            context.User.IsInRole(PerfConstants.Roles.Contributor);
                    });
                });
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme)
                    .RequireRole(PerfConstants.Roles.Contributor, PerfConstants.Roles.Reader)
                    .Build();
            });

            services
                .AddRazorPages()
                .AddMicrosoftIdentityUI();

            services.AddSingleton(
                sp => new SecretClient(
                    new Uri(Configuration[PerfConstants.ConfigurationKeys.KeyVaultUrlKey]), new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId = Configuration[PerfConstants.ConfigurationKeys.MsiAppId]
                        })
                ));
            services.AddSingleton<IPerfStorage>(sp =>
                {
                    var secretClient = sp.GetService<SecretClient>();
                    try
                    {
                        var saConnectionString = secretClient.GetSecretAsync("sa-accessKey").GetAwaiter().GetResult()
                            .Value.Value;
                        var cdbConnectionString = secretClient.GetSecretAsync("cdb-accessKey").GetAwaiter().GetResult()
                            .Value.Value;
                        return new PerfStorage(saConnectionString, cdbConnectionString);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Connection error:{e}");
                    }

                    return null;
                }
            );
            services.AddSingleton<ICronScheduler, CronScheduler>();
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
                app.UseMiddleware<ValidationMiddleware>();
            }

            //  app.UseHttpsRedirection();
            app.ApplicationServices.GetRequiredService<ICronScheduler>().Start();
            app.ApplicationServices.GetRequiredService<ClusterState>().Init().Wait();
            app.UseRouting();

            app.UseSpaStaticFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            if (env.IsProduction())
            {
                app.UseMiddleware<SecretMaskingMiddleware>();
                app.UseMiddleware<ReverseProxyMiddleware>();
            }
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment()) spa.UseReactDevelopmentServer("start");
            });
        }
    }
}