using MoneyBoard.ViewModels;

namespace MoneyBoard.Views
{
    public partial class UncategorizedTransactionsPage : ContentPage
    {
        private readonly UncategorizedTransactionsViewModel _viewModel;

        public UncategorizedTransactionsPage(UncategorizedTransactionsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }
    }
}
