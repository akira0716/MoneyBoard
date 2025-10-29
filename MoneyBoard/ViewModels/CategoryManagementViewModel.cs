using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MoneyBoard.ViewModels
{
    public partial class CategoryManagementViewModel : ObservableObject
    {
        private readonly IRepository<Category> _categoryRepository;

        [ObservableProperty]
        private string _newCategoryName;

        public ObservableCollection<Category> Categories { get; } = new();

        public CategoryManagementViewModel(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
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

            var newCategory = new Category { Name = NewCategoryName };
            await _categoryRepository.AddAsync(newCategory);
            await _categoryRepository.SaveChangesAsync();

            // Add to collection and keep it sorted
            var sortedCategories = new ObservableCollection<Category>(Categories.Union(new[] { newCategory }).OrderBy(c => c.Name));
            Categories.Clear();
            foreach (var c in sortedCategories) Categories.Add(c);

            NewCategoryName = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteCategoryAsync(Category category)
        {
            if (category == null)
                return;

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();

            Categories.Remove(category);
        }
    }
}
