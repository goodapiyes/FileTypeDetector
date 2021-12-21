FileTypeDetector-TrIDCore
=================

Base on TrIDEngine 

Supports 1w types of file identification

https://mark0.net/code-tridengine-e.html

Update to .NET 6.0

Identify file types from their binary signatures.

support automatic update of signature database(base on https://mark0.net)

## Example

```C#
//auto-upgrade trid db From https://mark0.net/download/triddefs_xml.7z
//TrIDExtension.UpgradeDefinitions();

var demoFilePath = Path.GetFullPath("DemoFile");

//get results list,results are presented in order of highest probability.
var results = TrIDEngine.GetExtensions(demoFilePath);

Console.WriteLine($"file extension name: {results.FirstOrDefault().FileExt}");
Console.WriteLine($"file type: {results.FirstOrDefault().FileType}");

```
