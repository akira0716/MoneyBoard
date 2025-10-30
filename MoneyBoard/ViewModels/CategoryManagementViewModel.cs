using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MoneyBoard.ViewModels
{
    public partial class CategoryManagementViewModel : ObservableObject
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Transaction> _transactionRepository;
        private readonly IRepository<Mapping> _mappingRepository;

        [ObservableProperty]
        private string _newCategoryName;

        [ObservableProperty]
        private string _categoryColor;

        public ObservableCollection<Category> Categories { get; } = new();

        public CategoryManagementViewModel(
            IRepository<Category> categoryRepository,
            IRepository<Transaction> transactionRepository,
            IRepository<Mapping> mappingRepository)
        {
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
            _mappingRepository = mappingRepository;
            LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                Categories.Clear();
                foreach (var category in categories.OrderBy(c => c.Name))
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load categories: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                return;

            if (Categories.Any(c => c.Name.Equals(NewCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.WriteLine($"Category '{NewCategoryName}' already exists.");
                return;
            }

            // カラーコードのバリデーションと正規化
            string colorHex = NormalizeColorHex(CategoryColor);

            var newCategory = new Category
            {
                Name = NewCategoryName,
                ColorHex = colorHex
            };

            await _categoryRepository.AddAsync(newCategory);
            await _categoryRepository.SaveChangesAsync();

            // Add to collection and keep it sorted
            var sortedCategories = new ObservableCollection<Category>(
                Categories.Union(new[] { newCategory }).OrderBy(c => c.Name));

            Categories.Clear();
            foreach (var c in sortedCategories)
                Categories.Add(c);

            NewCategoryName = string.Empty;
            CategoryColor = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteCategoryAsync(Category category)
        {
            if (category == null)
                return;

            try
            {
                // 1. このカテゴリーに紐づいている取引を未分類に変更
                var relatedTransactions = await _transactionRepository.FindAsync(t => t.CategoryId == category.Id);
                foreach (var transaction in relatedTransactions)
                {
                    transaction.CategoryId = null;
                    _transactionRepository.Update(transaction);
                }

                // 2. このカテゴリーに紐づいているマッピングを削除
                var relatedMappings = await _mappingRepository.FindAsync(m => m.CategoryId == category.Id);
                foreach (var mapping in relatedMappings)
                {
                    _mappingRepository.Delete(mapping);
                }

                // 3. カテゴリー自体を削除
                _categoryRepository.Delete(category);

                // 4. すべての変更を保存
                await _transactionRepository.SaveChangesAsync();
                await _mappingRepository.SaveChangesAsync();
                await _categoryRepository.SaveChangesAsync();

                // 5. UIから削除
                Categories.Remove(category);

                Debug.WriteLine($"Category '{category.Name}' deleted successfully. Related transactions and mappings were updated.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete category: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "カテゴリーの削除中にエラーが発生しました。", "OK");
            }
        }

        /// <summary>
        /// カラーコードを検証して正規化します
        /// </summary>
        private string NormalizeColorHex(string colorInput)
        {
            if (string.IsNullOrWhiteSpace(colorInput))
                return "#808080"; // デフォルトグレー

            // #を除去
            colorInput = colorInput.Trim().TrimStart('#');

            // 6桁の16進数かチェック
            if (Regex.IsMatch(colorInput, "^[0-9A-Fa-f]{6}$"))
            {
                return $"#{colorInput.ToUpper()}";
            }

            Debug.WriteLine($"Invalid color code: {colorInput}. Using default gray.");
            return "#808080"; // 無効な場合はデフォルトグレー
        }
    }
}