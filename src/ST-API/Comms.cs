using System.Net.Http.Json;

namespace SurfTimer;

internal class ApiMethod
{
    private ApiMethod() { }

    private static readonly HttpClient _client = new();
    private static readonly string base_addr = Config.ApiUrl;

    public static async Task<T?> GET<T>(string url)
    {
        var uri = new Uri(base_addr + url);

#if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> public static async Task<T?> GET -> BASE ADDR: {base_addr} | ENDPOINT: {url} | FULL: {uri.ToString()}");
#endif

        using var response = await _client.GetAsync(uri);

        try
        {
            System.Console.WriteLine($"[API] GET {url} => {response.StatusCode}");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Exception exception = new Exception("[API] GET - No data found");
                throw exception;
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
        var uri = new Uri(base_addr + url);

#if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> public static async Task<API_PostResponseData?> POST -> BASE ADDR: {base_addr} | ENDPOINT: {url} | FULL: {uri.ToString()}");
#endif

        using var response = await _client.PostAsJsonAsync(uri, body);

        try
        {
            System.Console.WriteLine($"[API] POST {url} => {response.StatusCode}");
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
        var uri = new Uri(base_addr + url);

#if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> public static async Task<API_PostResponseData?> PUT -> BASE ADDR: {base_addr} | ENDPOINT: {url} | FULL: {uri.ToString()}");
#endif

        using var response = await _client.PutAsJsonAsync(uri, body);

        try
        {
            System.Console.WriteLine($"[API] PUT {url} => {response.StatusCode}");
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