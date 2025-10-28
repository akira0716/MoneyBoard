using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBoard.Data;
using MoneyBoard.Models;
using MoneyBoard.Services;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace MoneyBoard.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ICsvService _csvService;
        private readonly IRepository<Transaction> _transactionRepository;

        public MainPageViewModel(ICsvService csvService, IRepository<Transaction> transactionRepository)
        {
            _csvService = csvService;
            _transactionRepository = transactionRepository;
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
                    var newTransactions = new List<Transaction>();
                    foreach (var t in transactions)
                    {
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
    }
}
