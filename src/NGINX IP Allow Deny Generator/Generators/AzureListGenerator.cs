using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NetTools;

namespace IPADG.Generators;

public sealed record ServiceTagDataSet
{
    [JsonPropertyName("changeNumber")]
    public int ChangeNumber { get; set; }

    [JsonPropertyName("cloud")]
    public string Cloud { get; set; } = null!;

    [JsonPropertyName("values")]
    public ServiceTagValue[] Values { get; set; } = null!;
}

public sealed record ServiceTagValue
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("properties")]
    public ServiceTagValueProperties Properties { get; set; } = null!;
}

public sealed record ServiceTagValueProperties
{
    [JsonPropertyName("changeNumber")]
    public int ChangeNumber { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; } = null!;

    [JsonPropertyName("regionId")]
    public int RegionId { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = null!;

    [JsonPropertyName("systemService")]
    public string SystemService { get; set; } = null!;

    [JsonPropertyName("addressPrefixes")]
    public string[] AddressPrefixes { get; set; } = null!;

    [JsonPropertyName("networkFeatures")]
    public string[] NetworkFeatures { get; set; } = null!;
}

[JsonSerializable(typeof(ServiceTagDataSet))]
public partial class AzureJsonContext : JsonSerializerContext
{
}

public sealed class AzureListGenerator : IListGenerator
{
    //private const string _url = "https://download.microsoft.com/download/7/1/D/71D86715-5596-4529-9B13-DA13A5DE5B63/ServiceTags_Public_20231106.json";
    private const string _url = "https://azureipranges.azurewebsites.net/Data/Public.json";

    private static readonly HttpClient _client;

    static AzureListGenerator()
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
        var json = await DownloadAsync(_url);
        var dataSet = JsonSerializer.Deserialize<ServiceTagDataSet>(json, AzureJsonContext.Default.ServiceTagDataSet);

        foreach (var value in dataSet.Values)
        {
            if (value.Properties?.AddressPrefixes is null || value.Properties.AddressPrefixes.Length == 0)
            {
                continue;
            }

            foreach (var address in value.Properties.AddressPrefixes)
            {
                IPAddressRange range;

                if (!IPAddressRange.TryParse(address, out range))
                {
                    continue;
                }

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

