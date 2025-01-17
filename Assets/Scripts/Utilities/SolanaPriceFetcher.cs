using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class SolanaPriceFetcher : MonoBehaviour
{
    private const string ApiUrl = "https://api.coingecko.com/api/v3/coins/markets";
    private const string Params = "?vs_currency=usd&order=market_cap_desc&per_page=10&page=1&sparkline=false";

    // Reference to the InfiniteScrollText script
    public InfiniteScrollText infiniteScrollText;

    // Class to represent a cryptocurrency in the response
    [System.Serializable]
    public class Crypto
    {
        public string name;
        public float current_price;
        public float price_change_percentage_24h;
    }

    // Dictionary for cryptocurrency symbol to name mapping (simplified version)
    private Dictionary<string, string> tickers = new Dictionary<string, string>
    {
        { "BTC", "Bitcoin" },
        { "ETH", "Ethereum" },
        { "SOL", "Solana" },
        { "PEPE", "PEPE" },
        // Add other symbols as needed...
    };

    void Start()
    {
        // Start the API request coroutine
        StartCoroutine(FetchTop10CryptoPrices());
    }

    IEnumerator FetchTop10CryptoPrices()
    {
        string url = ApiUrl + Params;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching data: " + webRequest.error);
                infiniteScrollText.UpdateText("Error fetching data. Please try again later.");
            }
            else
            {
                // Parse the JSON response
                string jsonResponse = webRequest.downloadHandler.text;
                List<Crypto> cryptocurrencies = JsonConvert.DeserializeObject<List<Crypto>>(jsonResponse);

                // Ensure infiniteScrollText is assigned
                if (infiniteScrollText == null)
                {
                    Debug.LogError("infiniteScrollText is not assigned.");
                    yield break; // Exit if the reference is missing
                }

                // Ensure cryptocurrencies data is fetched and not empty
                if (cryptocurrencies == null || cryptocurrencies.Count == 0)
                {
                    Debug.LogError("Failed to fetch or parse cryptocurrencies.");
                    infiniteScrollText.UpdateText("No data available.");
                    yield break; // Exit if no cryptocurrencies are available
                }

                // Build the formatted text for the top cryptocurrencies
                string displayText = "";

                foreach (var crypto in cryptocurrencies)
                {
                    // Look for a matching symbol for the cryptocurrency
                    foreach (var ticker in tickers)
                    {
                        if (ticker.Value == crypto.name) // Match the name
                        {
                            // Debug the raw percentage change value
                            //Debug.Log($"{crypto.name}: {crypto.price_change_percentage_24h}");

                            // Safely parse the percentage change
                            float percentageChange = crypto.price_change_percentage_24h;

                            // Determine the color based on the percentage change
                            string color = percentageChange >= 0 ? "#00FF00" : "#FF0000";
                            string change = percentageChange >= 0
                                ? $"+{percentageChange:F1}%"
                                : $"{percentageChange:F1}%";

                            // Build the ticker text
                            displayText += $"<color={color}>{crypto.name.ToUpper()} ({ticker.Key}): ${crypto.current_price:F2} ({change})</color> ";
                        }
                    }
                }

                // Update the scrolling ticker text
                infiniteScrollText.UpdateText(displayText);
            }
        }
    }
}
