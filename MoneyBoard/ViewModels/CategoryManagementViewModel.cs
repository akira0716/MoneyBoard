using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MoneyBoard.ViewModels
{
    public class ColorItem
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public string HexCode { get; set; }
    }

    public partial class CategoryManagementViewModel : ObservableObject
    {
        private readonly IRepository<Category> _categoryRepository;

        [ObservableProperty]
        private string _newCategoryName;

        [ObservableProperty]
        private ColorItem _selectedColorItem;

        public ObservableCollection<Category> Categories { get; } = new();

        public ObservableCollection<ColorItem> AvailableColors { get; set; }

        public Color SelectedColor => SelectedColorItem?.Color ?? Colors.Gray;

        public CategoryManagementViewModel(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;

            // �Œ�J���[���X�g�̏�����
            AvailableColors = new ObservableCollection<ColorItem>
            {
                new ColorItem { Name = "Red", Color = Color.FromArgb("#FF0000"), HexCode = "#FF0000" },
                new ColorItem { Name = "Blue", Color = Color.FromArgb("#0000FF"), HexCode = "#0000FF" },
                new ColorItem { Name = "Green", Color = Color.FromArgb("#00FF00"), HexCode = "#00FF00" },
                new ColorItem { Name = "Yellow", Color = Color.FromArgb("#FFFF00"), HexCode = "#FFFF00" },
                new ColorItem { Name = "Orange", Color = Color.FromArgb("#FFA500"), HexCode = "#FFA500" },
                new ColorItem { Name = "Purple", Color = Color.FromArgb("#800080"), HexCode = "#800080" },
                new ColorItem { Name = "Pink", Color = Color.FromArgb("#FFC0CB"), HexCode = "#FFC0CB" },
                new ColorItem { Name = "Cyan", Color = Color.FromArgb("#00FFFF"), HexCode = "#00FFFF" },
                new ColorItem { Name = "Gray", Color = Color.FromArgb("#808080"), HexCode = "#808080" },
                new ColorItem { Name = "Brown", Color = Color.FromArgb("#A52A2A"), HexCode = "#A52A2A" },
                new ColorItem { Name = "Teal", Color = Color.FromArgb("#008080"), HexCode = "#008080" },
                new ColorItem { Name = "Lime", Color = Color.FromArgb("#32CD32"), HexCode = "#32CD32" }
            };

            // �f�t�H���g�J���[��ݒ�
            SelectedColorItem = AvailableColors[0];

            LoadCategoriesAsync();
        }

        partial void OnSelectedColorItemChanged(ColorItem value)
        {
            OnPropertyChanged(nameof(SelectedColor));
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

            var newCategory = new Category
            {
                Name = NewCategoryName,
                ColorHex = SelectedColorItem.HexCode
            };

            await _categoryRepository.AddAsync(newCategory);
            await _categoryRepository.SaveChangesAsync();

            // Add to collection and keep it sorted
            var sortedCategories = new ObservableCollection<Category>(
                Categories.Union(new[] { newCategory }).OrderBy(c => c.Name));

            Categories.Clear();
            foreach (var c in sortedCategories)
                Categories.Add(c);

            // ���͂��N���A
            NewCategoryName = string.Empty;
            SelectedColorItem = AvailableColors[0]; // �f�t�H���g�ɖ߂�
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