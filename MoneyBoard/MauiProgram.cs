using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyBoard.Data;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MoneyBoard
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseLiveCharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "moneyboard.db");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Filename={dbPath}"));

            builder.Services.AddSingleton<ViewModels.MainPageViewModel>();
            builder.Services.AddSingleton<MainPage>();

            builder.Services.AddSingleton<Services.ICsvService, Services.CsvService>();

            builder.Services.AddScoped(typeof(Data.IRepository<>), typeof(Data.Repository<>));

            builder.Services.AddTransient<ViewModels.CategoryManagementViewModel>();
            builder.Services.AddTransient<Views.CategoryManagementPage>();

            builder.Services.AddTransient<ViewModels.UncategorizedTransactionsViewModel>();
            builder.Services.AddTransient<Views.UncategorizedTransactionsPage>();

            builder.Services.AddTransient<ViewModels.CategoryDetailViewModel>();
            builder.Services.AddTransient<Views.CategoryDetailPage>();

            var app = builder.Build();
            // データベース初期化
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                DbInitializer.Initialize(context);
            }

            return app;
        }
    }
}