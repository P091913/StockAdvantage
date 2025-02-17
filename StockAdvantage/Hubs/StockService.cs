using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using StockAdvantage.Components;

namespace StockAdvantage.Hubs;

public class StockService
{
    private readonly IHubContext<StockHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "5aa750b636msh71ccf3f2db0d164p1d3ce9jsn204d9855c397";

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

    public async Task<decimal> GetStockPriceAsync(string symbol)
    {
        try
        {
            var url = $"https://yahoo-finance15.p.rapidapi.com/api/v1/markets/stock/quotes?ticker=AAPL%2CMSFT%2C%5ESPX%2C%5ENYA%2CGAZP.ME%2CSIBN.ME%2CGEECEE.NS";
    
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-RapidAPI-Key", _apiKey);
            request.Headers.Add("X-RapidAPI-Host", "apidojo-yahoo-finance-v1.p.rapidapi.com");

            var response = await _httpClient.SendAsync(request);
            
            // hopefully we're getting here now
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Body: {responseContent}");

            response.EnsureSuccessStatusCode();

            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("price", out var priceElement) &&
                priceElement.TryGetProperty("regularMarketPrice", out var marketPriceElement) &&
                marketPriceElement.TryGetProperty("raw", out var rawPriceElement) &&
                rawPriceElement.TryGetDecimal(out var stockPrice))
            {
                Console.WriteLine("Here we are");
                return stockPrice;
            }

            throw new Exception("Stock price not available.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            throw new Exception("An error occurred while fetching the stock price.", ex);
        }
    }
}