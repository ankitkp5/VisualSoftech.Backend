using Microsoft.OpenApi.Models;

namespace VisualSoftech.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // App listening URLs
            builder.WebHost.UseUrls(
                "http://0.0.0.0:5160",
                "https://0.0.0.0:7296",
                "https://localhost:7296",
                "http://localhost:5160"
            );

            

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowAnyOrigin();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "VisualSoftech.Backend",
                    Version = "v1"
                });
            });

            var app = builder.Build();

            
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("AllowAll");

           
            app.UseSwagger();
            app.UseSwaggerUI();

            
            app.MapControllers();

            app.Run();
        }
    }
}
