using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using StockAdvantage.Components;
using StockAdvantage.Models;

namespace StockAdvantage.Hubs;

public class StockService
{
    private readonly IHubContext<StockHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "e969b9577cmshd3d428eb03f480dp13f022jsn0232f94b8400";
    private decimal _balance = 50000m;
    private List<PurchasedStock> _purchasedStocks = new();
    public StockService(IHubContext<StockHub> hubContext, HttpClient httpClient)
    {
        _hubContext = hubContext;
        _httpClient = httpClient;
    }

    public async Task SendStockPriceUpdate(string symbol)
    {
        var price = await GetStockPriceAsync(symbol);
        await _hubContext.Clients.All.SendAsync("ReceiveStockPriceUpdate", symbol, price);
    }
    
    public List<PurchasedStock> GetPurchasedStocks()
    {
        return _purchasedStocks;
    }
    
    public decimal GetBalance()
    {
        return _balance;
    }
    
    public async Task<decimal> GetStockPriceAsync(string symbol)
    {
        try
        {
            var url = $"https://apidojo-yahoo-finance-v1.p.rapidapi.com/stock/v2/get-summary?symbol={symbol}&region=US";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-RapidAPI-Key", _apiKey);
            request.Headers.Add("X-RapidAPI-Host", "apidojo-yahoo-finance-v1.p.rapidapi.com");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Response: {responseContent}");

            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("price", out var priceElement) &&
                priceElement.TryGetProperty("regularMarketPrice", out var marketPriceElement) &&
                marketPriceElement.TryGetProperty("raw", out var rawPriceElement) &&
                rawPriceElement.TryGetDecimal(out var stockPrice))
            {
                return stockPrice;
            }

            throw new Exception("Stock price not available.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching stock price: {ex.Message}");
            return 0m; 
        }
    }
    
    public async Task BuyStockAsync(string symbol, int shares, decimal purchasePrice)
    {
        if (shares <= 0 || purchasePrice <= 0) return;

        decimal totalCost = shares * purchasePrice;
        if (_balance < totalCost)
        {
            Console.WriteLine("Not enough balance to buy shares.");
            return;
        }

        _balance -= totalCost;
        var purchasedStock = new PurchasedStock
        {
            Symbol = symbol,
            Shares = shares,
            PurchasePrice = purchasePrice,
            PurchaseDate = DateTime.UtcNow
        };

        _purchasedStocks.Add(purchasedStock);
        await NotifyPortfolioUpdate();
        await NotifyTransaction(symbol, shares, purchasePrice, 0, "Buy");
        Console.WriteLine(purchasedStock);

        await _hubContext.Clients.All.SendAsync("StockPurchased", symbol, shares, purchasePrice, _balance);
    }
    
    public async Task NotifyPortfolioUpdate()
    {
        await _hubContext.Clients.All.SendAsync("ReceivePortfolioUpdate", _purchasedStocks, _balance);
    }

    public async Task NotifyTransaction(string symbol, int shares, decimal price, decimal profit, string type)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTransactionUpdate", symbol, shares, price, profit, type);
    }

    public async Task SellStockAsync(string symbol, int shares, decimal sellPrice, DateTime sellDate)
    {
        var stock = _purchasedStocks.FirstOrDefault(s => s.Symbol == symbol && s.Shares >= shares);
        if (stock == null)
        {
            Console.WriteLine("Not enough shares to sell.");
            return;
        }

        decimal profit = (sellPrice * shares) - (stock.PurchasePrice * shares);
        int holdingDays = (sellDate - stock.PurchaseDate).Days;
        decimal fee = (holdingDays <= 7) ? profit * 0.18m : profit * 0.05m;
        decimal finalProfit = profit - fee;

        _balance += (stock.PurchasePrice * shares) + finalProfit;
        stock.Shares -= shares;

        if (stock.Shares == 0) _purchasedStocks.Remove(stock);

        await NotifyPortfolioUpdate();
        await NotifyTransaction(symbol, shares, sellPrice, finalProfit, "Sell");
    }
    


}