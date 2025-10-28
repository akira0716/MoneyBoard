using MoneyBoard.ViewModels;

namespace MoneyBoard.Views
{
    public partial class CategoryManagementPage : ContentPage
    {
        public CategoryManagementPage(CategoryManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
