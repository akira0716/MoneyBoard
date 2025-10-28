using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyBoard.ViewModels
{
    public partial class UncategorizedTransactionsViewModel : ObservableObject
    {
        private readonly IRepository<Transaction> _transactionRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Mapping> _mappingRepo;

        public ObservableCollection<string> UncategorizedUsageNames { get; } = new();
        public ObservableCollection<Category> AvailableCategories { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUsageNameSelected))]
        private string _selectedUsageName;

        [ObservableProperty]
        private Category _selectedCategory;

        public bool IsUsageNameSelected => !string.IsNullOrEmpty(SelectedUsageName);

        public UncategorizedTransactionsViewModel(IRepository<Transaction> transactionRepo, IRepository<Category> categoryRepo, IRepository<Mapping> mappingRepo)
        {
            _transactionRepo = transactionRepo;
            _categoryRepo = categoryRepo;
            _mappingRepo = mappingRepo;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                UncategorizedUsageNames.Clear();
                var transactions = await _transactionRepo.FindAsync(t => t.CategoryId == null);
                var usageNames = transactions.Select(t => t.UsageName).Distinct().OrderBy(name => name);
                foreach (var name in usageNames)
                {
                    UncategorizedUsageNames.Add(name);
                }

                AvailableCategories.Clear();
                var categories = await _categoryRepo.GetAllAsync();
                foreach (var c in categories.OrderBy(c => c.Name))
                {
                    AvailableCategories.Add(c);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load data: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AssignCategoryAsync()
        {
            if (string.IsNullOrEmpty(SelectedUsageName) || SelectedCategory == null)
                return;

            // Update all transactions with the same UsageName
            var transactionsToUpdate = await _transactionRepo.FindAsync(t => t.UsageName == SelectedUsageName && t.CategoryId == null);
            foreach (var t in transactionsToUpdate)
            {
                t.CategoryId = SelectedCategory.Id;
                _transactionRepo.Update(t);
            }

            // Create a new mapping for future imports
            var newMapping = new Mapping
            {
                UsageName = SelectedUsageName,
                CategoryId = SelectedCategory.Id
            };
            await _mappingRepo.AddAsync(newMapping);

            await _transactionRepo.SaveChangesAsync();
            await _mappingRepo.SaveChangesAsync();

            // Remove the now-categorized name from the list
            UncategorizedUsageNames.Remove(SelectedUsageName);

            SelectedUsageName = null;
            SelectedCategory = null;
        }
    }
}
