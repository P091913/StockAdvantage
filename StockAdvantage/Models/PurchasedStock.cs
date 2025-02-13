namespace StockAdvantage.Models;

public class PurchasedStock
{
    public string Symbol { get; set; } = "";
    public int Shares { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
}