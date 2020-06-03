using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    internal class Program
    {
        private static string _fileUrl = "http://bms.bemaniso.ws/files/iidx/";
        private static char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        private static IEnumerable<string> FileNames(string pageContent)
        {
            var reg = @"<a[^>]*href=([""'])?(?<href>[^""]+)\1[^>]*>";
            return Regex.Matches(pageContent, reg, RegexOptions.IgnoreCase)
                        .Where(r => r.Value.Contains("download"))
                        .Where(r => r.Value.Contains("file=all") == false)
                        .Select(r =>
                        {
                            var href = r.Groups[2].Value.Replace("amp;", "");
                            return href.Split('&', '=').Last();
                        });
        }

        private static async Task Main(string[] args)
        {
            try
            {
                for(var style = 18; style < 21; style++)
                {
                    await StyleFiles(style);
                }

                Console.WriteLine("done");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static async Task<string> PageContent(string url)
        {
            var httpClient = new HttpClient();
            var message = await httpClient.GetAsync(url);
            return await message.Content.ReadAsStringAsync();
        }

        private static async Task SaveFile(HttpResponseMessage message, string path)
        {
            await using var fs = new FileStream(path, FileMode.CreateNew);
            await message.Content.CopyToAsync(fs);
        }

        private static async Task StyleFiles(int style)
        {
            Console.WriteLine($"begin get style={style} page");
            var pageContent = await PageContent($"http://bms.bemaniso.ws/files.php?group=iidx&style={style}");
            Console.WriteLine($"end get style={style} page");

            var matches = FileNames(pageContent);

            var httpClient = new HttpClient();
            foreach (var name in matches)
            {
                var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var path = $"{directoryName}/DL/{style}/";
                var message = await httpClient.GetAsync($"{_fileUrl}/{style}/{name}");
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }

                var htmlDecode = Uri.UnescapeDataString(name);
                foreach (var nameChar in _invalidFileNameChars)
                {
                    htmlDecode = htmlDecode.Replace(nameChar, '_');
                }

                Console.WriteLine($"save file {htmlDecode}");
                await SaveFile(message, $"{path}{htmlDecode}");
            }
        }
    }
}