namespace MoneyBoard.Models;

public class Mapping
{
    public int Id { get; set; }
    public string UsageName { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
}
