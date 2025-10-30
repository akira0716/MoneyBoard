using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MoneyBoard.Data;
using MoneyBoard.Models;
using MoneyBoard.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MoneyBoard.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ICsvService _csvService;
        private readonly IRepository<Transaction> _transactionRepository;
        private readonly IRepository<Mapping> _mappingRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<ImportHistory> _importHistoryRepository;

        public ObservableCollection<string> AvailableMonths { get; } = new();
        public ObservableCollection<string> ImportMonthOptions { get; } = new();
        public ObservableCollection<CategorySummary> SummaryItems { get; } = new();
        public ObservableCollection<ISeries> Series { get; set; } = new();

        [ObservableProperty]
        private string _selectedMonth;

        [ObservableProperty]
        private string _selectedImportMonth;

        public MainPageViewModel(
            ICsvService csvService,
            IRepository<Transaction> transactionRepository,
            IRepository<Mapping> mappingRepository,
            IRepository<Category> categoryRepository,
            IRepository<ImportHistory> importHistoryRepository)
        {
            _csvService = csvService;
            _transactionRepository = transactionRepository;
            _mappingRepository = mappingRepository;
            _categoryRepository = categoryRepository;
            _importHistoryRepository = importHistoryRepository;

            LoadAvailableMonthsAsync();
            InitializeImportMonthOptions();
        }

        private void InitializeImportMonthOptions()
        {
            var currentDate = DateTime.Now;
            for (int i = 0; i < 12; i++)
            {
                var date = currentDate.AddMonths(-i);
                ImportMonthOptions.Add(date.ToString("yyyy年MM月"));
            }
            SelectedImportMonth = ImportMonthOptions.FirstOrDefault();
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
                if (string.IsNullOrEmpty(SelectedImportMonth))
                {
                    await Application.Current.MainPage.DisplayAlert("エラー", "登録する月を選択してください。", "OK");
                    return;
                }

                var targetYear = int.Parse(SelectedImportMonth.Substring(0, 4));
                var targetMonth = int.Parse(SelectedImportMonth.Substring(5, 2));

                Debug.WriteLine("LoadCsvAsync command executed.");
                var transactions = (await _csvService.PickAndParseCsvAsync()).ToList();

                if (transactions == null || !transactions.Any())
                {
                    Debug.WriteLine("No transactions loaded or file picker cancelled.");
                    return;
                }

                // ファイルハッシュの計算
                var fileHash = CalculateFileHash(transactions);

                // 月の検証
                var allowedMonths = new HashSet<(int year, int month)>
                {
                    (targetYear, targetMonth),
                    (targetMonth == 1 ? targetYear - 1 : targetYear, targetMonth == 1 ? 12 : targetMonth - 1)
                };

                var invalidTransactions = transactions
                    .Where(t => !allowedMonths.Contains((t.UsageDate.Year, t.UsageDate.Month)))
                    .ToList();

                if (invalidTransactions.Any())
                {
                    var invalidDates = string.Join(", ",
                        invalidTransactions
                            .Select(t => t.UsageDate.ToString("yyyy年MM月"))
                            .Distinct()
                            .OrderBy(d => d));

                    await Application.Current.MainPage.DisplayAlert(
                        "エラー",
                        $"選択した月とその前月以外のデータが含まれています。\n含まれている月: {invalidDates}",
                        "OK");
                    return;
                }

                // 既存のインポート履歴をチェック
                var existingHistory = (await _importHistoryRepository.FindAsync(h =>
                    h.Year == targetYear && h.Month == targetMonth)).FirstOrDefault();

                if (existingHistory != null)
                {
                    if (existingHistory.FileHash == fileHash)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "情報",
                            $"{SelectedImportMonth}は既に同じ内容で登録済みです。",
                            "OK");
                        return;
                    }
                    else
                    {
                        var result = await Application.Current.MainPage.DisplayAlert(
                            "警告",
                            $"{SelectedImportMonth}は既に登録済みですが、ファイルの内容が異なります。\n上書きしますか？",
                            "はい",
                            "いいえ");

                        if (!result)
                            return;

                        // 既存のトランザクションを削除
                        var existingTransactions = await _transactionRepository.FindAsync(t =>
                            t.UsageDate.Year == targetYear && t.UsageDate.Month == targetMonth);

                        foreach (var t in existingTransactions)
                        {
                            _transactionRepository.Delete(t);
                        }
                        await _transactionRepository.SaveChangesAsync();

                        // 履歴を更新
                        _importHistoryRepository.Delete(existingHistory);
                        await _importHistoryRepository.SaveChangesAsync();
                    }
                }

                // マッピングの適用
                var mappings = (await _mappingRepository.GetAllAsync())
                                .ToDictionary(m => m.UsageName, m => m.CategoryId);

                foreach (var t in transactions)
                {
                    if (mappings.TryGetValue(t.UsageName, out var categoryId))
                    {
                        t.CategoryId = categoryId;
                    }
                }

                // トランザクションの保存
                await _transactionRepository.AddRangeAsync(transactions);
                await _transactionRepository.SaveChangesAsync();

                // インポート履歴の保存
                var history = new ImportHistory
                {
                    Year = targetYear,
                    Month = targetMonth,
                    FileHash = fileHash,
                    ImportedAt = DateTime.Now,
                    TransactionCount = transactions.Count
                };
                await _importHistoryRepository.AddAsync(history);
                await _importHistoryRepository.SaveChangesAsync();

                Debug.WriteLine($"{transactions.Count} transactions saved to database.");
                await Application.Current.MainPage.DisplayAlert(
                    "成功",
                    $"{SelectedImportMonth}に{transactions.Count}件の取引データを登録しました。",
                    "OK");

                await LoadAvailableMonthsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during CSV import: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "エラー",
                    "インポート中にエラーが発生しました。詳細はログを確認してください。",
                    "OK");
            }
        }

        private string CalculateFileHash(List<Transaction> transactions)
        {
            // トランザクションの内容からハッシュを計算
            var sb = new StringBuilder();
            foreach (var t in transactions.OrderBy(x => x.UsageDate).ThenBy(x => x.UsageName))
            {
                sb.AppendLine($"{t.UsageDate:yyyyMMdd}|{t.UsageName}|{t.Amount}");
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
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

        [RelayCommand]
        private async Task GoToCategoryDetailAsync(CategorySummary summary)
        {
            if (summary == null) return;

            var categories = await _categoryRepository.GetAllAsync();
            var category = categories.FirstOrDefault(c => c.Name == summary.CategoryName);

            if (category == null) return;

            var navigationParameter = new Dictionary<string, object>
            {
                { "CategoryId", category.Id },
                { "CategoryName", summary.CategoryName },
                { "Month", SelectedMonth }
            };

            await Shell.Current.GoToAsync("CategoryDetailPage", navigationParameter);
        }
    }
}