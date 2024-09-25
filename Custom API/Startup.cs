using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Custom_API
{
    // The Startup class is the entry point for configuring services and the app's request pipeline.
    public class Startup
    {
        // ConfigureServices is used to add services to the container.
        // This method is called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // AddControllers is used to add support for controllers, allowing the application to handle API requests.
            services.AddControllers();
        }

        // Configure is used to define how the application responds to HTTP requests.
        // This method is called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // If the application is running in the development environment, enable detailed error pages.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable routing middleware, which matches incoming HTTP requests to the appropriate route.
            app.UseRouting();

            // Configure the endpoints to map controller routes.
            // This allows the application to handle requests based on the controllers that are defined.
            app.UseEndpoints(endpoints =>
            {
                // Maps the controller actions to the request pipeline. 
                // Any HTTP request that matches a controller's route will be processed accordingly.
                endpoints.MapControllers();
            });
        }
    }
}
