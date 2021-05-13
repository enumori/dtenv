using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Xml.Serialization;
using HtmlAgilityPack;
using System.Diagnostics;
using CommandLine;
using Alphaleonis;

namespace dtenv
{
    [Serializable, XmlRoot("VersionsCache"), XmlType("VersionsCache")]
    public class VersionsCache
    {
        [XmlElement("Version")]
        public List<string> VersionList = new List<string>();
    }

    public class Program
    {
        static readonly string _VersionsCacheFile = @".versions_cache.xml";
      
        static readonly string _VersionFile = @".dart-version";

        static public VersionsCache _VersionsCache;
        static string _AppDir = "";
        static string _DartDir = "";
        static int _WaitCharId = 0;
        public enum ErrorCode
        {
            NO_ERROR,
            INVALID_ARGS,
            WITH_NOT_PARSED
        }


        [Verb("--version", HelpText = "dtenvのバージョンを出力します。")]
        public class ShowVersion
        {
        }

        [Verb("install", HelpText = "指定されたバージョンのdart.exeをインストール、もしくは listでインストール可能なバージョンを出力します。")]
        public class Install
        {
            [CommandLine.Option('l', "list", Required = false, HelpText = "インストール可能なバージョンを出力します。")]
            public bool IsList { get; set; }
            [CommandLine.Option('L', "Last", Required = false, HelpText = "最新バージョンをインストールします。")]
            public bool IsLast { get; set; }
            [CommandLine.Value(0, MetaName = "Version", HelpText = "インストールするバージョン")]
            public string Version { get; set; }
        }
        [Verb("uninstall", HelpText = "指定されたバージョンのdart.exeをアンインストールします。")]
        public class Uninstall
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "アンインストールするバージョン")]
            public string Version { get; set; }
        }
        [Verb("update", HelpText = "インストール可能なバージョンを更新します。")]
        public class Update
        {
        }
        [Verb("version", HelpText = "現在のバージョンを出力する。")]
        public class Version
        {
        }
        [Verb("versions", HelpText = "インストール済みのバージョンを表示する。")]
        public class Versions
        {
        }
        [Verb("global", HelpText = "全体で使用するDartのバージョンを指定する。")]
        public class Global
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "使用するバージョン")]
            public string Version { get; set; }
        }
        [Verb("local", HelpText = "フォルダーで使用するDartのバージョンを指定する。")]
        public class Local
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "使用するバージョン")]
            public string Version { get; set; }
        }

        static void Main(string[] args)
        {
            ErrorCode error_code = ErrorCode.INVALID_ARGS;
            _AppDir = System.AppDomain.CurrentDomain.BaseDirectory;
            _DartDir = System.IO.Path.Combine(_AppDir, "versions");


            var serializer = new XmlSerializer(typeof(VersionsCache));
            using (var sr = new System.IO.StreamReader(System.IO.Path.Combine(_AppDir, _VersionsCacheFile), Encoding.UTF8))
            {
                _VersionsCache = (VersionsCache)serializer.Deserialize(sr);
            }


            var parser = new Parser(config => { config.IgnoreUnknownArguments = false; config.AutoVersion = false; config.HelpWriter = Console.Out; });
            var result = parser.ParseArguments<ShowVersion, Install, Uninstall, Update, Version, Versions, Global, Local>(args)
                .WithParsed<ShowVersion>(opts => {
                    error_code = ProcShowVersion(opts);
                })
                .WithParsed<Install>(opts => {
                    error_code = ProcInstall(opts);
                })
                .WithParsed<Uninstall>(opts => {
                    error_code = ProcUninstall(opts);
                })
                .WithParsed<Update>(opts => {
                    error_code = ProcUpdate(opts);
                })
                .WithParsed<Version>(opts => {
                    error_code = ProcVersion(opts);
                })
                .WithParsed<Versions>(opts => {
                    error_code = ProcVersions(opts);
                })
                .WithParsed<Global>(opts => {
                    error_code = ProcGlobal(opts);
                })
                .WithParsed<Local>(opts => {
                    error_code = ProcLocal(opts);
                })
                .WithNotParsed(errs => {
                    error_code = ErrorCode.WITH_NOT_PARSED;
                });


            if (error_code == ErrorCode.INVALID_ARGS)
            {
                parser.ParseArguments<ShowVersion, Install, Version, Versions, Global, Local>(new string[] { "" });
            }

#if DEBUG
            Console.WriteLine("続行するには何かキーを押してください．．．");
            Console.ReadKey();
#endif
            Environment.Exit((error_code != ErrorCode.NO_ERROR) ? 1 : 0);
        }

        static ErrorCode ProcShowVersion(Program.ShowVersion opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            System.Diagnostics.FileVersionInfo ver =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.Console.WriteLine(ver.FileVersion);
            return ret_code;
        }
        static ErrorCode ProcInstall(Program.Install opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            int num_opts = 0;
            System.Diagnostics.Debug.WriteLine("ProcInstall");
            

            if (opt.IsList)
            {
                ++num_opts;
            }
            if (opt.Version != null)
            {
                ++num_opts;
            }
            if (opt.IsLast)
            {
                ++num_opts;
            }
            if (num_opts != 1)
            {
                return ErrorCode.INVALID_ARGS;
            };
            if (opt.IsList)
            {
                ret_code = ShowList();
            }
            if (opt.IsLast)
            {
                InstallDart(_VersionsCache.VersionList[_VersionsCache.VersionList.Count - 1]);
            }
            if (opt.Version != null)
            {
                ret_code = InstallDart(opt.Version);
            }
            return ret_code;
        }

        static ErrorCode ProcUninstall(Program.Uninstall opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (opt.Version != null)
            {
                var version = opt.Version.ToLower();
                if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^[0-9]+(\.[0-9]+)*"))
                {
                    System.Console.Error.WriteLine("不正なバージョン文字列です。");
                    return ErrorCode.INVALID_ARGS;
                }
                var dart_path = System.IO.Path.Combine(_DartDir, opt.Version);
                if (!System.IO.Directory.Exists(dart_path))
                {
                    System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                    return ErrorCode.INVALID_ARGS;
                }
                DeleteDirectory(dart_path);
            }
            else
            {
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static void DeleteDirectory(string dir)
        {
            Alphaleonis.Win32.Filesystem.DirectoryInfo di = new Alphaleonis.Win32.Filesystem.DirectoryInfo(dir);

            RemoveReadonlyAttribute(di);

            di.Delete(true);

            return;
        }

        static void RemoveReadonlyAttribute(Alphaleonis.Win32.Filesystem.DirectoryInfo dirInfo)
        {
            if ((dirInfo.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
            {
                dirInfo.Attributes = System.IO.FileAttributes.Normal;
            }
            foreach (var fi in dirInfo.GetFiles())
            {
                if ((fi.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    fi.Attributes = System.IO.FileAttributes.Normal;
                }
            }
            foreach (var di in dirInfo.GetDirectories())
            {
                RemoveReadonlyAttribute(di);
            }
            return;
        }

        static ErrorCode ProcVersion(Program.Version opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_DartDir))
            {
                System.Console.Error.WriteLine("Dartがインストールされていません。");
                return ret_code;
            }
            System.Diagnostics.Debug.WriteLine("ProcVersion");
            var ver_file = System.IO.Path.Combine(System.Environment.CurrentDirectory, _VersionFile);
            if (System.IO.File.Exists(ver_file))
            {
                string version = "";
                using (System.IO.StreamReader sr = new System.IO.StreamReader(ver_file, Encoding.GetEncoding("utf-8")))
                {
                    version = sr.ReadToEnd();
                }
                var versions = System.IO.Directory.GetDirectories(_DartDir).ToList();
                if (versions.Where(w => System.IO.Path.GetFileName(w) == version).Any())
                {
                    System.Console.WriteLine(version + "(set by " + ver_file + ")");
                    return ret_code;
                }
            }
            ver_file = System.IO.Path.Combine(_AppDir, _VersionFile);
            if (System.IO.File.Exists(ver_file))
            {
                string version = "";
                using (System.IO.StreamReader sr = new System.IO.StreamReader(ver_file, Encoding.GetEncoding("utf-8")))
                {
                    version = sr.ReadToEnd();
                }
                var versions = System.IO.Directory.GetDirectories(_DartDir).ToList();
                if (versions.Where(w => System.IO.Path.GetFileName(w) == version).Any())
                {
                    System.Console.WriteLine(version);
                    return ret_code;
                }
            }
            System.Console.Error.WriteLine("使用するバージョンが設定されていません。");
            return ret_code;
        }
        static ErrorCode ProcVersions(Program.Versions opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_DartDir))
            {
                System.Console.Error.WriteLine("Dartがインストールされていません。");
                return ret_code;
            }
            var versions = System.IO.Directory.GetDirectories(_DartDir).ToList();
            foreach (var ver in versions)
            {
                System.Console.WriteLine(System.IO.Path.GetFileName(ver));
            }
            return ret_code;
        }
        static ErrorCode ProcGlobal(Program.Global opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_DartDir))
            {
                System.Console.Error.WriteLine("Dartがインストールされていません。");
                return ret_code;
            }
            var ver_file = System.IO.Path.Combine(_AppDir, _VersionFile);
            var versions = System.IO.Directory.GetDirectories(_DartDir).ToList();
            if (versions.Where(w => System.IO.Path.GetFileName(w) == opt.Version).Any())
            {
                if (System.IO.File.Exists(ver_file))
                {
                    System.IO.File.Delete(ver_file);
                }
                var utf8_encoding = new System.Text.UTF8Encoding(false);
                using (var wr = new System.IO.StreamWriter(ver_file, false, utf8_encoding))
                {
                    wr.Write(opt.Version);
                }
            }
            else
            {
                System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static ErrorCode ProcLocal(Program.Local opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_DartDir))
            {
                System.Console.Error.WriteLine("Dartがインストールされていません。");
                return ret_code;
            }
            var ver_file = System.IO.Path.Combine(System.Environment.CurrentDirectory, _VersionFile);
            var versions = System.IO.Directory.GetDirectories(_DartDir).ToList();
            if (versions.Where(w => System.IO.Path.GetFileName(w) == opt.Version).Any())
            {
                if (System.IO.File.Exists(ver_file))
                {
                    System.IO.File.Delete(ver_file);
                }
                var utf8_encoding = new System.Text.UTF8Encoding(false);
                using (var wr = new System.IO.StreamWriter(ver_file, false, utf8_encoding))
                {
                    wr.Write(opt.Version);
                }
            }
            else
            {
                System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static ErrorCode ShowList()
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            foreach (var ver in _VersionsCache.VersionList)
            {
                Console.WriteLine(ver);
            }
            return ret_code;
        }

        static ErrorCode InstallDart(string version)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            version = version.ToLower();
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^[0-9]+(\.[0-9]+)*"))
            {
                System.Console.Error.WriteLine("不正なバージョン文字列です。");
                return ErrorCode.INVALID_ARGS;
            }
            if (!_VersionsCache.VersionList.Where(w => w == version).Any())
            {
                System.Console.Error.WriteLine("指定されたバージョンが見つかりませんでした。");
                return ErrorCode.INVALID_ARGS;
            }
            if ((ret_code = DownloadDart(version)) != ErrorCode.NO_ERROR)
            {
                return ret_code;
            }
            return ret_code;
        }
        static string WaitChar()
        {

            if (_WaitCharId == 0)
            {
                _WaitCharId = 1;
                return "\b|";
            }
            else
            {
                _WaitCharId = 0;
                return "\b-";
            }

        }
        static ErrorCode ProcUpdate(Program.Update opt)
        {
            string source = "";
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            var address = Properties.Settings.Default.DartGitHub;
            Console.Write(address + "を検索中です。 ");
            while (true)
            {
                bool hasNext = false;
                System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(address);
                Console.Write(WaitChar());
                using (System.Net.HttpWebResponse webres = (System.Net.HttpWebResponse)webreq.GetResponse())
                {
                    using (System.IO.Stream st = webres.GetResponseStream())
                    {
                        //文字コードを指定して、StreamReaderを作成
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(st, System.Text.Encoding.UTF8))
                        {
                            source = sr.ReadToEnd();
                        }
                    }
                }

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(source);
                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a"))
                {
                    Console.Write(WaitChar());
                    if (link.InnerText.Trim().ToLower() == "next")
                    {
                        address = link.GetAttributeValue("href", null);
                        hasNext = true;
                    }
                    else if (link.InnerText.Trim().ToLower() == "zip")
                    {
                        var url = link.GetAttributeValue("href", null).Split('/').Last();
                        if (System.Text.RegularExpressions.Regex.IsMatch(url, @"^(([0-9\-]+)\.)+zip$"))
                        {
                            
                            var version = System.IO.Path.GetFileNameWithoutExtension(url);
                            if (_VersionsCache.VersionList.Where(w => w == version).Any())
                            {
                                hasNext = false;
                                break;
                            }
                            else
                            {
                                _VersionsCache.VersionList.Add(version);
                                if (version == Properties.Settings.Default.DartOldVersion)
                                {
                                    hasNext = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!hasNext)
                {
                    break;
                }
                System.Threading.Thread.Sleep(Properties.Settings.Default.UpdateSleep);
            }
            Console.Write("\b ");
            Console.Write(Environment.NewLine);

            _VersionsCache.VersionList.Sort((a, b) => string.Compare(Version2String(a), Version2String(b)));

            var serializer = new XmlSerializer(typeof(VersionsCache));
            using (var sw = new System.IO.StreamWriter(System.IO.Path.Combine(_AppDir, _VersionsCacheFile), false, Encoding.UTF8))
            {
                serializer.Serialize(sw, _VersionsCache);
                sw.Flush();
            }
            return ret_code;
        }

        static string Version2String(string version)
        {
            Regex re = new Regex(@"[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = re.Match(version);
            string ver_string = "";
            while (m.Success)
            {
                ver_string += string.Format("{0:D4}", Convert.ToInt32(m.Value));
                m = m.NextMatch();
            }
            return ver_string;
        }

        static ErrorCode DownloadDart(string version)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;

            string dir = System.IO.Path.Combine(_DartDir, version);
            if (!System.IO.Directory.Exists(_DartDir))
            {
                System.IO.Directory.CreateDirectory(_DartDir);
            }
            if (!System.IO.Directory.Exists(dir))
            {
                System.Console.WriteLine("Dart(" + version + ")をインストール中");
                string download = "dartsdk-windows-release.zip";
                string url = "";
                if (Environment.Is64BitOperatingSystem)
                {
                    url = string.Format(Properties.Settings.Default.DartURL, version, "x64");
                }
                else
                {
                    url = string.Format(Properties.Settings.Default.DartURL, version, "ia32");
                }
                System.Net.WebClient wc = new System.Net.WebClient();
                download = System.IO.Path.Combine(_DartDir, download);
                System.Diagnostics.Debug.WriteLine("ダウンロード: " + url + " -> " + download);
                wc.DownloadFile(url, download);
                wc.Dispose();
                var dart_dir = ExtractToDirectoryExtensions(download, _DartDir, true);
                dart_dir = System.IO.Path.Combine(_DartDir, dart_dir);
                System.IO.File.Delete(download);
                System.IO.Directory.Move(dart_dir, dir);
            }
            return ret_code;
        }
        static string ExtractToDirectoryExtensions(string sourceArchiveFileName, string destinationDirectoryName, bool overwrite)
        {
            string top_name = "";
            using (ZipArchive archive = ZipFile.OpenRead(sourceArchiveFileName))
            {
                top_name = System.IO.Directory.GetParent(archive.Entries[0].FullName).FullName.Split('\\').Last();
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var fullPath = System.IO.Path.Combine(destinationDirectoryName, entry.FullName).Replace('/', '\\');

                    var parent = System.IO.Directory.GetParent(fullPath).FullName;
                    if (!System.IO.Directory.Exists(parent))
                    {
                        System.IO.Directory.CreateDirectory(parent);
                    }
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        if (!System.IO.Directory.Exists(fullPath))
                        {
                            System.IO.Directory.CreateDirectory(fullPath);
                        }
                    }
                    else
                    {
                        if (overwrite)
                        {
                            entry.ExtractToFile(fullPath, true);
                        }
                        else
                        {
                            if (!System.IO.File.Exists(fullPath))
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                        }
                    }
                }
            }
            return top_name;
        }
    }
}
