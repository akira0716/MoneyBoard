using MoneyBoard.Models;

namespace MoneyBoard.Services
{
    public interface ICsvService
    {
        Task<IEnumerable<Transaction>> PickAndParseCsvAsync();
    }
}
