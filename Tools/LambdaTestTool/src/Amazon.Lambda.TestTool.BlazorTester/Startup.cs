using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Amazon.Lambda.TestTool.BlazorTester.Services;

using Blazored.Modal;

namespace Amazon.Lambda.TestTool.BlazorTester
{
    public class Startup
    {
        public static void LaunchWebTester(LocalLambdaOptions lambdaOptions, bool openWindow)
        {
            var host = StartWebTesterAsync(lambdaOptions, openWindow).GetAwaiter().GetResult();
            host.WaitForShutdown();
        }

        public static async Task<IWebHost> StartWebTesterAsync(LocalLambdaOptions lambdaOptions, bool openWindow, CancellationToken token = default(CancellationToken))
        {
            var port = lambdaOptions.Port ?? Constants.DEFAULT_PORT;
            
            var url = $"http://localhost:{port}";

            var contentPath = Path.GetFullPath(Directory.GetCurrentDirectory());
            var builder = new WebHostBuilder()
                .UseKestrel()
                .SuppressStatusMessages(true)
                .ConfigureServices(services => services.AddSingleton(lambdaOptions))
                .UseContentRoot(contentPath)
                .UseUrls(url)
                .UseStartup<Startup>();

            var host = builder.Build();

            await host.StartAsync(token);
            Console.WriteLine($"Environment running at {url}");

            if (openWindow)
            {
                try
                {
                    var info = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = url
                    };
                    Process.Start(info);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error launching browser: {e.Message}");
                }
            }

            return host;
        }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRuntimeApiDataStore, RuntimeApiDataStore>();
            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();

            services.AddBlazoredModal();

            services.AddOptions<StaticFileOptions>()
                .PostConfigure(o =>
                {
                    var fileProvider = new ManifestEmbeddedFileProvider(typeof(Startup).Assembly, "wwwroot");

                    // Make sure we don't remove the existing file providers (blazor needs this)
                    o.FileProvider = new CompositeFileProvider(o.FileProvider, fileProvider);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
