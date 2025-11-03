namespace MoneyBoard.Models;

public class Transaction
{
    public int Id { get; set; }
    public DateTime UsageDate { get; set; }
    public string UsageName { get; set; }
    public int Amount { get; set; }
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
}
