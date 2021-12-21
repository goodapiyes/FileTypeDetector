// See https://aka.ms/new-console-template for more information

using TrIDCore;

Console.WriteLine("Hello, World!");


//auto-upgrade trid db From https://mark0.net/download/triddefs_xml.7z
//TrIDExtension.UpgradeDefinitions();

var demoFilePath = Path.GetFullPath("DemoFile");

//get results list,results are presented in order of highest probability.
var results = TrIDEngine.GetExtensions(demoFilePath);

Console.WriteLine(results.FirstOrDefault().FileExt);

Console.ReadLine();