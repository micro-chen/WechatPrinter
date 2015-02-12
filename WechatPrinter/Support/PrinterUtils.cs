using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WechatPrinter.Support
{
    class PrinterUtils
    {
        private const double PRINT_WIDTH = 200;
        private const double PRINT_HEIGHT = 300;
        private static PrintDialog printDialog;
        private static PrintQueue printQueue;
        private static Queue<string> filepahtQueue = new Queue<string>();
        private const int PRINTER_CHECK_INTERVAL = 250;
        private static IPrinterStatus printerStatus;
        private static bool run;
        private static bool isPrinting = false;

        public static void Start(IPrinterStatus ps)
        {
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                run = true;
                foreach (PrintQueue pq in new LocalPrintServer().GetPrintQueues())
                {
                    if (pq.Name.Equals("Microsoft XPS Document Writer"))
                    {
                        printQueue = pq;
                        printDialog = new PrintDialog();
                        printDialog.PrintQueue = printQueue;
                        break;
                    }
                }
                printerStatus = (IPrinterStatus)state;
                Work();
            }, ps);
        }

        public static void Stop()
        {
            run = false;
        }

        public static void Print(string filepath)
        {
            filepahtQueue.Enqueue(filepath);
        }

        public static bool CurrentStatus
        {
            get { return printQueue.IsPrinting || printQueue.IsProcessing; }
        }

        private static void Work()
        {
            while (run)
            {
                if (filepahtQueue.Count > 0)
                {
                    while (filepahtQueue.Count > 0)
                    {
                        string filepath = filepahtQueue.Dequeue();

                        Console.WriteLine(printQueue.Name + " print\t" + filepath);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            Bitmap bm = new Bitmap(filepath);
                            bm.Save(ms, ImageFormat.Png);
                            int oriWidth = bm.Width;
                            int oriHeight = bm.Height;
                            bm.Dispose();
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.StreamSource = ms;
                            if (oriWidth > oriHeight)
                            {
                                bi.Rotation = Rotation.Rotate90;
                            }
                            bi.EndInit();
                            bi.Freeze();

                            TransformedBitmap tbi = new TransformedBitmap();
                            tbi.BeginInit();
                            tbi.Source = bi;
                            tbi.Transform = new ScaleTransform(PRINT_WIDTH / bi.PixelWidth, PRINT_HEIGHT / bi.PixelHeight);
                            tbi.EndInit();
                            tbi.Freeze();
                            Console.WriteLine("Final Size: {0}x{1}", tbi.PixelWidth, tbi.PixelHeight);

                            var vis = new DrawingVisual();
                            var dc = vis.RenderOpen();
                            dc.DrawImage(tbi, new Rect { Width = bi.Width, Height = bi.Height });
                            dc.Close();

                            isPrinting = true;

                            printDialog.PrintVisual(vis, "Wechat Printer Image");
                        }

                    }
                }
                else if (printQueue.IsPrinting || printQueue.IsBusy || printQueue.IsProcessing)
                {
                    //if (!isPrinting)
                    //{
                    //    isPrinting = true;
                    //}
                    //Console.WriteLine("Printing");
                }
                else if (printQueue.IsWaiting || printQueue.QueueStatus == PrintQueueStatus.None)
                {
                    if (isPrinting)
                    {
                        printerStatus.PrinterCompeleted();
                        isPrinting = false;
                    }
                }
                else
                {
                    CheckError();
                }
                Thread.Sleep(PRINTER_CHECK_INTERVAL);
            }
        }


        private static void CheckError()
        {
            if (printQueue.IsOutOfPaper || printQueue.IsManualFeedRequired)
            {
                printerStatus.PrinterError(ErrorUtils.Error.PrinterOutOfPaper);
            }
            else if (printQueue.IsPaperJammed)
            {
                printerStatus.PrinterError(ErrorUtils.Error.PrinterPaperJammed);
            }
            else if (printQueue.IsTonerLow)
            {
                printerStatus.PrinterError(ErrorUtils.Error.PrinterOutOfInk);
            }
            else if (printQueue.IsDoorOpened)
            {
                printerStatus.PrinterError(ErrorUtils.Error.PrinterDoorOpened);
            }
            else if (printQueue.IsOffline || printQueue.IsNotAvailable)
            {
                printerStatus.PrinterError(ErrorUtils.Error.PrinterUnavailable);
            }
        }
    }

    public interface IPrinterStatus
    {
        void PrinterError(ErrorUtils.Error error);
        void PrinterAvailable();
        void PrinterCompeleted();
    }
}
