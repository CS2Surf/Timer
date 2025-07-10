using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SurfTimer;

internal class ApiMethod
{
    private ApiMethod() { }

    private static readonly HttpClient _client = new();
    private static readonly string base_addr = Config.ApiUrl;

    /// <summary>
    /// Executes a GET request to the specified URL and deserializes the response to type T.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response into</typeparam>
    /// <param name="url">Relative URL to call</param>
    /// <returns>Deserialized T or null</returns>
    public static async Task<T?> GET<T>(string url, [CallerMemberName] string methodName = "")
    {
        var uri = new Uri(base_addr + url);
        var _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ApiMethod>>();

#if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> public static async Task<T?> GET -> BASE ADDR: {base_addr} | ENDPOINT: {url} | FULL: {uri.ToString()}");
#endif

        using var response = await _client.GetAsync(uri);

        try
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> GET {URL} => {StatusCode}",
                nameof(ApiMethod), methodName, url, response.StatusCode
            );

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning("[{ClassName}] {MethodName} -> No data found {StatusCode}",
                    nameof(ApiMethod), methodName, response.StatusCode
                );

                return default;
            }
            else if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Exception exception = new Exception($"[{nameof(ApiMethod)}] {methodName} -> Unexpected status code {response.StatusCode}");
                throw exception;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ClassName}] {MethodName} -> HTTP Response was invalid or could not be deserialised.", nameof(ApiMethod), methodName);
            return default;
        }
    }

    /// <summary>
    /// Executes a POST request to the specified URL with the given body and returns the response.
    /// </summary>
    /// <typeparam name="T">Type of the request body</typeparam>
    /// <param name="url">Relative URL to call</param>
    /// <param name="body">Request body to send</param>
    /// <returns>API_PostResponseData or null</returns>
    public static async Task<API_PostResponseData?> POST<T>(string url, T body, [CallerMemberName] string methodName = "")
    {
        var uri = new Uri(base_addr + url);
        var _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ApiMethod>>();

        try
        {
            using var response = await _client.PostAsJsonAsync(uri, body);

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> POST {URL} => {StatusCode}",
                nameof(ApiMethod), methodName, url, response.StatusCode
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<API_PostResponseData>();
            }
            else
            {
                // Read response body to log what went wrong
                var errorContent = await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "[{ClassName}] {MethodName} -> POST {URL} failed with status {StatusCode}. Response body: {ResponseBody}",
                    nameof(ApiMethod), methodName, url, response.StatusCode, errorContent
                );

                return default;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{ClassName}] {MethodName} -> Exception during POST {URL}",
                nameof(ApiMethod), methodName, url
            );

            return default;
        }
    }

    /// <summary>
    /// Executes a PUT request to the specified URL with the given body and returns the response.
    /// </summary>
    /// <typeparam name="T">Type of the request body</typeparam>
    /// <param name="url">Relative URL to call</param>
    /// <param name="body">Request body to send</param>
    /// <returns>API_PostResponseData or null</returns>
    public static async Task<API_PostResponseData?> PUT<T>(string url, T body, [CallerMemberName] string methodName = "")
    {
        var uri = new Uri(base_addr + url);
        var _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ApiMethod>>();

        try
        {
            using var response = await _client.PutAsJsonAsync(uri, body);

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> PUT {URL} => {StatusCode}",
                nameof(ApiMethod), methodName, url, response.StatusCode
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<API_PostResponseData>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "[{ClassName}] {MethodName} -> PUT {URL} failed with status {StatusCode}. Response body: {ResponseBody}",
                    nameof(ApiMethod), methodName, url, response.StatusCode, errorContent
                );

                return default;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{ClassName}] {MethodName} -> Exception during PUT {URL}",
                nameof(ApiMethod), methodName, url
            );

            return default;
        }
    }

}