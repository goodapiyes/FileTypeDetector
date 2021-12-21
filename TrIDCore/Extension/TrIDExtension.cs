using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using SevenZipExtractor;

namespace TrIDCore;

public class TrIDExtension
{
    private static readonly string upgradeUrl = "https://mark0.net/download/triddefs_xml.7z";
    private static readonly string definitionsFileName = "trid.db";
    public static void UpgradeDefinitions(string defsFullPath = null)
    {
        //auto upgrade
        if (string.IsNullOrEmpty(defsFullPath))
        {
            var web = new WebClient();
            var fileName = Path.GetFileName(upgradeUrl);
            web.DownloadFile(upgradeUrl, fileName);
            defsFullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        }

        if (File.Exists(defsFullPath))
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(defsFullPath) ?? "\\", "output");
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            using (ArchiveFile archiveFile = new ArchiveFile(defsFullPath))
            {
                archiveFile.Extract("output");
            }

            defsFullPath = outputPath;
        }

        string[] defsFiles = Directory.GetFiles(defsFullPath, "*.trid.xml", SearchOption.AllDirectories);
        Dictionary<string, string> xmlDict = new Dictionary<string, string>();
        foreach (string defsFile in defsFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(defsFile);
            fileName = fileName.Replace(".trid", "");
            var fileContent = File.ReadAllText(defsFile);
            var cs = CompressString(fileContent);
            xmlDict.Add(fileName, cs);
        }
        string jsonString = JsonSerializer.Serialize(xmlDict);
        File.WriteAllText(Path.GetFullPath(definitionsFileName), CompressString(jsonString));
    }

    public static Dictionary<string, string> GetDefinitionsKeyValuePairs()
    {
        var file = Path.GetFullPath(definitionsFileName);
        if (!File.Exists(file))
            return XmlDefinitions.XmlDict;
        string jsonString = File.ReadAllText(file);
        jsonString = DecompressString(jsonString);
        var dics = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        return dics.ToDictionary(item => item.Key, item => DecompressString(item.Value));
    }


    public static string CompressString(string str)
    {
        var compressBeforeByte = Encoding.GetEncoding("UTF-8").GetBytes(str);
        var compressAfterByte = Compress(compressBeforeByte);
        string compressString = Convert.ToBase64String(compressAfterByte);
        return compressString;
    }

    public static string DecompressString(string str)
    {
        var compressBeforeByte = Convert.FromBase64String(str);
        var compressAfterByte = Decompress(compressBeforeByte);
        string compressString = Encoding.GetEncoding("UTF-8").GetString(compressAfterByte);
        return compressString;
    }

    private static byte[] Compress(byte[] data)
    {
        try
        {
            var ms = new MemoryStream();
            var zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(data, 0, data.Length);
            zip.Close();
            var buffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buffer, 0, buffer.Length);
            ms.Close();
            return buffer;

        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    private static byte[] Decompress(byte[] data)
    {
        try
        {
            var ms = new MemoryStream(data);
            var zip = new GZipStream(ms, CompressionMode.Decompress, true);
            var msreader = new MemoryStream();
            var buffer = new byte[0x1000];
            while (true)
            {
                var reader = zip.Read(buffer, 0, buffer.Length);
                if (reader <= 0)
                {
                    break;
                }
                msreader.Write(buffer, 0, reader);
            }
            zip.Close();
            ms.Close();
            msreader.Position = 0;
            buffer = msreader.ToArray();
            msreader.Close();
            return buffer;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
}