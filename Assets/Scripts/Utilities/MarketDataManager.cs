using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class MarketDataManager : MonoBehaviour
{
    [SerializeField] private InfiniteScrollText scrollText;
    [SerializeField] private string separator = "   ●   "; // Separator between tickers

    [Serializable]
    private class MarketSymbol
    {
        public string name; // Coin name (e.g., BTC)
        public string geckoId; // CoinGecko ID (e.g., bitcoin)
        public float currentPrice; // Current price
        public float priceChange24h; // 24-hour percentage change

        public string FormatTickerText()
        {
            string changeSymbol = priceChange24h >= 0 ? "+" : ""; // Add "+" for positive change
            string priceFormat = currentPrice < 1 ? "F4" : "F2"; // Format for prices below $1
            string color = priceChange24h >= 0 ? "#00FF00" : "#FF0000"; // Green for positive, red for negative
            return $"<color=#FFFFFF>{name}:</color> <color=#FFFFFF>${currentPrice.ToString(priceFormat)}</color> <color={color}>({changeSymbol}{priceChange24h:F2}%)</color>";
        }
    }

    [SerializeField] private List<MarketSymbol> symbols = new List<MarketSymbol>
    {
        new MarketSymbol { name = "BTC", geckoId = "bitcoin" },
        new MarketSymbol { name = "ETH", geckoId = "ethereum" },
        new MarketSymbol { name = "SOL", geckoId = "solana" },
        new MarketSymbol { name = "DOGE", geckoId = "dogecoin" }
    };

    private void Start()
    {
        if (scrollText == null)
        {
            scrollText = GetComponent<InfiniteScrollText>();
        }

        if (scrollText == null)
        {
            Debug.LogError("InfiniteScrollText is not assigned or found in the GameObject.");
            enabled = false;
            return;
        }

        StartCoroutine(FetchAndDisplayData());
    }

    private IEnumerator FetchAndDisplayData()
    {
        string coinIds = string.Join(",", symbols.ConvertAll(s => s.geckoId));
        string url = $"https://api.coingecko.com/api/v3/simple/price?ids={coinIds}&vs_currencies=usd&include_24hr_change=true";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"Received JSON: {jsonResponse}");

                    var priceData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float>>>(jsonResponse);

                    foreach (var symbol in symbols)
                    {
                        if (priceData.ContainsKey(symbol.geckoId))
                        {
                            var coinData = priceData[symbol.geckoId];
                            if (coinData.ContainsKey("usd"))
                                symbol.currentPrice = coinData["usd"];
                            if (coinData.ContainsKey("usd_24h_change"))
                                symbol.priceChange24h = coinData["usd_24h_change"];
                        }
                        else
                        {
                            Debug.LogWarning($"No data found for {symbol.geckoId}");
                        }
                    }

                    UpdateTickerText();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing CoinGecko response: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to fetch prices: {request.error}");
            }
        }
    }

    private void UpdateTickerText()
    {
        if (symbols.Count == 0)
        {
            Debug.LogWarning("No symbols available to display.");
            return;
        }

        string tickerContent = "";

        for (int i = 0; i < symbols.Count; i++)
        {
            tickerContent += symbols[i].FormatTickerText();
            if (i < symbols.Count - 1)
            {
                tickerContent += separator;
            }
        }

        scrollText.UpdateText(tickerContent);
    }

    public void AddCoin(string name, string geckoId)
    {
        symbols.Add(new MarketSymbol { name = name, geckoId = geckoId });
    }
}
