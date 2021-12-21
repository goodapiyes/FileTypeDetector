using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System.Collections;
using System.Xml;
namespace TrIDCore;

public class TrIDEngine
{
    private static readonly ArrayList _definitions;
    private static readonly int _maxFrontSize;

    static TrIDEngine()
    {
        var xmlDict = TrIDExtension.GetDefinitionsKeyValuePairs();
        TrIDEngine.TrIdEngineConf[] array = xmlDict.Values.Select<string, TrIDEngine.TrIdEngineConf>((Func<string, TrIDEngine.TrIdEngineConf>) (e => TrIDEngine.PatternEngine.LoadDefinitionByXml(e, 0))).Where<TrIDEngine.TrIdEngineConf>((Func<TrIDEngine.TrIdEngineConf, bool>) (e => e != null)).ToArray<TrIDEngine.TrIdEngineConf>();
        TrIDEngine._maxFrontSize = ((IEnumerable<TrIDEngine.TrIdEngineConf>) array).Max<TrIDEngine.TrIdEngineConf>((Func<TrIDEngine.TrIdEngineConf, int>) (e => e.MaxFrontSize));
        TrIDEngine._definitions = new ArrayList();
        TrIDEngine._definitions.AddRange((ICollection) ((IEnumerable<TrIDEngine.TrIdEngineConf>) array).Select<TrIDEngine.TrIdEngineConf, TrIDEngine.FileDefPat>((Func<TrIDEngine.TrIdEngineConf, TrIDEngine.FileDefPat>) (e => e.Definition)).ToArray<TrIDEngine.FileDefPat>());
    }

    public static string GetExtensionByFileContent(string filePath)
    {
        TrIDEngine.PatternEngine patternEngine = new TrIDEngine.PatternEngine(TrIDEngine._definitions, TrIDEngine._maxFrontSize);
        patternEngine.SubmitFile(filePath);
        patternEngine.Analyze();
        string fileExt = ((IEnumerable<TrIDEngine.Result>) patternEngine.GetResultsData(1)).FirstOrDefault<TrIDEngine.Result>().ExtraInfo.FileExt;
        return fileExt != null ? "." + fileExt : (string) null;
    }

    public static TrIDEngine.Result[] GetExtensions(string filePath)
    {
        TrIDEngine.PatternEngine patternEngine = new TrIDEngine.PatternEngine(TrIDEngine._definitions, TrIDEngine._maxFrontSize);
        patternEngine.SubmitFile(filePath);
        patternEngine.Analyze();
        return patternEngine.GetResultsData(int.MaxValue);
    }

    public static string GetBestExtension(string filePath, string defaultExtension = null)
    {
        TrIDEngine.Result[] extensions = TrIDEngine.GetExtensions(filePath);
        var data = extensions != null ? ((IEnumerable<TrIDEngine.Result>) extensions).GroupBy<TrIDEngine.Result, string>((Func<TrIDEngine.Result, string>) (result => result.FileExt)).Select(results => new
        {
            result = results.First<TrIDEngine.Result>(),
            pers = results.Sum<TrIDEngine.Result>((Func<TrIDEngine.Result, float>) (result => result.Perc))
        }).OrderByDescending(arg => arg.pers).FirstOrDefault() : null;
        return data == null || (double) data.pers <= 50.0 ? defaultExtension : data.result.FileExt;
    }

    public class PatternEngine
    {
        private ArrayList _definitions = new ArrayList();
        private int _fileFrontSize;
        private TrIDEngine.ByteString _frontBlock;
        private readonly ArrayList _results = new ArrayList();
        private string _submittedFile;

        public PatternEngine()
        {
        }

        public PatternEngine(ArrayList definitions, int maxFrontSize)
        {
            this._definitions = definitions;
            this._fileFrontSize = maxFrontSize;
        }

        public int DefInMemory => this._definitions.Count;

        public string Version => "1.04";

