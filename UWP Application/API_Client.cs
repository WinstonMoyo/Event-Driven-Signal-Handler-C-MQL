using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Telegram_Signal_Copier___Demo
{
    /// <summary>
    /// The ApiClient class is responsible for sending HTTP requests to an external API.
    /// This is used to send parsed trade signals from the application to a specified API endpoint.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient httpClient; // Instance of HttpClient used to send HTTP requests
        private readonly string apiUrl; // The API endpoint URL to which the requests will be sent

        /// <summary>
        /// Constructor for the ApiClient class.
        /// Initializes the HttpClient and sets the API URL.
        /// </summary>
        /// <param name="apiUrl">The API URL to which the trade signals will be sent.</param>
        public ApiClient(string apiUrl)
        {
            this.apiUrl = apiUrl; // Store the API URL
            this.httpClient = new HttpClient(); // Initialize a new instance of HttpClient
        }

        /// <summary>
        /// Sends a trade signal (in JSON format) to the specified API endpoint using an HTTP POST request.
        /// </summary>
        /// <param name="tradeSignal">A JObject containing the trade signal data (operation, symbol, prices, etc.).</param>
        /// <returns>A string containing the response from the API or an error message if the request fails.</returns>
        public async Task<string> SendTextToApiAsync(JObject tradeSignal)
        {
            try
            {
                // Convert the JObject (trade signal) to a JSON-formatted string
                var content = new StringContent(tradeSignal.ToString(), Encoding.UTF8, "application/json");

                // Send the POST request to the API endpoint with the trade signal as the payload
                var response = await httpClient.PostAsync(apiUrl, content);

                // Ensure the response indicates success (2xx HTTP status codes)
                response.EnsureSuccessStatusCode();

                // Output the HTTP status code for logging/debugging purposes
                Console.WriteLine(response.StatusCode);

                // Return the API response content as a string
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // If an exception occurs, return the exception message for debugging
                return ex.Message;
            }
        }
    }
}
