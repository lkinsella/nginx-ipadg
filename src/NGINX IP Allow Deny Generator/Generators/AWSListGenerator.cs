using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IPADG.Generators;

public abstract record AWSPrefix
{
    [JsonPropertyName("region")]
    public string Region { get; set; } = null!;

    [JsonPropertyName("network_border_group")]
    public string NetworkBorderGroup { get; set; } = null!;

    [JsonPropertyName("service")]
    public string Service { get; set; } = null!;
}

public sealed record AWSPrefix4 : AWSPrefix
{
    [JsonPropertyName("ip_prefix")]
    public string Prefix { get; set; } = null!;
}

public sealed record AWSPrefix6 : AWSPrefix
{
    [JsonPropertyName("ipv6_prefix")]
    public string Prefix { get; set; } = null!;
}

public sealed record AWSDataSet
{
    [JsonPropertyName("syncToken")]
    public string SyncToken { get; set; } = null!;

    [JsonPropertyName("createDate")]
    public string CreateDate { get; set; } = null!;

    [JsonPropertyName("prefixes")]
    public AWSPrefix4[] IPv4Prefixes { get; set; } = null!;

    [JsonPropertyName("ipv6_prefixes")]
    public AWSPrefix6[] IPv6Prefixes { get; set; } = null!;
}

[JsonSerializable(typeof(AWSDataSet))]
public partial class AWSJsonContext : JsonSerializerContext
{
}

public sealed class AWSListGenerator : IListGenerator
{
    private const string _url = "https://ip-ranges.amazonaws.com/ip-ranges.json";

    private static readonly HttpClient _client;

    static AWSListGenerator()
    {
        _client = new HttpClient();
    }

    #region Methods

    public async Task<IReadOnlyCollection<string>> GeneratorAsync()
    {
        var ip4List = await GetListAsync(false);
        var ip6List = await GetListAsync(true);

        return ip4List.Union(ip6List)
            .ToList();
    }

    private async Task<IReadOnlyCollection<string>> GetListAsync(bool v6)
    {
        var results = new List<string>();
        var json = await DownloadAsync(_url);
        var dataSet = JsonSerializer.Deserialize<AWSDataSet>(json, AWSJsonContext.Default.AWSDataSet);

        if (!v6)
        {
            foreach (var prefix in dataSet.IPv4Prefixes)
            {
                if (prefix.Prefix.EndsWith("/32"))
                {
                    results.Add(prefix.Prefix.Substring(0, prefix.Prefix.Length - 3));
                }
                else
                {
                    results.Add(prefix.Prefix);
                }
            }
        }
        else
        {
            foreach (var prefix in dataSet.IPv6Prefixes)
            {
                if (prefix.Prefix.EndsWith("/128"))
                {
                    results.Add(prefix.Prefix.Substring(0, prefix.Prefix.Length - 3));
                }
                else
                {
                    results.Add(prefix.Prefix);
                }
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

