using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kestrel.Interop.TestSites
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration((_, config) => config.AddCommandLine(args))
                .ConfigureLogging((_, factory) => factory.SetMinimumLevel(LogLevel.Trace))
                .UseKestrel(options =>
                    options.ListenAnyIP(0, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps();
                    }))
                .Configure(app =>
                    app.Run(async (context) =>
                    {
                        if (string.Equals(context.Request.Query["TestMethod"], "POST", StringComparison.OrdinalIgnoreCase))
                        {
                            await context.Response.SendFileAsync("post.html");
                        }
                        else
                        {
                            await context.Response.WriteAsync($"Interop {context.Request.Protocol} {context.Request.Method}");
                        }
                    }))
                .UseContentRoot(Directory.GetCurrentDirectory());


            await hostBuilder.Build().RunAsync();
        }
    }
}
