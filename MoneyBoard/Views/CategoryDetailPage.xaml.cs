using MoneyBoard.ViewModels;

namespace MoneyBoard.Views
{
    public partial class CategoryDetailPage : ContentPage
    {
        private readonly CategoryDetailViewModel _viewModel;

        public CategoryDetailPage(CategoryDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTransactionsAsync();
        }
    }
}