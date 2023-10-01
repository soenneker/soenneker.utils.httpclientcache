using System.Net.Http;
using System.Threading.Tasks;

namespace Soenneker.Utils.HttpClientCache.Abstract;

/// <summary>
/// A utility library for singleton thread-safe HttpClients
/// </summary>
public interface IHttpClientCache
{
    ValueTask<HttpClient> GetClient(string id, bool cookieContainer = false, int maxConnectionsPerServer = 40);
}