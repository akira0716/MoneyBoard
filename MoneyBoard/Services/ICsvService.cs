using MoneyBoard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyBoard.Services
{
    public interface ICsvService
    {
        Task<IEnumerable<Transaction>> PickAndParseCsvAsync();
    }
}
