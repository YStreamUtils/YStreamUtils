using Jint;
using Jint.Native;
using Jint.Native.Json;

namespace YStreamUtils.Core.Bridges;

public class FetchBridge : IBridge
{
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    });

    public void Register(Engine vm, JsObject hostObj)
    {
        var networkObj = new JsObject(vm);

        networkObj.Set("fetch", JsValue.FromObject(vm, new Func<string, JsValue, Task<JsValue>>(async (url, options) =>
        {
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

                    if (optObj.HasOwnProperty("body"))
                    {
                        var bodyValue = optObj.Get("body");
                        var stringContent = bodyValue.IsObject() 
                            ? bodyValue.ToString() 
                            : bodyValue.AsString();

                        request.Content = new StringContent(stringContent, System.Text.Encoding.UTF8, "application/json");
                    }

                    if (optObj.HasOwnProperty("headers") && optObj.Get("headers").IsObject())
                    {
                        var headersObj = optObj.Get("headers").AsObject();
                        foreach (var key in headersObj.GetOwnPropertyKeys())
                        {
                            var headerKey = key.AsString();
                            var headerVal = headersObj.Get(headerKey).AsString();
                            
                            if (request.Content != null && headerKey.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                            {
                                request.Content.Headers.TryAddWithoutValidation(headerKey, headerVal);
                            }
                            else
                            {
                                request.Headers.TryAddWithoutValidation(headerKey, headerVal);
                            }
                        }
                    }
                }

                using var response = await HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                var responseMirror = new JsObject(vm);
                responseMirror.Set("status", (int)response.StatusCode);
                responseMirror.Set("ok", response.IsSuccessStatusCode);
                
                responseMirror.Set("json", JsValue.FromObject(vm, new Func<JsValue>(() => 
                    new JsonParser(vm).Parse(content)
                )));
                    
                responseMirror.Set("text", JsValue.FromObject(vm, new Func<string>(() => content)));

                return responseMirror;
            }
            catch (Exception ex)
            {
                var errorMirror = new JsObject(vm);
                errorMirror.Set("status", 500);
                errorMirror.Set("ok", false);
                errorMirror.Set("error", ex.Message);
                return errorMirror;
            }
        })));

        hostObj.Set("network", networkObj);
    }
}
