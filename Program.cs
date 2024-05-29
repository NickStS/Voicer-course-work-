using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voicer.Data;
using Voicer.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using Voicer.Controllers;

namespace VoiceAssistant
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();


            builder.Services.AddHttpClient();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSingleton<EmailService>();

            builder.Services.AddScoped<RemindersController>();

            builder.Services.AddScoped<IVoiceService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found.");
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
                var remindersController = provider.GetRequiredService<RemindersController>();
                var logger = provider.GetRequiredService<ILogger<VoiceService>>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                return new VoiceService(httpClient, apiKey, context, userManager, remindersController, logger, httpContextAccessor);
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });

            app.Run();
        }
    }
}
