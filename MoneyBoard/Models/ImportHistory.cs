namespace MoneyBoard.Models;

public class ImportHistory
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string FileHash { get; set; }
    public DateTime ImportedAt { get; set; }
    public int TransactionCount { get; set; }
}