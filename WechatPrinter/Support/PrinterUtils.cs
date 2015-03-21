using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
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

        public const int EmptyImgId = -23333;
        private const int PRINTER_CHECK_INTERVAL = 250;

        private static PrintDialog printDialog;
        private static PrintQueue printQueue;
        private static Queue<string> filepahtQueue = new Queue<string>();
        private static IPrinterStatus printerStatus;
        private static MainPage mainPage;
        private static bool run;
        private static bool isPrinting = false;
        private static int imgId = EmptyImgId;


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

        public static void Print(string filepath, int imgId)
        {
            filepahtQueue.Enqueue(filepath);
            PrinterUtils.imgId = imgId;
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
                            bool rotated270 = false;
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
                            else
                            {
                                //bi.Rotation = Rotation.Rotate270;
                                //rotated270 = true;
                            }
                            
                            bi.EndInit();
                            bi.Freeze();

                            TransformedBitmap tbi = new TransformedBitmap();
                            tbi.BeginInit();
                            tbi.Source = bi;
                            double targetScale;
                            if (WechatPrinterConf.PrinterWidth / bi.PixelWidth * bi.PixelHeight > WechatPrinterConf.PrinterHeight)
                            {
                                targetScale = WechatPrinterConf.PrinterHeight / bi.PixelHeight;
                            }
                            else
                            {
                                targetScale = WechatPrinterConf.PrinterWidth / bi.PixelWidth;
                            }

                            tbi.Transform = new ScaleTransform(targetScale, targetScale);
                            tbi.EndInit();
                            tbi.Freeze();

                            BitmapImage logo = new BitmapImage();
                            logo.BeginInit();
                            if(rotated270)
                            {
                                //logo.Rotation = Rotation.Rotate270;
                            }
                            logo.UriSource = new Uri("pack://application:,,,/Resource/Image/smkjblack.png");
                            logo.EndInit();
                            logo.Freeze();

                            //Console.WriteLine("printable 1: {0}x{1}", printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                            //PrintTicket pt = printQueue.DefaultPrintTicket;
                            //pt.PageMediaSize = new PageMediaSize(PRINT_WIDTH, PRINT_HEIGHT);
                            //printDialog.PrintTicket = pt;
                            //Console.WriteLine("printable 2: {0}x{1}", printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                            var group = new DrawingGroup();
                            group.Children.Add(new ImageDrawing(tbi, new Rect(WechatPrinterConf.PrinterWidthPos, WechatPrinterConf.PrinterHeightPos, tbi.PixelWidth * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), tbi.PixelHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));
                            group.Children.Add(new ImageDrawing(logo, new Rect(WechatPrinterConf.PrinterLogoWidthPos, WechatPrinterConf.PrinterLogoHeightPos, WechatPrinterConf.PrinterLogoHeight * 5.65 * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), WechatPrinterConf.PrinterLogoHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));

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
                        printerStatus.PrinterCompeleted(imgId);
                        PrinterUtils.imgId = EmptyImgId;
                        //TODO
                        isPrinting = false;
                    }

                }
                else
                {
                    printerStatus.PrinterError(printQueue.QueueStatus, imgId);
                    PrinterUtils.imgId = EmptyImgId;

                    //TODO 打印机错误
                    Console.WriteLine("Printer Error: {0}", printQueue.QueueStatus);
                }
                Thread.Sleep(PRINTER_CHECK_INTERVAL);
            }
        }
    }

    public interface IPrinterStatus
    {
        void PrinterError(PrintQueueStatus error, int imgId);
        void PrinterAvailable();
        void PrinterCompeleted(int imgId);
    }
}
