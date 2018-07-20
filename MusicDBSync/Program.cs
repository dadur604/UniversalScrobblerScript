using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MusicDBSync {
    class Program {

        public static List<string> batchGMP;
        public static List<string> batchString;
        public static List<TrackObject> NameList;

        public static string MBInput = @"C:\Users\dadur\Documents\New Text Document.txt";


        static void Main(string[] args) {

            foreach (var process in Process.GetProcessesByName("chrome")) {
                process.Kill();
            }

            batchGMP = new List<string>();

            batchString = new List<string>();

            NameList = TrackArtistList();

            GetMBData();
            File.WriteAllLines(Path.GetDirectoryName(MBInput) + "\\outmb.txt", batchString.ToArray());

            GetGMP();
            File.WriteAllLines(Path.GetDirectoryName(MBInput) + "\\outgmp.txt", batchGMP.ToArray());
            Console.WriteLine("Done");
        }

        private static void GetMBData() {
            var filelines = File.ReadAllLines(MBInput);
            foreach (var line in filelines) {
                // Artist - Track - Album - Album Artist - Duration - Play Count
                var match = Regex.Match(line, "(.+) - (.+) - (.*) - (.+) - (.*) - (.*)");
                if (match.Success) {
                    var artist = match.Groups[1].Value;
                    var track = match.Groups[2].Value;
                    var album = match.Groups[3].Value;
                    var aartist = match.Groups[4].Value;
                    var duration = match.Groups[5].Value;
                    var durationsec = (int.Parse(duration.Split(':')[0]) * 60) + int.Parse(duration.Split(':')[1]);
                    for (int i = 0; i < int.Parse(match.Groups[6].Value); i++) {
                        batchString.Add($"\"{artist}\", \"{track}\", \"{album}\", \"{aartist}\", \"{duration}\"");
                    }
                }
            }
        }

        private static void GetGMP() {

            RunGMP(new DataReceivedEventHandler((s, e) => {
                HandleData(e);
                
            }));
            }

        private static void HandleData(DataReceivedEventArgs e) {
            var match = Regex.Match(e.Data, "Listened to (.+) by (.+) on (.+)");
            if (match.Success) {
                HandleNewTrack(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
            }

        }

        private static List<TrackObject> TrackArtistList() {
            List<TrackObject> output = new List<TrackObject>();
            var filelines = File.ReadAllLines(MBInput);
            foreach (var line in filelines) {
                // Artist - Track - Album - Album Artist - Duration - Play Count
                var match = Regex.Match(line, "(.+) - (.+) - (.*) - (.+) - (.*) - (.*)");
                if (match.Success) {
                    var artist = match.Groups[1].Value;
                    var track = match.Groups[2].Value;
                    var album = match.Groups[3].Value;
                    var aartist = match.Groups[4].Value;
                    var duration = match.Groups[5].Value;
                    var durationsec = (int.Parse(duration.Split(':')[0]) * 60) + int.Parse(duration.Split(':')[1]);
                    var obj = new TrackObject($"{artist} - {track}", $"\"{artist}\", \"{track}\", \"{album}\", \"{aartist}\", \"{duration}\"");
                    output.Add(obj);
                }
            }

            return output;
        }

        private static void HandleNewTrack(string Title, string Artist, string _DateTime) {
            Console.WriteLine(Title, Artist);
            var found = NameList.OrderBy((x) => LevenshteinDistance.Compute(x.Short, (Artist + " - " + Title))).First();
            batchGMP.Add(found.Long);
            File.AppendAllLines(Path.GetDirectoryName(MBInput) + "\\outgmp.txt", new string[] { found.Long });
            Console.WriteLine(found.Long);
        }


        private static void RunGMP(DataReceivedEventHandler listener) {
            Process cmd = SetupCMD("");

            cmd.OutputDataReceived += listener;
            cmd.ErrorDataReceived += listener;

            cmd.Start();
            cmd.StandardInput.WriteLine($@"go run {AppDomain.CurrentDomain.BaseDirectory}main.go get");
            cmd.BeginOutputReadLine();
            cmd.BeginErrorReadLine();

            cmd.WaitForExit();
        
        }

        private static Process SetupCMD(string filepath) {
            List<string> cmds = new List<string>();

            //           "\\\\?\\" + 
            string bdir = AppDomain.CurrentDomain.BaseDirectory;


            Process cmd = new Process();

            cmd.StartInfo.FileName = $@"cmd";

            cmd.StartInfo.WorkingDirectory = @"C:\Windows\System32";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            

            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;

            return cmd;
        }
    }

    static class LevenshteinDistance {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t) {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0) {
                return m;
            }

            if (m == 0) {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) {
            }

            for (int j = 0; j <= m; d[0, j] = j++) {
            }

            // Step 3
            for (int i = 1; i <= n; i++) {
                //Step 4
                for (int j = 1; j <= m; j++) {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }

}
