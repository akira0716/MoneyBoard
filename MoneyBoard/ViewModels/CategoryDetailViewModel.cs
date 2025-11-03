using CommunityToolkit.Mvvm.ComponentModel;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MoneyBoard.ViewModels
{
    [QueryProperty(nameof(CategoryId), nameof(CategoryId))]
    [QueryProperty(nameof(CategoryName), nameof(CategoryName))]
    [QueryProperty(nameof(Month), nameof(Month))]
    public partial class CategoryDetailViewModel : ObservableObject
    {
        private readonly IRepository<Transaction> _transactionRepository;

        [ObservableProperty]
        private int _categoryId;

        [ObservableProperty]
        private string _categoryName;

        [ObservableProperty]
        private string _month;

        [ObservableProperty]
        private int _totalAmount;

        public ObservableCollection<Transaction> Transactions { get; } = new();

        public CategoryDetailViewModel(IRepository<Transaction> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task LoadTransactionsAsync()
        {
            try
            {
                Transactions.Clear();

                if (string.IsNullOrEmpty(Month))
                    return;

                var year = int.Parse(Month.Substring(0, 4));
                var monthNum = int.Parse(Month.Substring(5, 2));

                var transactions = await _transactionRepository.FindAsync(t =>
                    t.CategoryId == CategoryId &&
                    t.UsageDate.Year == year &&
                    t.UsageDate.Month == monthNum);

                var sortedTransactions = transactions.OrderByDescending(t => t.UsageDate)
                                                    .ThenBy(t => t.UsageName);

                foreach (var transaction in sortedTransactions)
                {
                    Transactions.Add(transaction);
                }

                TotalAmount = Transactions.Sum(t => t.Amount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load transactions: {ex.Message}");
            }
        }
    }
}