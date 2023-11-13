using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DnsClient;
using DnsClient.Protocol;
using NetTools;

namespace IPADG.Generators;

public sealed class GCPListGenerator : IListGenerator
{
    private readonly ILookupClient _client;

    public GCPListGenerator()
    {
        var options = new LookupClientOptions(IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4"))
        {
            UseCache = false
        };

        _client = new LookupClient(options);
    }

    #region Methods

    public async Task<IReadOnlyCollection<string>> GeneratorAsync()
    {
        var ip4List = new List<string>();
        var ip6List = new List<string>();
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addresses = new List<IPAddressRange>();

        await QueryAddressesAsync(domains, addresses, "_cloud-netblocks.googleusercontent.com");

        foreach (var range in addresses)
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

    private async Task QueryAddressesAsync(ISet<string> domains, ICollection<IPAddressRange> addresses, string lookup)
    {
        if (domains.Contains(lookup))
        {
            return;
        }

        domains.Add(lookup);

        var blocks = await _client.QueryAsync(lookup, QueryType.TXT);

        if (blocks == null || blocks.Answers is null || blocks.Answers.Count == 0)
        {
            return;
        }

        foreach (var answer in blocks.Answers)
        {
            if (answer is not TxtRecord)
            {
                continue;
            }

            var txtAnswer = (TxtRecord)answer;

            foreach (var text in txtAnswer.Text)
            {
                if (!text.StartsWith("v=spf1"))
                {
                    continue;
                }

                var includeMatches = Regex.Matches(text, @"include\:(.*?)\s+", RegexOptions.IgnoreCase);
                var ip4Matches = Regex.Matches(text, @"ip4\:(.*?)\s+", RegexOptions.IgnoreCase);
                var ip6Matches = Regex.Matches(text, @"ip6\:(.*?)\s+", RegexOptions.IgnoreCase);

                foreach (Match match in includeMatches)
                {
                    await QueryAddressesAsync(domains, addresses, match.Groups[1].Value);
                }

                foreach (Match match in ip4Matches)
                {
                    if (IPAddressRange.TryParse(match.Groups[1].Value.ToLower(), out var address))
                    {
                        addresses.Add(address);
                    }
                }

                foreach (Match match in ip6Matches)
                {
                    if (IPAddressRange.TryParse(match.Groups[1].Value.ToLower(), out var address))
                    {
                        addresses.Add(address);
                    }
                }
            }
        }
    }

    #endregion
}

