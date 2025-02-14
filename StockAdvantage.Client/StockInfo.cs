namespace StockAdvantage.Client;

public class StockInfo
{
    public string Symbol { get; set; }
    public decimal price { get; set; }
    public int ownedShares { get; set; }
    
    public decimal cummulativeValue => price * ownedShares;

    public StockInfo(string symbol, decimal price, int ownedShares)
    {
        Symbol = symbol;
        this.price = price;
        this.ownedShares = ownedShares;
    }
}