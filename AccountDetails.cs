using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MetalPriceConsole.Models;

namespace MetalPriceConsole
{
    public class AccountDetails
    {
        public static async Task<Account> GetDetailsAsync(ApiServer apiServer, string url)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Add("x-access-token", apiServer.Token);
            using (HttpRequestMessage request = new(HttpMethod.Get, url))
            {
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStreamAsync();
                Account account = await JsonSerializer.DeserializeAsync<Account>(result);
                return account;
            }
        }
    }
}
