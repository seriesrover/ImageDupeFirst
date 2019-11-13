using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Preprocessor
{
    public class ScannedBitmap
    {
        public String Path { get; set; }

        public Bitmap Bitmap { get; set; }

        public ScannedBitmap(String path, Bitmap bitmap)
        {
            Path = path;
            Bitmap = bitmap;
        }

        public ScannedBitmap()
        {
            Path = null;
            Bitmap = null;
        }

        ~ScannedBitmap()
        {
            if (Bitmap != null)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }

            Path = null;
        }
    }

    class Program
    {
        private const String NORMALIZED = "/normalized/";
        private const String LOGFILE = "D:\\log.txt";
        private const String EXCEPTIONFILE = "D:\\exception.txt";

        static UInt64? HasNumber(String path)
        {
            String fileName = Path.GetFileName(path);

            String numberStr = Regex.Match(fileName, @"\d+").Value;

            if (String.IsNullOrEmpty(numberStr)) return null;

            return UInt64.Parse(numberStr);
        }

        static List<UInt64> GetExceptionIds()
        {
            if (!File.Exists(EXCEPTIONFILE)) return null;

            return Array.ConvertAll(File.ReadAllLines(EXCEPTIONFILE), UInt64.Parse).ToList();
        }

        static void Log(string message)
        {
            using (StreamWriter w = File.AppendText(LOGFILE))
            {
                w.Write("\r\n" + message);
            }

            Console.WriteLine(message);
        }

        static void ExceptionLog(UInt64 id)
        {
            using (StreamWriter w = File.AppendText(EXCEPTIONFILE))
            {
                w.Write("\r\n{0}", id);
            }
        }

        static void Main(String[] args)
        {
            if (args.Count() == 0) return;

            String normalizedDir = args[0] + NORMALIZED;
            Directory.CreateDirectory(normalizedDir);

            List<String> paths = Directory.GetFiles(args[0], "*.*", SearchOption.TopDirectoryOnly).ToList();
            List<ScannedBitmap> scannedBitmaps = new List<ScannedBitmap>();
            List<UInt64> exceptionIds = GetExceptionIds();

            int scannedCount = 0;
            int scannedCountCreated = 0;

            // get paths and bitmaps off disk
            foreach (String path in paths)
            {
                UInt64? exceptionId = HasNumber(path);

                if (exceptionId == null ||
                    (exceptionIds != null && exceptionIds.Contains((UInt64)exceptionId)))
                    continue;

                ScannedBitmap scannedBitmap = new ScannedBitmap(normalizedDir + Path.GetFileName(path), null);

                if (!File.Exists(scannedBitmap.Path))
                {
                    using (Image image = Image.FromFile(path))
                    {
                        scannedBitmap.Bitmap = PreProcessor.ImageUtil.ResizeImage(image, 256, 256);

                        // save normalized
                        scannedBitmap.Bitmap.Save(scannedBitmap.Path, ImageFormat.Jpeg);
                    }

                    ++scannedCountCreated;
                }
                else 
                {
                    using (Image image = Image.FromFile(scannedBitmap.Path))
                    {
                        scannedBitmap.Bitmap = new Bitmap(image);
                    }

                    ++scannedCount;
                }

                scannedBitmaps.Add(scannedBitmap);
            }

            Log("====");
            Log(String.Format("==== New Run, {0}, {1} of {2}", args[1], scannedBitmaps.Count, paths.Count));
            Log("====");

            // iterate through bitmaps for psnr
            AForge.Imaging.ExhaustiveTemplateMatching tm = new AForge.Imaging.ExhaustiveTemplateMatching(0);

            using (List<ScannedBitmap>.Enumerator outer = scannedBitmaps.GetEnumerator())
            {
                while (outer.MoveNext())
                {
                    String pathOuter = Path.GetFileName(outer.Current.Path);
                    UInt64? outerId = HasNumber(pathOuter);

                    using (List<ScannedBitmap>.Enumerator inner = outer)
                    {
                        while (inner.MoveNext())
                        {
                            String pathInner = Path.GetFileName(inner.Current.Path);
                            UInt64? innerId = HasNumber(pathInner);

                            using (Bitmap outer24 = PreProcessor.ImageUtil.ConvertToFormat(outer.Current.Bitmap, PixelFormat.Format24bppRgb))
                            {
                                using (Bitmap inner24 = PreProcessor.ImageUtil.ConvertToFormat(inner.Current.Bitmap, PixelFormat.Format24bppRgb))
                                {
                                    AForge.Imaging.TemplateMatch[] matchings = tm.ProcessImage(outer24, inner24);

                                    if (matchings[0].Similarity > Convert.ToDouble(args[1]))
                                    {
                                        Log(String.Format("{0}\t{1}\t{2}\t{3}", pathOuter, (outerId == null) ? 0 : outerId,
                                                            pathInner, (innerId == null) ? 0 : innerId));
                                    }
                                }
                            }
                        }
                    }

                    if (outerId != null)
                    {
                        ExceptionLog((UInt64) outerId);
                    }
                }
            }

            paths = null;
            scannedBitmaps = null;

            Console.WriteLine("Done");

            Console.ReadKey();
        }
    }
}
