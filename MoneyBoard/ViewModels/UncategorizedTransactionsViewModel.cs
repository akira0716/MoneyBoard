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

                // �S�Ă̎�����痘�p�����擾
                var allTransactions = await _transactionRepo.GetAllAsync();
                var usageNameGroups = allTransactions
                    .GroupBy(t => t.UsageName)
                    .Select(g => new
                    {
                        UsageName = g.Key,
                        CategoryId = g.First().CategoryId
                    })
                    .OrderBy(x => x.UsageName);

                // �J�e�S���[�����擾
                var categories = (await _categoryRepo.GetAllAsync()).ToDictionary(c => c.Id, c => c);

                // �t�B���^�����O
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

                // ���p�\�ȃJ�e�S���[�����[�h
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
                // 1. ����UsageName�����S�Ă̎�����X�V
                var transactionsToUpdate = await _transactionRepo.FindAsync(
                    t => t.UsageName == SelectedUsageNameMapping.UsageName);

                foreach (var t in transactionsToUpdate)
                {
                    t.CategoryId = SelectedCategory.Id;
                    _transactionRepo.Update(t);
                }

                // 2. �����̃}�b�s���O���m�F
                var existingMappings = await _mappingRepo.FindAsync(
                    m => m.UsageName == SelectedUsageNameMapping.UsageName);

                if (existingMappings.Any())
                {
                    // �����̃}�b�s���O���X�V
                    foreach (var mapping in existingMappings)
                    {
                        mapping.CategoryId = SelectedCategory.Id;
                        _mappingRepo.Update(mapping);
                    }
                }
                else
                {
                    // �V�����}�b�s���O���쐬
                    var newMapping = new Mapping
                    {
                        UsageName = SelectedUsageNameMapping.UsageName,
                        CategoryId = SelectedCategory.Id
                    };
                    await _mappingRepo.AddAsync(newMapping);
                }

                // 3. �ύX��ۑ�
                await _transactionRepo.SaveChangesAsync();
                await _mappingRepo.SaveChangesAsync();

                // 4. UI���X�V
                if (ShowOnlyUncategorized)
                {
                    // �����ނ̂ݕ\���̏ꍇ�́A���X�g����폜
                    UsageNameMappings.Remove(SelectedUsageNameMapping);
                }
                else
                {
                    // �S�ĕ\���̏ꍇ�́A�J�e�S���[�����X�V
                    SelectedUsageNameMapping.CurrentCategory = SelectedCategory;
                    SelectedUsageNameMapping.CategoryId = SelectedCategory.Id;
                }

                SelectedUsageNameMapping = null;
                SelectedCategory = null;

                await Application.Current.MainPage.DisplayAlert(
                    "����",
                    "�J�e�S���[������Ɋ��蓖�Ă��܂����B",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to assign category: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "�G���[",
                    "�J�e�S���[�̊��蓖�Ē��ɃG���[���������܂����B",
                    "OK");
            }
        }

        [RelayCommand]
        private void SelectUsageNameMapping(UsageNameMapping mapping)
        {
            SelectedUsageNameMapping = mapping;

            // �����̃J�e�S���[������ꍇ�́A�����I����Ԃɂ���
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

    // ���p���ƃJ�e�S���[�̃}�b�s���O����ێ�����N���X
    public partial class UsageNameMapping : ObservableObject
    {
        [ObservableProperty]
        private string _usageName;

        [ObservableProperty]
        private Category _currentCategory;

        [ObservableProperty]
        private int? _categoryId;

        public string DisplayText => CurrentCategory != null
            ? $"{UsageName} �� {CurrentCategory.Name}"
            : UsageName;

        public bool HasCategory => CurrentCategory != null;
    }
}