using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NetTools;


namespace IPADG.Generators;

public sealed class LinodeListGenerator : IListGenerator
{
    private const string _url = "https://geoip.linode.com/";

    private static readonly HttpClient _client;

    static LinodeListGenerator()
    {
        _client = new HttpClient();
    }

    #region Methods

    public async Task<IReadOnlyCollection<string>> GeneratorAsync()
    {
        var ip4List = new List<string>();
        var ip6List = new List<string>();
        var list = await GetListAsync();

        foreach (var range in list)
        {
            if (range.Begin.AddressFamily == AddressFamily.InterNetworkV6)
            {
                ip6List.Add(range.ToCleanCidrString());
            }
            else
            {
                ip4List.Add(range.ToCleanCidrString());
            }
        }

        ip4List = ip4List.Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
        ip6List = ip6List.Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        return ip4List.Union(ip6List)
            .ToList();
    }

    private async Task<IReadOnlyCollection<IPAddressRange>> GetListAsync()
    {
        var results = new List<IPAddressRange>();
        var content = await DownloadAsync(_url);

        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<IPAddressRange>();
        }

        using var reader = new StringReader(content);

        while (true)
        {
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(",",  5);

            if (parts.Length < 5)
            {
                // Should we show an error?

                break;
            }

            if (IPAddressRange.TryParse(parts[0], out var range))
            {
                results.Add(range);
            }
        }

        return results;
    }

    private async Task<string> DownloadAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    #endregion
}

