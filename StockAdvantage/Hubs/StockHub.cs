using Microsoft.AspNetCore.SignalR;
using StockAdvantage.Models;

namespace StockAdvantage.Hubs;

public class StockHub : Hub
{
    private readonly StockService _stockService;
    private readonly ILogger<StockHub> _logger;

    public StockHub(StockService stockService, ILogger<StockHub> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    public async Task SendStockUpdate(string symbol)
    {
        var price = await _stockService.GetStockPriceAsync(symbol);
        _logger.LogInformation($"Sending update for {symbol}: {price}");
        await Clients.All.SendAsync("ReceiveStockUpdate", symbol, price);
    }
    public async Task BuyStock(string symbol, int shares, decimal price)
    {
        await _stockService.BuyStockAsync(symbol, shares, price);
    }
    
    public List<PurchasedStock> GetPurchasedStocks()
    {
        return _stockService.GetPurchasedStocks();
    }

    public decimal GetBalance()
    {
        return _stockService.GetBalance();
    }
    public async Task SellStock(string symbol, int shares, decimal sellPrice, DateTime sellDate)
    {
        await _stockService.SellStockAsync(symbol, shares, sellPrice, sellDate);
    }

}