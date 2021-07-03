using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using JVDTLabLib;

using static JVData_Struct;

namespace JVLink_Testing
{
    struct ProgressBar
    {
        public int Maximum { set; get; }

        public int Value { set; get; }
    }

    enum JVOpenOption
    {
        /// <summary>通常データ</summary>
        Normal = 1,

        /// <summary>
        /// 今週データ
        /// 
        /// これを指定すると fromtime に先週以前が含まれていても、
        /// 今週のデータしか降ってこないっぽい。
        /// </summary>
        ThisWeek = 2,

        /// <summary>
        /// セットアップデータ
        /// 
        /// セットアップ時にのみ使用する想定。
        /// </summary>
        Setup = 3,

        /// <summary>
        /// ダイアログ無セットアップデータ (初回のみダイアログを表示)
        /// 
        /// セットアップ時にのみ使用する想定。
        /// </summary>
        SetupWithoutDialog = 4
    }

    class Program
    {
        const string FromtimeFormat = "yyyyMMddHHmmss";

        const string UserAgent = "UNKNOWN";

        static string AppDataPath;

        static JVLink JVLink = new JVLink();

        [STAThread]
        static void Main(string[] args)
        {
            var fullAssemblyName = Assembly.GetEntryAssembly().Location;
            var assemblyName = Path.GetFileNameWithoutExtension(fullAssemblyName);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AppDataPath = Path.Combine(appData, assemblyName);

            var dataspec = "RACE";
            var fromdate = DateTime.Now.AddYears(-1).ToString(FromtimeFormat);
            var option = JVOpenOption.Normal;
            var outDir = ".";

            var parsedArgs = new Dictionary<string, List<string>>();

            foreach (var a in args)
            {
                if (a.StartsWith("-"))
                {
                    parsedArgs[a] = new List<string>();
                }
                else
                {
                    parsedArgs.Last().Value.Add(a);
                }
            }

            if (parsedArgs.ContainsKey("--jv-open-fromdate"))
            {
                fromdate = parsedArgs["--jv-open-fromdate"][0].
                    Trim().
                    Replace("-", "").
                    Replace("/", "").
                    Replace(":", "").
                    PadRight(FromtimeFormat.Length, '0');
            }

            if (parsedArgs.ContainsKey("--outdir"))
            {
                outDir = parsedArgs["--outdir"][0];
            }

            if (parsedArgs.ContainsKey("--jv-open-option"))
            {
                bool success =
                    Enum.TryParse(parsedArgs["--jv-open-option"][0], out option) &&
                    Enum.IsDefined(typeof(JVOpenOption), option);

                if (!success)
                {
                    Console.Error.WriteLine("--jv-open-option の値が不正です。");
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("JVOpen dataspec: \"{0}\"", dataspec);
            Console.WriteLine("JVOpen fromdate: \"{0}\"", fromdate);
            Console.WriteLine("JVOpen option: \"{0}\" ({1})", option, (int)option);
            Console.WriteLine("Write to: \"{0}\"", outDir);

            Execute(dataspec, fromdate, option, outDir);
        }

        static void Execute(string dataspec, string fromdate, JVOpenOption option, string outDir)
        {
            JVLink.JVInit(UserAgent);

            var nReadCount = 0;             // JVOpen: 総読み込みファイル数
            var nDownloadCount = 0;         // JVOpen: 総ダウンロードファイル数
            var strLastFileTimestamp = "";  // JVOpen: 最新ファイルのタイムスタンプ
            var prgDownload = new ProgressBar();
            var prgJVRead = new ProgressBar();

            var r = JVLink.JVOpen(dataspec, fromdate, (int)option, ref nReadCount, ref nDownloadCount, out strLastFileTimestamp);

            if (r != 0)
            {
                Environment.Exit(r);
            }

            if (nDownloadCount == 0)
            {
                prgDownload.Maximum = 100;
                prgDownload.Value = 100;
            }
            else
            {
                prgDownload.Maximum = nDownloadCount;
                prgDownload.Value = 0;
            }

            prgJVRead.Maximum = nReadCount;
            prgJVRead.Value = 0;

            Console.WriteLine(
                "読み込みファイル数 : " + nReadCount + "\n" +
                "ダウンロードファイル数 : " + nDownloadCount + "\n" +
                "タイムスタンプ : " + strLastFileTimestamp + "\n"
            );

            var nBuffSize = 110000;                         // JVRead: データ格納バッファサイズ
            var nNameSize = 256;                            // JVRead: ファイル名サイズ
            var strBuff = new string('\0', nBuffSize);      // JVRead: データ格納バッファ
            var strFileName = new string('\0', nNameSize);  // JVRead: 読み込み中ファイル名
            var raceInfo = new JV_RA_RACE();                // レース詳細情報構造体
            var raceUmaInfo = new JV_SE_RACE_UMA();
            var bataijyuInfo = new JV_WH_BATAIJYU();
            var weatherInfo = new JV_WE_WEATHER();
            bool flg_exit = false;

            using (var _raceStream = new FileStream(Path.Combine(outDir, "races.csv"), FileMode.Create))
            using (var raceStream = new StreamWriter(_raceStream, Encoding.GetEncoding("shift_jis")))
            using (var _raceHorseStream = new FileStream(Path.Combine(outDir, "race_horses.csv"), FileMode.Create))
            using (var raceHorseStream = new StreamWriter(_raceHorseStream, Encoding.GetEncoding("shift_jis")))
            using (var _raceHorseWeightStream = new FileStream(Path.Combine(outDir, "race_horse_weights.csv"), FileMode.Create))
            using (var raceHorseWeightStream = new StreamWriter(_raceHorseWeightStream, Encoding.GetEncoding("shift_jis")))
            using (var _racePrizeStream = new FileStream(Path.Combine(outDir, "race_prizes.csv"), FileMode.Create))
            using (var racePrizeStream = new StreamWriter(_racePrizeStream, Encoding.GetEncoding("shift_jis")))
            using (var _raceConditionStream = new FileStream(Path.Combine(outDir, "race_conditions.csv"), FileMode.Create))
            using (var raceConditionStream = new StreamWriter(_raceConditionStream, Encoding.GetEncoding("shift_jis")))
            using (var _weatherStream = new FileStream(Path.Combine(outDir, "weathers.csv"), FileMode.Create))
            using (var weatherStream = new StreamWriter(_weatherStream, Encoding.GetEncoding("shift_jis")))
            {
                raceStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "racenum",
                    "youbicd",
                    "tokunum",
                    "hondai",
                    "fukudai",
                    "kakko",
                    "hondaieng",
                    "fukudaieng",
                    "kakkoeng",
                    "ryakusyo10",
                    "ryakusyo6",
                    "ryakusyo3",
                    "kubun",
                    "nkai",
                    "gradecd",
                    "gradecdbefore",
                    "syubetucd",
                    "kigocd",
                    "jyuryocd",
                    "jyokenname",
                    "kyori",
                    "kyoribefore",
                    "trackcd",
                    "trackcdbefore",
                    "coursekubuncd",
                    "coursekubuncdbefore",
                    "hassotime",
                    "hassotimebefore",
                    "torokutosu",
                    "syussotosu",
                    "nyusentosu",
                    "tenkocd",
                    "sibababacd",
                    "dirtbabacd",
                    "syogaimiletime",
                    "harontimes3",
                    "harontimes4",
                    "harontimel3",
                    "harontimel4",
                    "recordupkubun"
                }));

                raceHorseStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "racenum",
                    "wakuban",
                    "umaban",
                    "kettonum",
                    "bamei",
                    "umakigocd",
                    "sexcd",
                    "hinsyucd",
                    "keirocd",
                    "barei",
                    "tozaicd",
                    "chokyosicode",
                    "chokyosiryakusyo",
                    "banusicode",
                    "banusiname",
                    "fukusyoku",
                    "reserved1",
                    "futan",
                    "futanbefore",
                    "blinker",
                    "reserved2",
                    "kisyucode",
                    "kisyucodebefore",
                    "kisyuryakusyo",
                    "kisyuryakusyobefore",
                    "minaraicd",
                    "minaraicdbefore",
                    "bataijyu",
                    "zogenfugo",
                    "zogensa",
                    "ijyocd",
                    "nyusenjyuni",
                    "kakuteijyuni",
                    "dochakukubun",
                    "dochakutosu",
                    "time",
                    "chakusacd",
                    "chakusacdp",
                    "chakusacdpp",
                    "jyuni1c",
                    "jyuni2c",
                    "jyuni3c",
                    "jyuni4c",
                    "odds",
                    "ninki",
                    "honsyokin",
                    "fukasyokin",
                    "reserved3",
                    "reserved4",
                    "harontimel4",
                    "harontimel3",
                    "timediff",
                    "recordupkubun",
                    "dmkubun",
                    "dmtime",
                    "dmgosap",
                    "dmgosam",
                    "dmjyuni",
                    "kyakusitukubun"
                }));

                raceConditionStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "racenum",
                    "type",
                    "jyokencd"
                }));

                racePrizeStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "racenum",
                    "type",
                    "rank",
                    "prize"
                }));

                raceHorseWeightStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "racenum",
                    "happyotimemonth",
                    "happyotimeday",
                    "happyotimehour",
                    "happyotimeminute",
                    "umaban",
                    "bamei",
                    "bataijyu",
                    "zogenfugo",
                    "zogensa"
                }));

                weatherStream.WriteLine(string.Join(",", new[]{
                    "recordspec",
                    "datakubun",
                    "makeyear",
                    "makemonth",
                    "makeday",
                    "year",
                    "monthday",
                    "jyocd",
                    "kaiji",
                    "nichiji",
                    "happyotimemonth",
                    "happyotimeday",
                    "happyotimehour",
                    "happyotimeminute",
                    "henkoid",
                    "tenkobabatenkocd",
                    "tenkobabasibababacd",
                    "tenkobabadirtbabacd",
                    "tenkobababeforetenkocd",
                    "tenkobababeforesibababacd",
                    "tenkobababeforedirtbabacd"
                }));

                var wips = new List<string>();

                do
                {
                    var result = JVLink.JVRead(out strBuff, out nBuffSize, out strFileName);

                    switch (result)
                    {
                        case 0: // 全ファイル読み込み終了
                            prgDownload.Value = prgDownload.Maximum;
                            prgJVRead.Value = prgJVRead.Maximum;
                            Console.WriteLine("全ファイル読み込み終了: " + prgJVRead.Value + " / " + prgJVRead.Maximum);
                            flg_exit = true;
                            break;
                        case -1: // ファイル切り替わり
                            prgJVRead.Value = prgJVRead.Value + 1;
                            Console.WriteLine("ファイル切り替わり: " + prgJVRead.Value + " / " + prgJVRead.Maximum);
                            break;
                        case -3: // ダウンロード中
                            prgDownload.Value = JVLink.JVStatus();
                            Console.WriteLine("ダウンロード中: " + prgDownload.Value + " / " + prgDownload.Maximum);
                            break;
                        case -201: // JVInit されてない
                            Console.WriteLine("JVInit が行われていません。");
                            flg_exit = true;
                            break;
                        case -203: // JVOpen されてない
                            Console.WriteLine("JVOpen が行われていません。");
                            flg_exit = true;
                            break;
                        case -402:
                            Console.WriteLine("ダウンロードしたファイルが異常（ファイルサイズ＝０）");
                            Console.WriteLine(strFileName + "を削除します。");
                            JVLink.JVFiledelete(strFileName);
                            Console.WriteLine("プログラムを再度実行してください。");
                            flg_exit = true;
                            break;
                        case -403:
                            Console.WriteLine("ダウンロードしたファイルが異常（データ内容）");
                            Console.WriteLine(strFileName + "を削除します。");
                            JVLink.JVFiledelete(strFileName);
                            Console.WriteLine("プログラムを再度実行してください。");
                            flg_exit = true;
                            break;
                        case -502:
                            Console.WriteLine("ダウンロード失敗（通信エラーやディスクエラーなど）");
                            flg_exit = true;
                            break;
                        case -503: // ファイルがない
                            Console.WriteLine(strFileName + "が存在しません。");
                            flg_exit = true;
                            break;
                        case int ret when ret > 0:
                            /**
                             * # データ種別一覧
                             * - レース詳細(“RA”)
                             * - 馬毎レース情報(“SE”)
                             * - 競走馬(“UM”)
                             * - 騎手(“KS”)
                             * - 調教師(“CH”)
                             * - 生産者(“BR”)
                             * - 馬主(“BN”)
                             * - レコード(“RC”)
                             */
                            var recordSpec = strBuff.Substring(0, 2);  // データ種別
                            switch (recordSpec)
                            {
                                case "RA":
                                    raceInfo.SetDataB(ref strBuff);
                                    switch (raceInfo.head.DataKubun)
                                    {
                                        case "1":
                                        case "2":
                                        case "3":
                                        case "4":
                                        case "5":
                                        case "6":
                                            // 速報系
                                            if (option == JVOpenOption.ThisWeek)
                                                // JVOpenOption.ThisWeek なら JVRTOpen に渡す
                                                wips.Add(raceInfo.id.Year + raceInfo.id.MonthDay);
                                            break;
                                    }
                                    Write(raceInfo, raceStream, raceConditionStream, racePrizeStream);
                                    break;
                                case "SE":
                                    raceUmaInfo.SetDataB(ref strBuff);
                                    Write(raceUmaInfo, raceHorseStream);
                                    break;
                                default:
                                    // 読み飛ばし
                                    Console.WriteLine("読み飛ばし: " + recordSpec);
                                    prgJVRead.Value = prgJVRead.Value + 1;
                                    JVLink.JVSkip();
                                    break;
                            }
                            Console.WriteLine(recordSpec + ": " + prgJVRead.Value + " / " + prgJVRead.Maximum);
                            break;
                        default:
                            Console.WriteLine("不明な JVRead 戻り値: " + result);
                            break;
                    }
                }
                while (!flg_exit);

                JVLink.JVClose();

                foreach (var wip in wips.Distinct().OrderBy(x => x))
                {
                    // 速報レース情報 (出走馬名表～)
                    dataspec = "0B15";
                    r = JVLink.JVRTOpen(dataspec, wip);

                    if (r == 0)
                    {

                        strBuff = new string('\0', nBuffSize);      // JVRead: データ格納バッファ
                        strFileName = new string('\0', nNameSize);  // JVRead: 読み込み中ファイル名

                        while (true)
                        {
                            r = JVLink.JVRead(out strBuff, out nBuffSize, out strFileName);

                            if (r > 0)
                            {
                                var recordSpec = strBuff.Substring(0, 2);

                                if (recordSpec == "RA")
                                {
                                    raceInfo.SetDataB(ref strBuff);
                                    Write(raceInfo, raceStream, raceConditionStream, racePrizeStream);
                                }
                                else if (recordSpec == "SE")
                                {
                                    raceUmaInfo.SetDataB(ref strBuff);
                                    Write(raceUmaInfo, raceHorseStream);
                                }
                                else
                                {
                                    Console.WriteLine("読み飛ばし: " + recordSpec);
                                    Console.WriteLine();
                                    JVLink.JVSkip();
                                }
                            }
                            else if (r == 0)
                            {
                                Console.WriteLine("全ファイル読み込み終了");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -1)
                            {
                                Console.WriteLine("ファイル切り替わり");
                                Console.WriteLine();
                            }
                            else if (r == -3)
                            {
                                Console.WriteLine("ダウンロード中: " + JVLink.JVStatus());
                                Console.WriteLine();
                            }
                            else if (r == -201)
                            {
                                Console.WriteLine("JVInit が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -203)
                            {
                                Console.WriteLine("JVOpen が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -503)
                            {
                                Console.WriteLine(strFileName + "が存在しません。");
                                Console.WriteLine();
                                break;
                            }
                            else
                            {
                                Console.WriteLine("不明なエラー(" + r + ")");
                                Console.WriteLine();
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(dataspec + ": データが存在しません。 (" + wip + ")");
                    }

                    JVLink.JVClose();

                    // 速報馬体重
                    dataspec = "0B11";
                    r = JVLink.JVRTOpen(dataspec, wip);

                    if (r == 0)
                    {
                        strBuff = new string('\0', nBuffSize);      // JVRead: データ格納バッファ
                        strFileName = new string('\0', nNameSize);  // JVRead: 読み込み中ファイル名

                        while (true)
                        {
                            r = JVLink.JVRead(out strBuff, out nBuffSize, out strFileName);

                            if (r > 0)
                            {
                                var recordSpec = strBuff.Substring(0, 2);

                                if (recordSpec == "WH")
                                {
                                    bataijyuInfo.SetDataB(ref strBuff);
                                    Write(bataijyuInfo, raceHorseWeightStream);
                                }
                                else
                                {
                                    Console.WriteLine("読み飛ばし: " + recordSpec);
                                    Console.WriteLine();
                                    JVLink.JVSkip();
                                }
                            }
                            else if (r == 0)
                            {
                                Console.WriteLine("全ファイル読み込み終了");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -1)
                            {
                                Console.WriteLine("ファイル切り替わり");
                                Console.WriteLine();
                            }
                            else if (r == -3)
                            {
                                Console.WriteLine("ダウンロード中: " + JVLink.JVStatus());
                                Console.WriteLine();
                            }
                            else if (r == -201)
                            {
                                Console.WriteLine("JVInit が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -203)
                            {
                                Console.WriteLine("JVOpen が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -503)
                            {
                                Console.WriteLine(strFileName + "が存在しません。");
                                Console.WriteLine();
                                break;
                            }
                            else
                            {
                                Console.WriteLine("不明なエラー(" + r + ")");
                                Console.WriteLine();
                                break;
                            }
                        }

                    }
                    else
                    {
                        Console.WriteLine(dataspec + ": データが存在しません。 (" + wip + ")");
                    }

                    JVLink.JVClose();

                    // 速報開催情報 (一括)
                    dataspec = "0B14";
                    r = JVLink.JVRTOpen(dataspec, wip);

                    if (r == 0)
                    {

                        strBuff = new string('\0', nBuffSize);      // JVRead: データ格納バッファ
                        strFileName = new string('\0', nNameSize);  // JVRead: 読み込み中ファイル名

                        while (true)
                        {
                            r = JVLink.JVRead(out strBuff, out nBuffSize, out strFileName);

                            if (r > 0)
                            {
                                var recordSpec = strBuff.Substring(0, 2);

                                if (recordSpec == "WE")
                                {
                                    weatherInfo.SetDataB(ref strBuff);
                                    Write(weatherInfo, weatherStream);
                                }
                                else
                                {
                                    Console.WriteLine("読み飛ばし: " + recordSpec);
                                    Console.WriteLine();
                                    JVLink.JVSkip();
                                }
                            }
                            else if (r == 0)
                            {
                                Console.WriteLine("全ファイル読み込み終了");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -1)
                            {
                                Console.WriteLine("ファイル切り替わり");
                                Console.WriteLine();
                            }
                            else if (r == -3)
                            {
                                Console.WriteLine("ダウンロード中: " + JVLink.JVStatus());
                                Console.WriteLine();
                            }
                            else if (r == -201)
                            {
                                Console.WriteLine("JVInit が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -203)
                            {
                                Console.WriteLine("JVOpen が行われていません。");
                                Console.WriteLine();
                                break;
                            }
                            else if (r == -503)
                            {
                                Console.WriteLine(strFileName + "が存在しません。");
                                Console.WriteLine();
                                break;
                            }
                            else
                            {
                                Console.WriteLine("不明なエラー(" + r + ")");
                                Console.WriteLine();
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(dataspec + ": データが存在しません。 (" + wip + ")");
                    }

                    JVLink.JVClose();
                }
            }
        }

        static void Write(JV_RA_RACE info, StreamWriter s, StreamWriter condS, StreamWriter prizeS)
        {
            s.Write(info.head.RecordSpec);
            s.Write(',');
            s.Write(info.head.DataKubun);
            s.Write(',');
            s.Write(info.head.MakeDate.Year);
            s.Write(',');
            s.Write(info.head.MakeDate.Month);
            s.Write(',');
            s.Write(info.head.MakeDate.Day);
            s.Write(',');
            s.Write(info.id.Year);
            s.Write(',');
            s.Write(info.id.MonthDay);
            s.Write(',');
            s.Write(info.id.JyoCD);
            s.Write(',');
            s.Write(info.id.Kaiji);
            s.Write(',');
            s.Write(info.id.Nichiji);
            s.Write(',');
            s.Write(info.id.RaceNum);
            s.Write(',');
            s.Write(info.RaceInfo.YoubiCD);
            s.Write(',');
            s.Write(info.RaceInfo.TokuNum);
            s.Write(',');
            s.Write(info.RaceInfo.Hondai);
            s.Write(',');
            s.Write(info.RaceInfo.Fukudai);
            s.Write(',');
            s.Write(info.RaceInfo.Kakko);
            s.Write(',');
            s.Write(info.RaceInfo.HondaiEng);
            s.Write(',');
            s.Write(info.RaceInfo.FukudaiEng);
            s.Write(',');
            s.Write(info.RaceInfo.KakkoEng);
            s.Write(',');
            s.Write(info.RaceInfo.Ryakusyo10);
            s.Write(',');
            s.Write(info.RaceInfo.Ryakusyo6);
            s.Write(',');
            s.Write(info.RaceInfo.Ryakusyo3);
            s.Write(',');
            s.Write(info.RaceInfo.Kubun);
            s.Write(',');
            s.Write(info.RaceInfo.Nkai);
            s.Write(',');
            s.Write(info.GradeCD);
            s.Write(',');
            s.Write(info.GradeCDBefore);
            s.Write(',');
            s.Write(info.JyokenInfo.SyubetuCD);
            s.Write(',');
            s.Write(info.JyokenInfo.KigoCD);
            s.Write(',');
            s.Write(info.JyokenInfo.JyuryoCD);
            s.Write(',');

            for (var i = 0; i < info.JyokenInfo.JyokenCD.Length; i++)
            {
                condS.Write(info.head.RecordSpec);
                condS.Write(',');
                condS.Write(info.head.DataKubun);
                condS.Write(',');
                condS.Write(info.head.MakeDate.Year);
                condS.Write(',');
                condS.Write(info.head.MakeDate.Month);
                condS.Write(',');
                condS.Write(info.head.MakeDate.Day);
                condS.Write(',');
                condS.Write(info.id.Year);
                condS.Write(',');
                condS.Write(info.id.MonthDay);
                condS.Write(',');
                condS.Write(info.id.JyoCD);
                condS.Write(',');
                condS.Write(info.id.Kaiji);
                condS.Write(',');
                condS.Write(info.id.Nichiji);
                condS.Write(',');
                condS.Write(info.id.RaceNum);
                condS.Write(',');
                condS.Write(i + 1);
                condS.Write(',');
                condS.Write(info.JyokenInfo.JyokenCD[i]);
                condS.WriteLine();
            }

            s.Write(info.JyokenName);
            s.Write(',');
            s.Write(info.Kyori);
            s.Write(',');
            s.Write(info.KyoriBefore);
            s.Write(',');
            s.Write(info.TrackCD);
            s.Write(',');
            s.Write(info.TrackCDBefore);
            s.Write(',');
            s.Write(info.CourseKubunCD);
            s.Write(',');
            s.Write(info.CourseKubunCDBefore);
            s.Write(',');

            for (var i = 0; i < info.Honsyokin.Length; i++)
            {
                prizeS.Write(info.head.RecordSpec);
                prizeS.Write(',');
                prizeS.Write(info.head.DataKubun);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Year);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Month);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Day);
                prizeS.Write(',');
                prizeS.Write(info.id.Year);
                prizeS.Write(',');
                prizeS.Write(info.id.MonthDay);
                prizeS.Write(',');
                prizeS.Write(info.id.JyoCD);
                prizeS.Write(',');
                prizeS.Write(info.id.Kaiji);
                prizeS.Write(',');
                prizeS.Write(info.id.Nichiji);
                prizeS.Write(',');
                prizeS.Write(info.id.RaceNum);
                prizeS.Write(',');
                prizeS.Write('1');  // 本賞金
                prizeS.Write(',');
                prizeS.Write(i + 1);  // 着順
                prizeS.Write(',');
                prizeS.Write(info.Honsyokin[i]);
                prizeS.WriteLine();
            }

            for (var i = 0; i < info.Fukasyokin.Length; i++)
            {
                prizeS.Write(info.head.RecordSpec);
                prizeS.Write(',');
                prizeS.Write(info.head.DataKubun);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Year);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Month);
                prizeS.Write(',');
                prizeS.Write(info.head.MakeDate.Day);
                prizeS.Write(',');
                prizeS.Write(info.id.Year);
                prizeS.Write(',');
                prizeS.Write(info.id.MonthDay);
                prizeS.Write(',');
                prizeS.Write(info.id.JyoCD);
                prizeS.Write(',');
                prizeS.Write(info.id.Kaiji);
                prizeS.Write(',');
                prizeS.Write(info.id.Nichiji);
                prizeS.Write(',');
                prizeS.Write(info.id.RaceNum);
                prizeS.Write(',');
                prizeS.Write('2');  // 付加賞金
                prizeS.Write(',');
                prizeS.Write(i + 1);  // 着順
                prizeS.Write(',');
                prizeS.Write(info.Fukasyokin[i]);
                prizeS.WriteLine();
            }

            s.Write(info.HassoTime);
            s.Write(',');
            s.Write(info.HassoTimeBefore);
            s.Write(',');
            s.Write(info.TorokuTosu);
            s.Write(',');
            s.Write(info.SyussoTosu);
            s.Write(',');
            s.Write(info.NyusenTosu);
            s.Write(',');
            s.Write(info.TenkoBaba.TenkoCD);
            s.Write(',');
            s.Write(info.TenkoBaba.SibaBabaCD);
            s.Write(',');
            s.Write(info.TenkoBaba.DirtBabaCD);
            s.Write(',');

            /*foreach (var a in info.LapTime)
            {
                Console.WriteLine("LapTime " + a);
            }*/

            s.Write(info.SyogaiMileTime);
            s.Write(',');
            s.Write(info.HaronTimeS3);
            s.Write(',');
            s.Write(info.HaronTimeS4);
            s.Write(',');
            s.Write(info.HaronTimeL3);
            s.Write(',');
            s.Write(info.HaronTimeL4);
            s.Write(',');

            /*foreach (var a in info.CornerInfo)
            {
                Console.WriteLine("CornerInfo " + a);
            }*/

            s.Write(info.RecordUpKubun);
            s.WriteLine();
        }

        static void Write(JV_SE_RACE_UMA info, StreamWriter s)
        {
            s.Write(info.head.RecordSpec);
            s.Write(',');
            s.Write(info.head.DataKubun);
            s.Write(',');
            s.Write(info.head.MakeDate.Year);
            s.Write(',');
            s.Write(info.head.MakeDate.Month);
            s.Write(',');
            s.Write(info.head.MakeDate.Day);
            s.Write(',');
            s.Write(info.id.Year);
            s.Write(',');
            s.Write(info.id.MonthDay);
            s.Write(',');
            s.Write(info.id.JyoCD);
            s.Write(',');
            s.Write(info.id.Kaiji);
            s.Write(',');
            s.Write(info.id.Nichiji);
            s.Write(',');
            s.Write(info.id.RaceNum);
            s.Write(',');
            s.Write(info.Wakuban);
            s.Write(',');
            s.Write(info.Umaban);
            s.Write(',');
            s.Write(info.KettoNum);
            s.Write(',');
            s.Write(info.Bamei);
            s.Write(',');
            s.Write(info.UmaKigoCD);
            s.Write(',');
            s.Write(info.SexCD);
            s.Write(',');
            s.Write(info.HinsyuCD);
            s.Write(',');
            s.Write(info.KeiroCD);
            s.Write(',');
            s.Write(info.Barei);
            s.Write(',');
            s.Write(info.TozaiCD);
            s.Write(',');
            s.Write(info.ChokyosiCode);
            s.Write(',');
            s.Write(info.ChokyosiRyakusyo);
            s.Write(',');
            s.Write(info.BanusiCode);
            s.Write(',');
            s.Write(info.BanusiName);
            s.Write(',');
            s.Write(info.Fukusyoku);
            s.Write(',');
            s.Write(info.reserved1);
            s.Write(',');
            s.Write(info.Futan);
            s.Write(',');
            s.Write(info.FutanBefore);
            s.Write(',');
            s.Write(info.Blinker);
            s.Write(',');
            s.Write(info.reserved2);
            s.Write(',');
            s.Write(info.KisyuCode);
            s.Write(',');
            s.Write(info.KisyuCodeBefore);
            s.Write(',');
            s.Write(info.KisyuRyakusyo);
            s.Write(',');
            s.Write(info.KisyuRyakusyoBefore);
            s.Write(',');
            s.Write(info.MinaraiCD);
            s.Write(',');
            s.Write(info.MinaraiCDBefore);
            s.Write(',');
            s.Write(info.BaTaijyu);
            s.Write(',');
            s.Write(info.ZogenFugo);
            s.Write(',');
            s.Write(info.ZogenSa);
            s.Write(',');
            s.Write(info.IJyoCD);
            s.Write(',');
            s.Write(info.NyusenJyuni);
            s.Write(',');
            s.Write(info.KakuteiJyuni);
            s.Write(',');
            s.Write(info.DochakuKubun);
            s.Write(',');
            s.Write(info.DochakuTosu);
            s.Write(',');
            s.Write(info.Time);
            s.Write(',');
            s.Write(info.ChakusaCD);
            s.Write(',');
            s.Write(info.ChakusaCDP);
            s.Write(',');
            s.Write(info.ChakusaCDPP);
            s.Write(',');
            s.Write(info.Jyuni1c);
            s.Write(',');
            s.Write(info.Jyuni2c);
            s.Write(',');
            s.Write(info.Jyuni3c);
            s.Write(',');
            s.Write(info.Jyuni4c);
            s.Write(',');
            s.Write(info.Odds);
            s.Write(',');
            s.Write(info.Ninki);
            s.Write(',');
            s.Write(info.Honsyokin);
            s.Write(',');
            s.Write(info.Fukasyokin);
            s.Write(',');
            s.Write(info.reserved3);
            s.Write(',');
            s.Write(info.reserved4);
            s.Write(',');
            s.Write(info.HaronTimeL4);
            s.Write(',');
            s.Write(info.HaronTimeL3);
            s.Write(',');

            /*Console.WriteLine(
                info.id.Year + info.id.MonthDay + info.id.JyoCD + info.id.Kaiji + info.id.Nichiji + info.id.RaceNum + " " +
                info.KakuteiJyuni + "着 " + info.Bamei);
            foreach (var a in info.ChakuUmaInfo)
            {
                Console.WriteLine("ChakuUmaInfoに入ってる馬は" + a.Bamei);
            }*/

            s.Write(info.TimeDiff);
            s.Write(',');
            s.Write(info.RecordUpKubun);
            s.Write(',');
            s.Write(info.DMKubun);
            s.Write(',');
            s.Write(info.DMTime);
            s.Write(',');
            s.Write(info.DMGosaP);
            s.Write(',');
            s.Write(info.DMGosaM);
            s.Write(',');
            s.Write(info.DMJyuni);
            s.Write(',');
            s.Write(info.KyakusituKubun);
            s.WriteLine();
        }

        static void Write(JV_WH_BATAIJYU info, StreamWriter s)
        {
            foreach (var uma in info.BataijyuInfo)
            {
                s.Write(info.head.RecordSpec);
                s.Write(',');
                s.Write(info.head.DataKubun);
                s.Write(',');
                s.Write(info.head.MakeDate.Year);
                s.Write(',');
                s.Write(info.head.MakeDate.Month);
                s.Write(',');
                s.Write(info.head.MakeDate.Day);
                s.Write(',');
                s.Write(info.id.Year);
                s.Write(',');
                s.Write(info.id.MonthDay);
                s.Write(',');
                s.Write(info.id.JyoCD);
                s.Write(',');
                s.Write(info.id.Kaiji);
                s.Write(',');
                s.Write(info.id.Nichiji);
                s.Write(',');
                s.Write(info.id.RaceNum);
                s.Write(',');
                s.Write(info.HappyoTime.Month);
                s.Write(',');
                s.Write(info.HappyoTime.Day);
                s.Write(',');
                s.Write(info.HappyoTime.Hour);
                s.Write(',');
                s.Write(info.HappyoTime.Minute);
                s.Write(',');
                s.Write(uma.Umaban);
                s.Write(',');
                s.Write(uma.Bamei);
                s.Write(',');
                s.Write(uma.BaTaijyu);
                s.Write(',');
                s.Write(uma.ZogenFugo);
                s.Write(',');
                s.Write(uma.ZogenSa);
                s.WriteLine();
            }
        }

        static void Write(JV_WE_WEATHER info, StreamWriter s)
        {
            s.Write(info.head.RecordSpec);
            s.Write(',');
            s.Write(info.head.DataKubun);
            s.Write(',');
            s.Write(info.head.MakeDate.Year);
            s.Write(',');
            s.Write(info.head.MakeDate.Month);
            s.Write(',');
            s.Write(info.head.MakeDate.Day);
            s.Write(',');
            s.Write(info.id.Year);
            s.Write(',');
            s.Write(info.id.MonthDay);
            s.Write(',');
            s.Write(info.id.JyoCD);
            s.Write(',');
            s.Write(info.id.Kaiji);
            s.Write(',');
            s.Write(info.id.Nichiji);
            s.Write(',');
            s.Write(info.HappyoTime.Month);
            s.Write(',');
            s.Write(info.HappyoTime.Day);
            s.Write(',');
            s.Write(info.HappyoTime.Hour);
            s.Write(',');
            s.Write(info.HappyoTime.Minute);
            s.Write(',');
            s.Write(info.HenkoID);
            s.Write(',');
            s.Write(info.TenkoBaba.TenkoCD);
            s.Write(',');
            s.Write(info.TenkoBaba.SibaBabaCD);
            s.Write(',');
            s.Write(info.TenkoBaba.DirtBabaCD);
            s.Write(',');
            s.Write(info.TenkoBabaBefore.TenkoCD);
            s.Write(',');
            s.Write(info.TenkoBabaBefore.SibaBabaCD);
            s.Write(',');
            s.Write(info.TenkoBabaBefore.DirtBabaCD);
            s.WriteLine();
        }
    }
}
