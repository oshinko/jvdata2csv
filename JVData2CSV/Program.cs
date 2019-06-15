using System;
using System.IO;
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

            var fromtime = DateTime.Now.AddYears(-1).ToString(FromtimeFormat);
            var outDir = ".";

            if (args.Length > 0)
            {
                fromtime = args[0].
                    Trim().
                    Replace("-", "").
                    Replace("/", "").
                    Replace(":", "").
                    PadRight(FromtimeFormat.Length, '0');

                if (args.Length > 1)
                {
                    outDir = args[1];
                }
            }

            Console.WriteLine("Read data since " + fromtime);
            Console.WriteLine("Write to " + outDir);

            Execute(fromtime, outDir);
        }

        static void Execute(string fromtime, string outDir)
        {
            JVLink.JVInit(UserAgent);

            var nReadCount = 0;             // JVOpen: 総読み込みファイル数
            var nDownloadCount = 0;         // JVOpen: 総ダウンロードファイル数
            var strLastFileTimestamp = "";  // JVOpen: 最新ファイルのタイムスタンプ
            var prgDownload = new ProgressBar();
            var prgJVRead = new ProgressBar();

            JVLink.JVOpen("RACE", fromtime, 4, ref nReadCount, ref nDownloadCount, out strLastFileTimestamp);

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
            bool flg_exit = false;

            using (var _raceStream = new FileStream(Path.Combine(outDir, "races.csv"), FileMode.Create))
            using (var raceStream = new StreamWriter(_raceStream, Encoding.GetEncoding("shift_jis")))
            using (var _raceHorseStream = new FileStream(Path.Combine(outDir, "race_horses.csv"), FileMode.Create))
            using (var raceHorseStream = new StreamWriter(_raceHorseStream, Encoding.GetEncoding("shift_jis")))
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

                do
                {
                    switch (JVLink.JVRead(out strBuff, out nBuffSize, out strFileName))
                    {
                        case 0: // 全ファイル読み込み終了
                            prgDownload.Value = prgDownload.Maximum;
                            prgJVRead.Value = prgJVRead.Maximum;
                            Console.WriteLine("全ファイル読み込み終了: " + prgJVRead.Value + " / " + prgJVRead.Maximum + "\n");
                            flg_exit = true;
                            break;
                        case -1: // ファイル切り替わり
                            prgJVRead.Value = prgJVRead.Value + 1;
                            Console.WriteLine("ファイル切り替わり: " + prgJVRead.Value + " / " + prgJVRead.Maximum + "\n");
                            break;
                        case -3: // ダウンロード中
                            prgDownload.Value = JVLink.JVStatus();
                            Console.WriteLine("ダウンロード中: " + prgDownload.Value + " / " + prgDownload.Maximum + "\n");
                            break;
                        case -201: // JVInit されてない
                            Console.WriteLine("JVInit が行われていません。");
                            flg_exit = true;
                            break;
                        case -203: // JVOpen されてない
                            Console.WriteLine("JVOpen が行われていません。");
                            flg_exit = true;
                            break;
                        case -503: // ファイルがない
                            Console.WriteLine(strFileName + "が存在しません。");
                            flg_exit = true;
                            break;
                        case int ret when ret > 0:
                            switch (strBuff.Substring(0, 2))
                            {
                                case "RA":
                                    raceInfo.SetDataB(ref strBuff);
                                    Write(raceInfo, raceStream);
                                    break;
                                case "SE":
                                    raceUmaInfo.SetDataB(ref strBuff);
                                    Write(raceUmaInfo, raceHorseStream);
                                    break;
                                default:
                                    // 読み飛ばし
                                    prgJVRead.Value = prgJVRead.Value + 1;
                                    Console.WriteLine("読み飛ばし: " + strBuff.Substring(0, 2) + " " + prgJVRead.Value + " / " + prgJVRead.Maximum + "\n");
                                    JVLink.JVSkip();
                                    break;
                            }
                            break;
                    }
                }
                while (!flg_exit);
            }

            Console.WriteLine("exit");

            JVLink.JVClose();
        }

        static void Write(JV_RA_RACE info, StreamWriter s)
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

            /*Console.WriteLine(
                info.id.Year + info.id.MonthDay + info.id.JyoCD + info.id.Kaiji + info.id.Nichiji + info.id.RaceNum + " " +
                info.RaceInfo.Ryakusyo10);
            foreach (var a in info.JyokenInfo.JyokenCD)
            {
                Console.WriteLine("JyokenCD " + a);
            }*/

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

            /*foreach (var a in info.Honsyokin)
            {
                Console.WriteLine("Honsyokin " + a);
            }

            foreach (var a in info.HonsyokinBefore)
            {
                Console.WriteLine("HonsyokinBefore " + a);
            }

            foreach (var a in info.Fukasyokin)
            {
                Console.WriteLine("Fukasyokin " + a);
            }

            foreach (var a in info.FukasyokinBefore)
            {
                Console.WriteLine("FukasyokinBefore " + a);
            }*/

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
    }
}
