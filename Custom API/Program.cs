using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Custom_API
{
    // The entry point for the Custom_API application.
    public class Program
    {
        // The Main method is the entry point of the application where execution begins.
        // It calls CreateHostBuilder to initialize and run the web host.
        public static void Main(string[] args)
        {
            // Build and run the web host using the configuration defined in CreateHostBuilder.
            CreateHostBuilder(args).Build().Run();
        }

        // This method creates and configures the web host for the application.
        // It sets up the host with default configurations and specifies the Startup class to use.
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Specifies the Startup class, which defines services and the request pipeline.
                    webBuilder.UseStartup<Startup>();
                });
    }
}
