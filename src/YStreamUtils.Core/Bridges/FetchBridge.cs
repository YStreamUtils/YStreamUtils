using Jint;
using Jint.Native;
using Reinforced.Typings.Attributes;

namespace YStreamUtils.Core.Bridges;

/// <summary>
/// Represents the structured network response passed back to the JavaScript context.
/// </summary>
[TsInterface(IncludeNamespace = false, Name = "FetchResponse")]
public interface IFetchResponse
{
    int Status { get; }
    bool Ok { get; }
    string Body { get; }
}

public class FetchResponse : IFetchResponse
{
    public int Status { get; set; }
    public bool Ok { get; set; }
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Bridge class exposed to the Jint runtime to allow scripts to execute network requests.
/// </summary>
[TsInterface(AutoI = false, Name = "NetworkBridge", IncludeNamespace = false)]
public class FetchBridge : IBridge
{
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    });

    private Engine? _vm;

    public void Register(Engine vm, JsObject hostObj)
    {
        _vm = vm;

        hostObj.Set("network", JsValue.FromObject(vm, this));
    }

    /// <summary>
    /// Fetches data from a given URL asynchronously.
    /// </summary>
    /// <returns>A promise resolving to the network response object.</returns>
    [TsFunction(Type = "Promise<FetchResponse>")]
    public async Task<IFetchResponse> Fetch(string url, JsValue options)
    {
        if (_vm == null)
            throw new InvalidOperationException("Jint Engine is not registered.");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (options.IsObject())
            {
                var optObj = options.AsObject();

                if (optObj.HasOwnProperty("method"))
                {
                    var methodStr = optObj.Get("method").AsString().ToUpperInvariant();
                    request.Method = new HttpMethod(methodStr);
                }

                if (optObj.HasOwnProperty("headers"))
                {
                    var headersValue = optObj.Get("headers");
                    if (headersValue.IsObject())
                    {
                        var headersObj = headersValue.AsObject();
                        foreach (var key in headersObj.GetOwnPropertyKeys())
                        {
                            var headerName = key.AsString();
                            var headerValue = headersObj.Get(headerName).AsString();

                            request.Headers.TryAddWithoutValidation(headerName, headerValue);
                        }
                    }
                }

                if (optObj.HasOwnProperty("body"))
                {
                    var bodyValue = optObj.Get("body");
                    var stringContent = bodyValue.IsObject()
                        ? bodyValue.ToString()
                        : bodyValue.AsString();

                    request.Content = new StringContent(stringContent, System.Text.Encoding.UTF8, "application/json");

                    if (optObj.HasOwnProperty("headers") && optObj.Get("headers").IsObject())
                    {
                        var headersObj = optObj.Get("headers").AsObject();
                        if (headersObj.HasOwnProperty("content-type"))
                        {
                            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(headersObj.Get("content-type").AsString());
                        }
                    }
                }
            }

            var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return new FetchResponse
            {
                Status = (int)response.StatusCode,
                Ok = response.IsSuccessStatusCode,
                Body = responseBody
            };
        }
        catch (Exception ex)
        {
            return new FetchResponse
            {
                Status = 500,
                Ok = false,
                Body = ex.Message
            };
        }
    }
}
