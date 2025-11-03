using MoneyBoard.Data;

namespace MoneyBoard
{
    public partial class App : Application
    {
        public App(ApplicationDbContext context)
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}