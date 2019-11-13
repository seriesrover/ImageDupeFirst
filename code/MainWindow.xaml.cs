using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace ImageDupeFirst
{
    public class LogInfo
    {
        public String LeftPath { get; set; }
        public UInt64 LeftId { get; set; }
        public String RightPath { get; set; }
        public UInt64 RightId { get; set; }

        public LogInfo()
        {
        }
    }

    public partial class MainWindow : Window
    {
        private const String LOGFILE = "D:\\log.txt";
        private const String MATCHFILE = "D:\\match.txt";

        List<LogInfo> logInfos = null;

        int currentInfo = -1;
        int previousInfo = -1;

        static void MatchLog(UInt64 leftId, UInt64 rightId)
        {
            using (StreamWriter w = File.AppendText(MATCHFILE))
            {
                w.Write("\r\n{0}\t{1}", leftId, rightId);
            }
        }

        static List<LogInfo> GetLogInfos(String imagePath)
        {
            if (!File.Exists(LOGFILE)) return null;

            List<LogInfo> ids = new List<LogInfo>();
            char[] pattern = new char[] { '\t' };

            foreach (String s in File.ReadAllLines(LOGFILE))
            {
                string[] sLine = s.Split(pattern);

                if (sLine.Count() < 4) continue;

                LogInfo l = new LogInfo();
                l.LeftPath = imagePath + sLine[0];
                l.RightPath = imagePath + sLine[2];

                UInt64 value64;

                if (UInt64.TryParse(sLine[1], out value64))
                {
                    l.LeftId = value64;
                }
                else{
                    continue;
                }

                if (UInt64.TryParse(sLine[3], out value64))
                {
                    l.RightId = value64;
                }
                else
                {
                    continue;
                }

                ids.Add(l);
            }

            return ids;
        }

        public void UpdateInfos(int id)
        {
            if (id < 0)
            {
                id = 0;
            }
            else if (id > logInfos.Count() - 1)
            {
                id = logInfos.Count() - 1;
            }

            previousInfo = currentInfo;
            currentInfo = id;

            if (previousInfo == -1 || logInfos[previousInfo].LeftPath != logInfos[currentInfo].LeftPath)
            {
                ImageLeft.Source = new BitmapImage(new Uri(logInfos[currentInfo].LeftPath));
            }

            if (previousInfo == -1 || logInfos[previousInfo].RightPath != logInfos[currentInfo].RightPath)
            {
                ImageRight.Source = new BitmapImage(new Uri(logInfos[currentInfo].RightPath));
            }

            LabelLeft.Content = logInfos[currentInfo].LeftId;
            LabelRight.Content = logInfos[currentInfo].RightId;

            GoId.Text = logInfos[currentInfo].LeftId.ToString();
        }

        public MainWindow()
        {
            InitializeComponent();

            String[] args = Environment.GetCommandLineArgs();

            if (args.Count() < 2) return;

            logInfos = GetLogInfos(args[1]);

            if (logInfos.Count() > 0)
            {
                UpdateInfos(0);
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfos(currentInfo - 1);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfos(currentInfo + 1);
        }

        private void Match_Click(object sender, RoutedEventArgs e)
        {
            MatchLog(logInfos[currentInfo].LeftId, logInfos[currentInfo].RightId);

            UpdateInfos(currentInfo + 1);
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            UInt64 goId = Convert.ToUInt64(GoId.Text);
            int i = 0;

            while (i < logInfos.Count())
            {
                if (goId == logInfos[i].LeftId)
                {
                    UpdateInfos(i);
                    break;
                }

                ++i;
            }
        }

        private void GoNext_Click(object sender, RoutedEventArgs e)
        {
            UInt64 goId = Convert.ToUInt64(GoId.Text);
            int i = 0;

            while (i < logInfos.Count())
            {
                if (goId == logInfos[i].LeftId)
                {
                    while (++i < logInfos.Count())
                    {
                        if (goId != logInfos[i].LeftId)
                        {
                            UpdateInfos(i);
                            break;
                        }
                    }
                }

                ++i;
            }
        }            

    }
}