        public void Analyze()
        {
            bool flag = true;
            int num1 = 0;
            this._results.Clear();
            byte[] pBlock = (byte[]) null;
            TrIDEngine.Result result;
            foreach (TrIDEngine.FileDefPat definition in this._definitions)
            {
                int num2 = 0;
                if (this._fileFrontSize >= definition.FrontBlockSize)
                {
                    foreach (TrIDEngine.SomePattern pattern in definition.Patterns)
                    {
                        int num3 = 0;
                        int num4 = pattern.Len - 1;
                        for (int index = 0; index <= num4; ++index)
                        {
                            if ((int) this._frontBlock.data[pattern.Pos + index] == (int) pattern.Pattern[index])
                                ++num3;
                        }
                        if (num3 == pattern.Len)
                        {
                            num2 += pattern.Len * pattern.Points;
                        }
                        else
                        {
                            num2 = 0;
                            break;
                        }
                    }
                    //IEnumerator enumerator1;
                    //enumerator1.Reset();
                    if (num2 > 0)
                    {
                        if (definition.GlobalStrings.Count > 0)
                        {
                            if (flag)
                            {
                                BinaryReader binaryReader = (BinaryReader) null;
                                FileStream input = (FileStream) null;
                                try
                                {
                                    input = new FileStream(this._submittedFile, FileMode.Open, FileAccess.Read);
                                    binaryReader = new BinaryReader((Stream) input);
                                    if ((ulong) input.Length > 0UL)
                                    {
                                        if (input.Length <= 10485760L)
                                        {
                                            pBlock = binaryReader.ReadBytes((int) input.Length);
                                        }
                                        else
                                        {
                                            pBlock = binaryReader.ReadBytes(5242880);
                                            pBlock = (byte[]) Utils.CopyArray((Array) pBlock, (Array) new byte[10485762]);
                                            input.Seek(-5242880L, SeekOrigin.End);
                                            byte[] numArray = binaryReader.ReadBytes(5242880);
                                            pBlock[5242880] = (byte) 124;
                                            numArray.CopyTo((Array) pBlock, 5242881);
                                        }
                                        flag = false;
                                        TrIDEngine.PatternEngine.Ba2Upper(ref pBlock);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ProjectData.SetProjectError(ex);
                                    this._results.Clear();
                                    throw new Exception("ERROR");
                                }
                                finally
                                {
                                    if (input != null)
                                    {
                                        input.Close();
                                        binaryReader?.Close();
                                    }
                                }
                            }
                            foreach (TrIDEngine.ByteString globalString in definition.GlobalStrings)
                            {
                                if (this.ByteArraySearchCs(pBlock, globalString.data) != -1)
                                {
                                    num2 += globalString.data.Length * 500;
                                }
                                else
                                {
                                    num2 = 0;
                                    break;
                                }
                            }
                            //IEnumerator enumerator2;
                            //enumerator2.Reset();
                        }
                        if (num2 > 0)
                        {
                            result = new TrIDEngine.Result()
                            {
                                FileType = definition.FileType,
                                FileExt = definition.FileExt,
                                ExtraInfo = definition.ExtraInfo
                            };
                            result.ExtraInfo.FilePts = StringType.FromInteger(num2) + "/" + StringType.FromInteger(definition.Patterns.Count);
                            if (definition.GlobalStrings.Count > 0)
                                result.ExtraInfo.FilePts = result.ExtraInfo.FilePts + "/" + StringType.FromInteger(definition.GlobalStrings.Count);
                            result.FileType = result.FileType + " (" + StringType.FromInteger(num2) + "/" + StringType.FromInteger(definition.Patterns.Count) + ")";
                            result.Points = num2;
                            this._results.Add((object) result);
                            num1 += num2;
                        }
                    }
                }
            }
            //IEnumerator enumerator;
            //enumerator.Reset();
            if (this._results.Count <= 0)
                return;
            int num5 = this._results.Count - 1;
            for (int index = 0; index <= num5; ++index)
            {
                result = (TrIDEngine.Result) this._results[index];
                result.Perc = (float) (result.Points * 100) / (float) num1;
                this._results[index] = (object) result;
            }
        }

        private static void Ba2Upper(ref byte[] pBlock)
        {
            int num1 = pBlock.Length - 1;
            for (int index = 0; index <= num1; ++index)
            {
                byte num2 = pBlock[index];
                if (num2 > (byte) 96 & num2 < (byte) 123)
                    pBlock[index] = (byte) ((uint) num2 - 32U);
            }
        }

        private int ByteArraySearchCs(byte[] pBig, byte[] pSearch)
        {
            int[] numArray = new int[256];
            int length1 = pSearch.Length;
            int length2 = pBig.Length;
            int index1 = 0;
            do
            {
                numArray[index1] = length1;
                ++index1;
            }
            while (index1 <= (int) byte.MaxValue);
            int num1 = length1;
            for (int index2 = 1; index2 <= num1; ++index2)
            {
                byte index3 = pSearch[index2 - 1];
                numArray[(int) index3] = length1 - index2;
            }
            int num2 = length1;
            int num3 = length1;
            do
            {
                if ((int) pBig[num2 - 1] == (int) pSearch[num3 - 1])
                {
                    --num2;
                    --num3;
                }
                else
                {
                    if (length1 - num3 + 1 > numArray[(int) pBig[num2 - 1]])
                        num2 = num2 + length1 - num3 + 1;
                    else
                        num2 += numArray[(int) pBig[num2 - 1]];
                    num3 = length1;
                }
            }
            while (num3 >= 1 & num2 <= length2);
            return num2 >= length2 ? -1 : num2;
        }

        public void ClearDefinitions() => this._definitions.Clear();

        public object GetDefinitions() => (object) (ArrayList) this._definitions.Clone();

        public TrIDEngine.Result[] GetResultsData(int pResNum)
        {
            if (this._results.Count <= 0)
                return new TrIDEngine.Result[1];
            TrIDEngine.Result[] items = new TrIDEngine.Result[this._results.Count];
            int index1 = 0;
            int[] keys = new int[this._results.Count];
            foreach (TrIDEngine.Result result in this._results)
            {
                if (result.Points > 0)
                {
                    items[index1] = result;
                    keys[index1] = result.Points;
                    ++index1;
                }
            }
            Array.Sort<int, TrIDEngine.Result>(keys, items);
            Array.Reverse((Array) items);
            if (pResNum > index1)
                pResNum = index1;
            TrIDEngine.Result[] resultsData = new TrIDEngine.Result[pResNum - 1 + 1];
            int num = pResNum - 1;
            for (int index2 = 0; index2 <= num; ++index2)
                resultsData[index2] = items[index2];
            return resultsData;
        }

        public string[] GetResultsStrings(int pResNum)
        {
            string[] resultsStrings1 = new string[1];
            int index1 = 0;
            resultsStrings1[0] = "Unknown!";
            if (this._results.Count <= 0)
                return resultsStrings1;
            string[] items = new string[this._results.Count - 1 + 1];
            int[] keys = new int[this._results.Count - 1 + 1];
            int num1 = this._results.Count - 1;
            for (int index2 = 0; index2 <= num1; ++index2)
            {
                TrIDEngine.Result result = (TrIDEngine.Result) this._results[index2];
                if (result.Points > 0)
                {
                    string str = (uint) StringType.StrCmp(result.FileExt, "", false) <= 0U ? "" : "(." + result.FileExt + ") ";
                    items[index1] = result.Perc.ToString("##0.0").PadLeft(5) + "% " + str + result.FileType;
                    keys[index1] = result.Points;
                    ++index1;
                }
            }
            Array.Sort<int, string>(keys, items);
            Array.Reverse((Array) items);
            if (pResNum > index1)
                pResNum = index1;
            string[] resultsStrings2 = new string[pResNum - 1 + 1];
            int num2 = pResNum - 1;
            for (int index3 = 0; index3 <= num2; ++index3)
                resultsStrings2[index3] = items[index3];
            return resultsStrings2;
        }

        public bool IsBinary()
        {
            if ((uint) StringType.StrCmp(this._submittedFile, "", false) > 0U)
            {
                int num1 = this._fileFrontSize - 1;
                for (int index = 0; index <= num1; ++index)
                {
                    byte num2 = this._frontBlock.data[index];
                    if (num2 < (byte) 9 | num2 > (byte) 126)
                        return true;
                }
            }
            return false;
        }

        private static void LoadDefinitionNode(
            XmlDocument document,
            TrIDEngine.FileDefPat pat,
            XmlNodeList list2)
        {
            foreach (XmlNode xmlNode in list2)
            {
                TrIDEngine.SomePattern somePattern = new TrIDEngine.SomePattern()
                {
                    Pos = IntegerType.FromString(xmlNode.SelectSingleNode("Pos").InnerText)
                };
                string innerText = xmlNode.SelectSingleNode("Bytes").InnerText;
                somePattern.Len = innerText.Length / 2;
                somePattern.Pattern = new byte[somePattern.Len + 1];
                int len = somePattern.Len;
                for (int index = 1; index <= len; ++index)
                    somePattern.Pattern[index - 1] = (byte) Math.Round(Conversion.Val("&H" + innerText.Substring((index - 1) * 2, 2)));
                pat.FrontBlockSize = somePattern.Pos + somePattern.Len;
                somePattern.XM = false;
                somePattern.Points = 1;
                if (somePattern.Pos == 0)
                    somePattern.Points = 1000;
                pat.Patterns.Add((object) somePattern);
            }
            list2 = document.SelectNodes("//GlobalStrings/String");
            if (list2 == null)
                return;
            foreach (XmlNode xmlNode in list2)
            {
                string innerText = xmlNode.InnerText;
                TrIDEngine.ByteString byteString;
                byteString.data = new byte[Strings.Len(innerText) - 1 + 1];
                int num = Strings.Len(innerText) - 1;
                for (int index = 0; index <= num; ++index)
                {
                    byteString.data[index] = (byte) Strings.Asc(innerText[index]);
                    if (byteString.data[index] == (byte) 39)
                        byteString.data[index] = (byte) 0;
                }
                TrIDEngine.PatternEngine.Ba2Upper(ref byteString.data);
                pat.GlobalStrings.Add((object) byteString);
            }
        }

        public void LoadDefinitionByFilePath(string pFileName)
        {
            if (!File.Exists(pFileName))
                return;
            this.LoadDefinitionByXml(File.ReadAllText(pFileName));
        }

        public void LoadDefinitionByXml(string xml)
        {
            TrIDEngine.TrIdEngineConf trIdEngineConf = TrIDEngine.PatternEngine.LoadDefinitionByXml(xml, this._fileFrontSize);
            if (trIdEngineConf == null)
                return;
            this._definitions.Add((object) trIdEngineConf.Definition);
            this._fileFrontSize = trIdEngineConf.MaxFrontSize;
        }

        public static TrIDEngine.TrIdEngineConf LoadDefinitionByXml(
            string xml,
            int maxFrontSize = 0)
        {
            TrIDEngine.TrIdEngineConf trIdEngineConf = (TrIDEngine.TrIdEngineConf) null;
            if (xml != null)
            {
                trIdEngineConf = new TrIDEngine.TrIdEngineConf();
                TrIDEngine.FileDefPat pat = new TrIDEngine.FileDefPat();
                XmlDocument document = new XmlDocument();
                document.LoadXml(xml);
                XmlNode xmlNode1 = document.SelectSingleNode("//Info/FileType");
                pat.FileType = xmlNode1?.InnerText;
                XmlNode xmlNode2 = document.SelectSingleNode("//Info/Ext");
                if (xmlNode2 != null)
                    pat.FileExt = xmlNode2.InnerText;
                pat.ExtraInfo.FileType = pat.FileType;
                pat.ExtraInfo.FileExt = pat.FileExt;
                XmlNode xmlNode3 = document.SelectSingleNode("//Info/User");
                if (xmlNode3 != null)
                    pat.ExtraInfo.AuthorName = xmlNode3.InnerText;
                XmlNode xmlNode4 = document.SelectSingleNode("//Info/E-Mail");
                if (xmlNode4 != null)
                    pat.ExtraInfo.AuthorEMail = xmlNode4.InnerText;
                XmlNode xmlNode5 = document.SelectSingleNode("//Info/Home");
                if (xmlNode5 != null)
                    pat.ExtraInfo.AuthorHome = xmlNode5.InnerText;
                XmlNode xmlNode6 = document.SelectSingleNode("//ExtraInfo/Rem");
                if (xmlNode6 != null)
                    pat.ExtraInfo.Remark = xmlNode6.InnerText;
                XmlNode xmlNode7 = document.SelectSingleNode("//ExtraInfo/RefURL");
                if (xmlNode7 != null)
                    pat.ExtraInfo.RelURL = xmlNode7.InnerText;
                XmlNode xmlNode8 = document.SelectSingleNode("//General/FileNum");
                if (xmlNode8 != null)
                    pat.ExtraInfo.FilesScanned = (int) Math.Round(Conversion.Val(xmlNode8.InnerText));
                XmlNodeList list2 = document.SelectNodes("//FrontBlock/Pattern");
                TrIDEngine.PatternEngine.LoadDefinitionNode(document, pat, list2);
                trIdEngineConf.MaxFrontSize = Math.Max(maxFrontSize, pat.FrontBlockSize);
                trIdEngineConf.Definition = pat;
            }
            return trIdEngineConf;
        }

        public object SetDefs(ref ArrayList pDefObject)
        {
            this._definitions = pDefObject;
            return (object) null;
        }

        public void SubmitFile(string pFileName)
        {
            if (!File.Exists(pFileName))
                return;
            BinaryReader binaryReader = (BinaryReader) null;
            FileStream input = (FileStream) null;
            this._fileFrontSize = 4096;
            try
            {
                input = new FileStream(pFileName, FileMode.Open, FileAccess.Read);
                binaryReader = new BinaryReader((Stream) input);
                if ((ulong) input.Length > 0UL)
                {
                    if (input.Length < (long) this._fileFrontSize)
                        this._fileFrontSize = (int) input.Length;
                    this._frontBlock.data = binaryReader.ReadBytes(this._fileFrontSize);
                }
                this._submittedFile = pFileName;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                this._fileFrontSize = 0;
                throw new Exception("ERROR");
            }
            finally
            {
                if (input != null)
                {
                    input.Close();
                    binaryReader?.Close();
                }
            }
        }
    }

    [Serializable]
    private struct ByteString
    {
        public byte[] data;
    }

    [Serializable]
    public struct ExtraInfo
    {
        public string FileType;
        public string FileExt;
        public string FilePts;
        public int FilesScanned;
        public string AuthorName;
        public string AuthorEMail;
        public string AuthorHome;
        public string DefFile;
        public string Remark;
        public string RelURL;
    }

    [Serializable]
    public class FileDefPat
    {
        public TrIDEngine.ExtraInfo ExtraInfo;
        public string FileExt;
        public string FileType;
        public int FrontBlockSize;
        public readonly ArrayList GlobalStrings = new ArrayList();
        public readonly ArrayList Patterns = new ArrayList();
    }

    [Serializable]
    private struct SomePattern
    {
        public byte[] Pattern;
        public int Pos;
        public int Len;
        public int Points;
        public bool XM;
    }

    public struct Result
    {
        public string FileType;
        public string FileExt;
        public int Points;
        public float Perc;
        public TrIDEngine.ExtraInfo ExtraInfo;
    }

    public class TrIdEngineConf
    {
        public TrIDEngine.FileDefPat Definition { get; set; }

        public int MaxFrontSize { get; set; }
    }
}