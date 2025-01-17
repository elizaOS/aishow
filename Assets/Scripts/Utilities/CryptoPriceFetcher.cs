using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class CryptoPriceFetcher : MonoBehaviour
{
    // Updated API URL to fetch specific cryptocurrencies: Bitcoin, Ethereum, Solana, and Polygon
    private const string ApiUrl = "https://api.coingecko.com/api/v3/coins/markets";
    private const string Params = "?vs_currency=usd&ids=bitcoin,ethereum,solana,matic-network&order=market_cap_desc&per_page=4&page=1&sparkline=false";

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

    // List to store the parsed cryptocurrencies
    private List<Crypto> cryptocurrencies;

    void Start()
    {
        // Start the API request coroutine
        StartCoroutine(FetchCryptoPrices());
    }

    IEnumerator FetchCryptoPrices()
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
                cryptocurrencies = JsonConvert.DeserializeObject<List<Crypto>>(jsonResponse);

                // Build the formatted text
                string displayText = "";

                foreach (var crypto in cryptocurrencies)
                {
                    // Debug the raw percentage change value
                    Debug.Log($"{crypto.name}: {crypto.price_change_percentage_24h}");

                    // Safely parse the percentage change
                    float percentageChange = crypto.price_change_percentage_24h;

                    // Determine the color based on the percentage change
                    string color = percentageChange >= 0 ? "#00FF00" : "#FF0000";
                    string change = percentageChange >= 0
                        ? $"+{percentageChange:F2}%" // Increased precision for percentage change
                        : $"{percentageChange:F2}%";  // Increased precision for percentage change

                    // Build the ticker text with more decimal places
                    displayText += $"<color={color}>{crypto.name.ToUpper()}: ${crypto.current_price:F4} ({change})</color> "; // Shows 4 decimal places for price
                }

                // Update the scrolling ticker text
                infiniteScrollText.UpdateText(displayText);
            }
        }
    }
}
