# NGINX Allow/Deny Generator

A simple tool to import IP ranges from AWS, Azure, Cloudflare, DigitalOcean and GCP and 
output them to a text file for including directly in NGINX config.

```
nginx-iadg [options]

Usage
      --aws                  Include AWS IP ranges
      --azure                Include Azure IP ranges
      --cloudflare           Include Cloudflare IP ranges
      --do                   Include DigitalOcean IP ranges
      --gcp                  Include GCP IP ranges
  -o, --output-file=VALUE    File to output to
  -t, --type=VALUE           Type, use allow or deny (default is allow)
  -p, --plain                Whether the output should be plain or not (no type)
  -n, --no-comment           Do not comment each range block (default is on)
      --comment-format=VALUE The format used to denote a comment. Use $value$
                               as a placeholder for the value otherwise it will
                               append. Default is "# "
  -s, --silent               Don't show any output
  -h, --help                 Show this usage information
```

## Basic Example

To import only Cloudflare ranges for explicit allow:

`nginx-ipadg --type=allow --cloudflare -o ./cloudflare-ips.conf`

This would produce (at time of writing) a file called _cloudflare-ips.conf_ with the following content:

```
# Cloudflare

allow 103.21.244.0/22;
allow 103.22.200.0/22;
allow 103.31.4.0/22;
allow 104.16.0.0/13;
allow 104.24.0.0/14;
allow 108.162.192.0/18;
allow 131.0.72.0/22;
allow 141.101.64.0/18;
allow 162.158.0.0/15;
allow 172.64.0.0/13;
allow 173.245.48.0/20;
allow 188.114.96.0/20;
allow 190.93.240.0/20;
allow 197.234.240.0/22;
allow 198.41.128.0/17;
allow 2400:cb00::/32;
allow 2405:8100::/32;
allow 2405:b500::/32;
allow 2606:4700::/32;
allow 2803:f800::/32;
allow 2a06:98c0::/29;
allow 2c0f:f248::/32;
```

In NGINX you can then simply include the file, for example:

```
allow 127.0.0.1;
include ./cloudflare-ips.conf;
deny all;
```

## Non-NGINX

You can use the `--plain` flag which will simply output the IP address with no NGINX specific formatting.

## License

Copyright (c) Lloyd Kinsella

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.