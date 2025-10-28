using Microsoft.Extensions.Logging;
using MoneyBoard.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace MoneyBoard
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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

            return builder.Build();
        }
    }
}
