using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using NetTools;

namespace IPADG.Generators;

public sealed class CloudflareListGenerator : IListGenerator
{
    private const string _ip4Url = "https://www.cloudflare.com/ips-v4/";
    private const string _ip6Url = "https://www.cloudflare.com/ips-v6/";

    private static readonly HttpClient _client;

    static CloudflareListGenerator()
    {
        _client = new HttpClient();
    }

    #region Methods

    public async Task<IReadOnlyCollection<string>> GeneratorAsync()
    {
        var ip4List = await GetListAsync(_ip4Url);
        var ip6List = await GetListAsync(_ip6Url);

        return ip4List.Union(ip6List)
            .ToList();
    }

    private async Task<IReadOnlyCollection<string>> GetListAsync(string url)
    {
        var results = new List<string>();
        var content = await DownloadAsync(url);
        using var reader = new StringReader(content);

        while (true)
        {
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IPAddressRange.TryParse(line.Trim(), out var range))
            {
                results.Add(range.ToCleanCidrString());
            }
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
    }

    private async Task<string> DownloadAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    #endregion
}

