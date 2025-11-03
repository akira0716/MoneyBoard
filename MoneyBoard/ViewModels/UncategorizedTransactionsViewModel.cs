using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MoneyBoard.ViewModels
{
    public partial class UncategorizedTransactionsViewModel : ObservableObject
    {
        private readonly IRepository<Transaction> _transactionRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Mapping> _mappingRepo;

        public ObservableCollection<UsageNameMapping> UsageNameMappings { get; } = new();
        public ObservableCollection<Category> AvailableCategories { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUsageNameSelected))]
        private UsageNameMapping _selectedUsageNameMapping;

        [ObservableProperty]
        private Category _selectedCategory;

        [ObservableProperty]
        private bool _showOnlyUncategorized = true;

        public bool IsUsageNameSelected => SelectedUsageNameMapping != null;

        public UncategorizedTransactionsViewModel(
            IRepository<Transaction> transactionRepo,
            IRepository<Category> categoryRepo,
            IRepository<Mapping> mappingRepo)
        {
            _transactionRepo = transactionRepo;
            _categoryRepo = categoryRepo;
            _mappingRepo = mappingRepo;
        }

        partial void OnShowOnlyUncategorizedChanged(bool value)
        {
            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                UsageNameMappings.Clear();

                // 全ての取引から利用名を取得
                var allTransactions = await _transactionRepo.GetAllAsync();
                var usageNameGroups = allTransactions
                    .GroupBy(t => t.UsageName)
                    .Select(g => new
                    {
                        UsageName = g.Key,
                        CategoryId = g.First().CategoryId
                    })
                    .OrderBy(x => x.UsageName);

                // カテゴリー情報を取得
                var categories = (await _categoryRepo.GetAllAsync()).ToDictionary(c => c.Id, c => c);

                // フィルタリング
                var filteredGroups = ShowOnlyUncategorized
                    ? usageNameGroups.Where(x => !x.CategoryId.HasValue)
                    : usageNameGroups;

                foreach (var group in filteredGroups)
                {
                    Category category = null;
                    if (group.CategoryId.HasValue && categories.TryGetValue(group.CategoryId.Value, out var cat))
                    {
                        category = cat;
                    }

                    UsageNameMappings.Add(new UsageNameMapping
                    {
                        UsageName = group.UsageName,
                        CurrentCategory = category,
                        CategoryId = group.CategoryId
                    });
                }

                // 利用可能なカテゴリーをロード
                AvailableCategories.Clear();
                var allCategories = await _categoryRepo.GetAllAsync();
                foreach (var c in allCategories.OrderBy(c => c.Name))
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
            if (SelectedUsageNameMapping == null || SelectedCategory == null)
                return;

            try
            {
                // 1. 同じUsageNameを持つ全ての取引を更新
                var transactionsToUpdate = await _transactionRepo.FindAsync(
                    t => t.UsageName == SelectedUsageNameMapping.UsageName);

                foreach (var t in transactionsToUpdate)
                {
                    t.CategoryId = SelectedCategory.Id;
                    _transactionRepo.Update(t);
                }

                // 2. 既存のマッピングを確認
                var existingMappings = await _mappingRepo.FindAsync(
                    m => m.UsageName == SelectedUsageNameMapping.UsageName);

                if (existingMappings.Any())
                {
                    // 既存のマッピングを更新
                    foreach (var mapping in existingMappings)
                    {
                        mapping.CategoryId = SelectedCategory.Id;
                        _mappingRepo.Update(mapping);
                    }
                }
                else
                {
                    // 新しいマッピングを作成
                    var newMapping = new Mapping
                    {
                        UsageName = SelectedUsageNameMapping.UsageName,
                        CategoryId = SelectedCategory.Id
                    };
                    await _mappingRepo.AddAsync(newMapping);
                }

                // 3. 変更を保存
                await _transactionRepo.SaveChangesAsync();
                await _mappingRepo.SaveChangesAsync();

                // 4. UIを更新
                if (ShowOnlyUncategorized)
                {
                    // 未分類のみ表示の場合は、リストから削除
                    UsageNameMappings.Remove(SelectedUsageNameMapping);
                }
                else
                {
                    // 全て表示の場合は、カテゴリー情報を更新
                    SelectedUsageNameMapping.CurrentCategory = SelectedCategory;
                    SelectedUsageNameMapping.CategoryId = SelectedCategory.Id;
                }

                SelectedUsageNameMapping = null;
                SelectedCategory = null;

                await Application.Current.MainPage.DisplayAlert(
                    "成功",
                    "カテゴリーが正常に割り当てられました。",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to assign category: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "エラー",
                    "カテゴリーの割り当て中にエラーが発生しました。",
                    "OK");
            }
        }

        [RelayCommand]
        private void SelectUsageNameMapping(UsageNameMapping mapping)
        {
            SelectedUsageNameMapping = mapping;

            // 既存のカテゴリーがある場合は、それを選択状態にする
            if (mapping?.CurrentCategory != null)
            {
                SelectedCategory = AvailableCategories.FirstOrDefault(
                    c => c.Id == mapping.CurrentCategory.Id);
            }
            else
            {
                SelectedCategory = null;
            }
        }
    }

    // 利用名とカテゴリーのマッピング情報を保持するクラス
    public partial class UsageNameMapping : ObservableObject
    {
        [ObservableProperty]
        private string _usageName;

        [ObservableProperty]
        private Category _currentCategory;

        [ObservableProperty]
        private int? _categoryId;

        public string DisplayText => CurrentCategory != null
            ? $"{UsageName} → {CurrentCategory.Name}"
            : UsageName;

        public bool HasCategory => CurrentCategory != null;
    }
}