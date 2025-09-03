
using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Models;
using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Services;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            builder.Services.Configure<DocumentIntelligenceOptions>(
                builder.Configuration.GetSection("Azure:DocumentIntelligence"));

            builder.Services.AddSingleton<DocumentIntelligenceService>();
            builder.Services.AddSingleton<ChatGptService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
