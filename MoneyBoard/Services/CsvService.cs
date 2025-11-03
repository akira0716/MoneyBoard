using MoneyBoard.Models;

namespace MoneyBoard.Services
{
    public class CsvService : ICsvService
    {
        public async Task<IEnumerable<Transaction>> PickAndParseCsvAsync()
        {
            var transactions = new List<Transaction>();
            try
            {
                var pickOptions = new PickOptions
                {
                    PickerTitle = "Select a CSV file",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.WinUI, new[] { ".csv" } },
                            { DevicePlatform.macOS, new[] { "public.comma-separated-values-text" } }, // UTI for CSV
                            { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                            { DevicePlatform.Android, new[] { "text/csv" } },
                        }),
                };

                var result = await FilePicker.Default.PickAsync(pickOptions);
                if (result == null)
                {
                    return transactions; // User cancelled picker
                }

                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);

                // Skip header row
                await reader.ReadLineAsync();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var values = line.Split(',');

                    if (values.Length >= 5) // Check for minimum required columns
                    {
                        var dateStr = values[0].Trim('"');
                        var nameStr = values[1].Trim('"');
                        var amountStr = values[4].Trim('"'); // Index 4 for '利用金額'

                        if (DateTime.TryParse(dateStr, out var usageDate) &&
                            !string.IsNullOrWhiteSpace(nameStr) &&
                            int.TryParse(amountStr, out var amount))
                        {
                            transactions.Add(new Transaction
                            {
                                UsageDate = usageDate,
                                UsageName = nameStr.Trim(),
                                Amount = amount
                            });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to parse line: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file read error, parsing error)
                // For now, we can just write to debug output
                System.Diagnostics.Debug.WriteLine($"Error parsing CSV: {ex.Message}");
            }

            return transactions;
        }
    }
}
