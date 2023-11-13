using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NetTools;

namespace IPADG;

internal static class IPAddressRangeExtensions
{
    #region Methods

    public static string ToCleanCidrString(this IPAddressRange range)
    {
        var value = range.ToCidrString();
        string cidr;

        if (range.Begin.AddressFamily == AddressFamily.InterNetworkV6)
        {
            cidr = "/128";
        }
        else
        {
            cidr = "/32";
        }

        if (value.EndsWith(cidr, StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(0, value.Length - cidr.Length);
        }

        return value;
    }

    #endregion
}
