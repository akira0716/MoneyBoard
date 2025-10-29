using MoneyBoard.Data;

namespace MoneyBoard
{
    public partial class App : Application
    {
        public App(ApplicationDbContext context)
        {
            InitializeComponent();

            context.Database.EnsureCreated();

            MainPage = new AppShell();
        }
    }
}