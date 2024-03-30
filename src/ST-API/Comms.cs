using System.Net.Http.Json;

namespace SurfTimer;

internal class APICall
{
    private APICall()
    {

    }

    private static readonly HttpClient _client = new HttpClient();

    public static async Task<T?> GET<T>(string url)
    {
        var uri = new Uri(url);

        using var response = await _client.GetAsync(uri);

        try
        {
            System.Console.WriteLine($"[API] URL: {url} => {response.StatusCode}");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("No data found");
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            Console.WriteLine("HTTP Response was invalid or could not be deserialised.");
            return default;
        }
    }

    public static async Task<API_PostResponseData?> POST<T>(string url, T body)
    {
        var uri = new Uri(url);

        using var response = await _client.PostAsJsonAsync(uri, body);

        try
        {
            System.Console.WriteLine($"[API] URL: {url} => {response.StatusCode}");
            response.EnsureSuccessStatusCode(); // BAD BAD BAD
            return await response.Content.ReadFromJsonAsync<API_PostResponseData>();
        }
        catch
        {
            Console.WriteLine("HTTP Response was invalid or could not be deserialised.");
            return default;
        }
    }

    public static async Task<API_PostResponseData?> PUT<T>(string url, T body)
    {
        var uri = new Uri(url);

        using var response = await _client.PutAsJsonAsync(uri, body);

        try
        {
            System.Console.WriteLine($"[API] URL: {url} => {response.StatusCode}");
            response.EnsureSuccessStatusCode(); // BAD BAD BAD
            return await response.Content.ReadFromJsonAsync<API_PostResponseData>();
        }
        catch
        {
            Console.WriteLine("HTTP Response was invalid or could not be deserialised.");
            return default;
        }
    }
}