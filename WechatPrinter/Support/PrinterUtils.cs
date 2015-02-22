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

        private static PrintDialog printDialog;
        private static PrintQueue printQueue;
        private static Queue<string> filepahtQueue = new Queue<string>();
        private const int PRINTER_CHECK_INTERVAL = 250;
        private static IPrinterStatus printerStatus;
        private static MainPage mainPage;
        private static bool run;
        private static bool isPrinting = false;

        public static void Start(IPrinterStatus ps, MainPage page)
        {
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                run = true;
                foreach (PrintQueue pq in new LocalPrintServer().GetPrintQueues())
                {
                    Console.WriteLine(pq.Name);
                    if (pq.Name.Equals(WechatPrinterConf.PrinterName))
                    {
                        printQueue = pq;
                        printDialog = new PrintDialog();
                        printDialog.PrintQueue = printQueue;
                        break;
                    }
                }
                printerStatus = (IPrinterStatus)state;
                mainPage = page;
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

                        //mainPage.Dispatcher.BeginInvoke(new Action(delegate {

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
                            tbi.Transform = new ScaleTransform(WechatPrinterConf.PrinterWidth / bi.PixelWidth, WechatPrinterConf.PrinterHeight / bi.PixelHeight);
                            tbi.EndInit();
                            tbi.Freeze();

                            //Console.WriteLine("printable 1: {0}x{1}", printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                            //PrintTicket pt = printQueue.DefaultPrintTicket;
                            //pt.PageMediaSize = new PageMediaSize(PRINT_WIDTH, PRINT_HEIGHT);
                            //printDialog.PrintTicket = pt;
                            //Console.WriteLine("printable 2: {0}x{1}", printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                            var group = new DrawingGroup();
                            group.Children.Add(new ImageDrawing(tbi, new Rect(0, 0, WechatPrinterConf.PrinterWidth * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), WechatPrinterConf.PrinterHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));

                            var vis = new DrawingVisual();


                            using (DrawingContext dc = vis.RenderOpen())
                            {
                                dc.DrawDrawing(group);
                            }


                            isPrinting = true;

                            printDialog.PrintVisual(vis, "Wechat Printer Image");
                        }

                        //}));



                    }
                }
                else if (printQueue.IsPrinting || printQueue.IsBusy || printQueue.IsProcessing)
                {
                    if (!isPrinting)
                    {
                        isPrinting = true;
                    }
                    Console.WriteLine("Printing");
                }
                else if (printQueue.IsWaiting || printQueue.QueueStatus == PrintQueueStatus.None)
                {
                    if (isPrinting)
                    {
                        printerStatus.PrinterCompeleted();
                        //TODO
                        isPrinting = false;
                    }
                }
                else
                {
                    //CheckError();
                    //TODO
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
