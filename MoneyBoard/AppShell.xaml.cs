using MoneyBoard.Views;

namespace MoneyBoard
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(CategoryManagementPage), typeof(CategoryManagementPage));
            Routing.RegisterRoute(nameof(UncategorizedTransactionsPage), typeof(UncategorizedTransactionsPage));
            Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
        }
    }
}