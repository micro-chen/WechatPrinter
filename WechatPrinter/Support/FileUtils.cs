using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Printing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WechatPrinter.Support;

namespace WechatPrinter.Support
{
    public class FileUtils
    {

        public enum ResPathsEnum { PrintImg, AdImg, AdVid, QR, Logo };
        public static string LogPath = rootPath + "errors.log";

        private static string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string resPath = rootPath + "res\\";
        private static string[] resPaths = { resPath + "print\\", resPath + "ad\\img\\", resPath + "ad\\vid\\", resPath + "qr\\", resPath + "logo\\" };
        private static string printTicketPath = rootPath + "\\"+ "打印配置文件（删除此文件重新配置打印机）.xml";
        private const int FOLDER_SIZE_LIMIT = 300;

        public static PrintTicket GetPrintTicket()
        {
            try
            {
                StreamReader reader = new StreamReader(printTicketPath);
                PrintTicket pt = new PrintTicket(reader.BaseStream);
                reader.Close();
                return pt;
            }
            catch
            {
                return null;
            }
        }

        public static void SavePrintTicket(PrintTicket pt)
        {
            StreamWriter writer = new StreamWriter(printTicketPath);
            pt.SaveTo(writer.BaseStream);
            writer.Close();
        }

        public static void CleanCache(string filepath = null)
        {
            DirectoryInfo resDir;
            if(filepath == null)
                resDir = new DirectoryInfo(resPath);
            else
                resDir = new DirectoryInfo(filepath);

                foreach (FileInfo file in resDir.GetFiles()) 
                    try { file.Delete(); }
                    catch { }

                foreach (DirectoryInfo dir in resDir.GetDirectories()) 
                    try { CleanCache(dir.FullName); dir.Delete(true); }
                    catch { }
        }

        public static BitmapImage LoadImage(string filepath, int decodeWidth = -1)
        {

            Console.WriteLine(filepath);
            
            {
                using (Stream imgStream =
                    filepath.Contains("pack://") ?
                    System.Windows.Application.GetResourceStream(new Uri(filepath)).Stream:
                    null)
                {
                    MemoryStream ms = new MemoryStream();
                    using (Bitmap bm = imgStream != null ?
                        new Bitmap(imgStream):
                        new Bitmap(filepath))
                    {
                        bm.Save(ms, ImageFormat.Png);
                        bm.Dispose();
                    }
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = ms;
                    if(decodeWidth != -1)
                        bi.DecodePixelWidth = decodeWidth;
                    bi.EndInit();
                    bi.Freeze();

                    return bi;
                }
            }
        }

        public static bool CheckFile(ResPathsEnum path, string filename)
        {
            if (!Directory.Exists(resPaths[(int)path]))
            {
                Directory.CreateDirectory(resPaths[(int)path]);
                //Console.WriteLine("File folder doesn't exists, created: " + ResPaths[(int)path]);
                return false;
            }
            else if (!File.Exists(resPaths[(int)path] + filename))
            {
                Console.WriteLine("File doesn't exists: " + filename);
                return false;
            }
            else
            {
                //Console.WriteLine("File exists: " + filename);
                return true;
            }
        }

        public static string SaveFile(Stream stream, ResPathsEnum path, string filename, IDownloadProgress progress = null)
        {
            DeleteOldFiles(path);
            string filepath = resPaths[(int)path];
            try
            {
                if (!CheckFile(path, filename))
                {
                    using (FileStream fs = File.Create(filepath + filename))
                    {
                        CopyStream(stream, fs, progress);
                        //stream.CopyTo(fs);
                    }
                }
                Console.WriteLine("SaveFile: " + filepath + filename);
                return filepath + filename;
            }
            catch (Exception)
            {
                Console.WriteLine("[FileUtils: SaveFile Error]");
                throw;
            }

        }

        public static void DeleteOldFiles(ResPathsEnum path)
        {
            string filepath = resPaths[(int)path];
            if (Directory.Exists(filepath))
            {
                long size = 0;
                FileInfo[] fis = new DirectoryInfo(filepath).GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                //Console.WriteLine(filepath + " size: {0:#0.00}MB", size / 1024d / 1024d);
                if (size > FOLDER_SIZE_LIMIT * 1024 * 1024)
                {
                    Array.Sort(fis, new FileCreateTimeComparer());
                    foreach (FileInfo fi in fis)
                    {
                        Console.WriteLine(filepath + " delete: " + fi.FullName + "\t" + fi.CreationTime);
                        size -= fi.Length;
                        fi.Delete();
                        if (size <= FOLDER_SIZE_LIMIT)
                            break;
                    }
                }
            }
        }

        public static String GetLatestFile(ResPathsEnum path)
        {
            return new DirectoryInfo(resPaths[(int)path]).GetFiles().OrderByDescending(f => f.LastWriteTime).First().FullName;
        }

        private static void CopyStream(Stream input, Stream output, IDownloadProgress progress = null)
        {
            const int bufferSize = 256000;
            byte[] buffer = new byte[bufferSize];
            int buffered = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
                buffered += read;
                if (progress != null)
                    progress.Progress(-2, buffered);
            }
        }

        protected class FileCreateTimeComparer : IComparer
        {
            int IComparer.Compare(Object o1, Object o2)
            {
                FileInfo fi1 = o1 as FileInfo;
                FileInfo fi2 = o2 as FileInfo;
                return fi1.CreationTime.CompareTo(fi2.CreationTime);
            }
        }

    }
}
