using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Mono.Options;

using IPADG.Generators;

namespace IPADG;

public static class Program
{
    private static readonly OptionSet _options;

    private static bool _cloudflare;
    private static bool _aws;
    private static bool _azure;
    private static bool _gcp;
    private static bool _do;
    private static string? _outputFile;
    private static string? _type;
    private static bool _plain;
    private static bool _noComment;
    private static string? _commentCharacters;
    private static bool _silent;
    private static bool _help;
    private static int _returnCode;
    
    static Program()
    {
        _options = new OptionSet
        {
            { "aws", "Include AWS IP ranges", v => _aws = v != null },
            { "azure", "Include Azure IP ranges", v => _azure = v != null },
            { "cloudflare", "Include Cloudflare IP ranges", v => _cloudflare = v != null },
            { "do", "Include DigitalOcean IP ranges", v => _do = v != null },
            { "gcp", "Include GCP IP ranges", v => _gcp = v != null },
            { "o|output-file=", "File to output to", v => _outputFile = v },
            { "t|type=", "Type, use allow or deny (default is allow)", v => _type = v },
            { "p|plain", "Whether the output should be plain or not (no type)", v => _plain = v != null },
            { "n|no-comment", "Do not comment each range block (default is on)", v => _noComment = v != null },
            { "comment-format=", "The format used to denote a comment. Use $value$ as a placeholder for the value otherwise it will append. Default is \"# \"", v => _commentCharacters = v },
            { "s|silent", "Don't show any output", v => _silent = v != null },
            { "h|help", "Show this usage information", v => _help = v != null }
        };

        _cloudflare = false;
        _aws = false;
        _azure = false;
        _gcp = false;
        _do = false;
        _outputFile = null;
        _type = null;
        _plain = false;
        _noComment = false;
        _commentCharacters = null;
        _silent = false;
        _help = false;
    }

    public static async Task<int> Main(string[] args)
    {
        WriteLine("NGINX IP Allow/Deny Generator, v1.0");
        WriteLine("Copyright (c)2023 Lloyd Kinsella.");
        WriteLine();

        try
        {
            if (args.Length == 0)
            {
                Usage();

                return ++_returnCode;
            }

            _ = _options.Parse(args);

            if (_help)
            {
                Usage();

                return 0;
            }

            if (string.IsNullOrWhiteSpace(_outputFile))
            {
                WriteLine("Error: No output file specified.");
                WriteLine();

                return ++_returnCode;
            }

            if (string.IsNullOrWhiteSpace(_type))
            {
                _type = "allow";
            }

            if (_type.ToLower() != "allow" && _type.ToLower() != "deny")
            {
                WriteLine($"Error: Unknown type - {_type}");
                WriteLine();

                return ++_returnCode;
            }

            var generators = new List<IListGenerator>();

            if (_cloudflare)
            {
                generators.Add(new CloudflareListGenerator());
            }

            var file = new FileStream(_outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var writer = new StreamWriter(file, Encoding.ASCII);

            if (_aws)
            {
                WriteLine("Getting IP ranges from AWS...");

                var generator = new AWSListGenerator();
                var list = await generator.GeneratorAsync();

                if (list.Count > 0)
                {
                    await WriteCommentAsync(writer, "AWS");

                    foreach (var line in list)
                    {
                        await WriteAddressAsync(writer, line);
                    }

                    await writer.WriteLineAsync();
                }

                WriteLine($"AWS completed, {list.Count:#,##} IP ranges.");
                WriteLine();
            }

            if (_azure)
            {
                WriteLine("Getting IP ranges from Azure...");

                var generator = new AzureListGenerator();
                var list = await generator.GeneratorAsync();

                if (list.Count > 0)
                {
                    await WriteCommentAsync(writer, "Azure");

                    foreach (var line in list)
                    {
                        await WriteAddressAsync(writer, line);
                    }

                    await writer.WriteLineAsync();
                }

                WriteLine($"Azure completed, {list.Count:#,##} IP ranges.");
                WriteLine();
            }

            if (_cloudflare)
            {
                WriteLine("Getting IP ranges from Cloudflare...");

                var generator = new CloudflareListGenerator();
                var list = await generator.GeneratorAsync();

                if (list.Count > 0)
                {
                    await WriteCommentAsync(writer, "Cloudflare");

                    foreach (var line in list)
                    {
                        await WriteAddressAsync(writer, line);
                    }

                    await writer.WriteLineAsync();
                }

                WriteLine($"Cloudflare completed, {list.Count:#,##} IP ranges.");
                WriteLine();
            }

            if (_do)
            {
                WriteLine("Getting IP ranges from DigitalOcean...");

                var generator = new DigitalOceanListGenerator();
                var list = await generator.GeneratorAsync();

                if (list.Count > 0)
                {
                    await WriteCommentAsync(writer, "DigitalOcean");

                    foreach (var line in list)
                    {
                        await WriteAddressAsync(writer, line);
                    }

                    await writer.WriteLineAsync();
                }

                WriteLine($"DigitalOcean completed, {list.Count:#,##} IP ranges.");
                WriteLine();
            }

            if (_gcp)
            {
                WriteLine("Getting IP ranges from GCP...");

                var generator = new GCPListGenerator();
                var list = await generator.GeneratorAsync();

                if (list.Count > 0)
                {
                    await WriteCommentAsync(writer, "GCP");

                    foreach (var line in list)
                    {
                        await WriteAddressAsync(writer, line);
                    }

                    await writer.WriteLineAsync();
                }

                WriteLine($"GCP completed, {list.Count:#,##} IP ranges.");
                WriteLine();
            }

            await writer.FlushAsync();
            await file.FlushAsync();

            return 0;
        }
        catch (Exception ex)
        {
            WriteLine($"Error: {ex}");
            WriteLine();

            return 666;
        }
    }

    private static void Usage()
    {
        Console.WriteLine("nginx-iadg [options]");
        Console.WriteLine();
        Console.WriteLine("Usage");

        var writer = new StringWriter();

        _options.WriteOptionDescriptions(writer);

        Console.WriteLine(writer.GetStringBuilder().ToString());
    }

    private static async Task WriteCommentAsync(TextWriter writer, string value)
    {
        if (_noComment)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_commentCharacters))
        {
            _commentCharacters = "# ";
        }

        if (_commentCharacters.IndexOf("$value$", StringComparison.OrdinalIgnoreCase) > -1)
        {
            await writer.WriteLineAsync(_commentCharacters.Replace("$value$", value));
        }
        else
        {
            await writer.WriteLineAsync($"{_commentCharacters}{value}");
        }

        await writer.WriteLineAsync();
    }

    private static async Task WriteAddressAsync(TextWriter writer, string address)
    {
        if (_plain)
        {
            await writer.WriteLineAsync(address);

            return;
        }

        if (_type == "deny")
        {
            await writer.WriteLineAsync($"deny {address};");

            return;
        }

        await writer.WriteLineAsync($"allow {address};");
    }

    private static void WriteLine(string? line = null)
    {
        if (_silent)
        {
            return;
        }

        if (line is null)
        {
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine(line);
        }
    }
}
