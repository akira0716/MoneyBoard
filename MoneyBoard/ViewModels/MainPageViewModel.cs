using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MoneyBoard.Data;
using MoneyBoard.Models;
using MoneyBoard.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;

namespace MoneyBoard.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ICsvService _csvService;
        private readonly IRepository<Transaction> _transactionRepository;
        private readonly IRepository<Mapping> _mappingRepository;
        private readonly IRepository<Category> _categoryRepository;

        public ObservableCollection<string> AvailableMonths { get; } = new();
        public ObservableCollection<CategorySummary> SummaryItems { get; } = new();
        public ObservableCollection<ISeries> Series { get; set; } = new();

        [ObservableProperty]
        private string _selectedMonth;

        public MainPageViewModel(ICsvService csvService, IRepository<Transaction> transactionRepository, IRepository<Mapping> mappingRepository, IRepository<Category> categoryRepository)
        {
            _csvService = csvService;
            _transactionRepository = transactionRepository;
            _mappingRepository = mappingRepository;
            _categoryRepository = categoryRepository;

            LoadAvailableMonthsAsync();
        }

        partial void OnSelectedMonthChanged(string value)
        {
            if (value != null)
            {
                LoadSummaryAsync(value);
            }
            else
            {
                SummaryItems.Clear();
                UpdateChart();
            }
        }

        public async Task LoadAvailableMonthsAsync()
        {
            try
            {
                var allTransactions = await _transactionRepository.GetAllAsync();
                var months = allTransactions
                                .Select(t => t.UsageDate.ToString("yyyy年MM月", CultureInfo.CurrentCulture))
                                .Distinct()
                                .OrderByDescending(m => m)
                                .ToList();

                AvailableMonths.Clear();
                foreach (var month in months)
                {
                    AvailableMonths.Add(month);
                }

                if (AvailableMonths.Any() && SelectedMonth == null)
                {
                    SelectedMonth = AvailableMonths.First();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load available months: {ex.Message}");
            }
        }

        public async Task LoadSummaryAsync(string month)
        {
            try
            {
                SummaryItems.Clear();
                if (string.IsNullOrEmpty(month)) return;

                var year = int.Parse(month.Substring(0, 4));
                var monthNum = int.Parse(month.Substring(5, 2));

                var transactionsInMonth = (await _transactionRepository.FindAsync(t =>
                    t.UsageDate.Year == year && t.UsageDate.Month == monthNum)).ToList();

                var totalAmountInMonth = transactionsInMonth.Sum(t => t.Amount);

                var categories = (await _categoryRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c);

                var groupedTransactions = transactionsInMonth
                                            .Where(t => t.CategoryId.HasValue)
                                            .GroupBy(t => t.CategoryId.Value)
                                            .Select(g => new
                                            {
                                                CategoryId = g.Key,
                                                TotalAmount = g.Sum(t => t.Amount)
                                            })
                                            .OrderByDescending(g => g.TotalAmount);

                foreach (var group in groupedTransactions)
                {
                    var category = categories.TryGetValue(group.CategoryId, out var cat) ? cat : null;
                    var categoryName = category?.Name ?? "未分類";
                    var percentage = (double)group.TotalAmount / totalAmountInMonth * 100;

                    SummaryItems.Add(new CategorySummary
                    {
                        CategoryName = categoryName,
                        TotalAmount = group.TotalAmount,
                        Percentage = percentage,
                        ColorHex = category?.ColorHex
                    });
                }
                UpdateChart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load summary for {month}: {ex.Message}");
            }
        }

        private void UpdateChart()
        {
            Series.Clear();
            foreach (var item in SummaryItems)
            {
                Series.Add(new PieSeries<double>
                {
                    Values = new[] { (double)item.TotalAmount },
                    Name = item.CategoryName,
                    ToolTipLabelFormatter = (point) => $"¥{point.Model:N0}",
                    DataLabelsFormatter = (_) => $"{item.CategoryName}",
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    Fill = item.ColorHex != null ? new SolidColorPaint(SKColor.Parse(item.ColorHex)) : null
                });
            }
        }

        [RelayCommand]
        private async Task LoadCsvAsync()
        {
            try
            {
                Debug.WriteLine("LoadCsvAsync command executed.");
                var transactions = await _csvService.PickAndParseCsvAsync();

                if (transactions != null && transactions.Any())
                {
                    var mappings = (await _mappingRepository.GetAllAsync())
                                    .ToDictionary(m => m.UsageName, m => m.CategoryId);

                    var newTransactions = new List<Transaction>();
                    foreach (var t in transactions)
                    {
                        if (mappings.TryGetValue(t.UsageName, out var categoryId))
                        {
                            t.CategoryId = categoryId;
                        }

                        var exists = await _transactionRepository.FindAsync(x => x.UsageDate == t.UsageDate && x.UsageName == t.UsageName && x.Amount == t.Amount);
                        if (!exists.Any())
                        {
                            newTransactions.Add(t);
                        }
                    }

                    if (newTransactions.Any())
                    {
                        await _transactionRepository.AddRangeAsync(newTransactions);
                        var savedCount = await _transactionRepository.SaveChangesAsync();

                        Debug.WriteLine($"{savedCount} transactions saved to database.");
                        await Application.Current.MainPage.DisplayAlert("Success", $"{savedCount} new transactions have been successfully imported.", "OK");
                        await LoadAvailableMonthsAsync(); // Refresh months after import
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Info", "No new transactions to import. All records in the file already exist in the database.", "OK");
                    }
                }
                else
                {
                    Debug.WriteLine("No transactions loaded or file picker cancelled.");
                    await Application.Current.MainPage.DisplayAlert("Info", "No transactions were loaded from the file.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during CSV import: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred during the import process. Please check the logs.", "OK");
            }
        }

        [RelayCommand]
        private async Task GoToCategoryManagementAsync()
        {
            await Shell.Current.GoToAsync("CategoryManagementPage");
        }

        [RelayCommand]
        private async Task GoToUncategorizedTransactionsAsync()
        {
            await Shell.Current.GoToAsync("UncategorizedTransactionsPage");
        }
    }
}
