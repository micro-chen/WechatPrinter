﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Printing;
using System.Threading;
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
        private static bool run;
        private static bool isPrinting = false;
        private static int imgId = EmptyImgId;
        
        private static DrawingGroup DefaultDrawingGroup;

        public static void Start(IPrinterStatus ps)
        {
            ThreadPool.QueueUserWorkItem(delegate (object o)
            {
                Thread.Sleep(800);

                printQueue = GetPrintQueueByName();
                printDialog = new PrintDialog();
                printDialog.PrintQueue = printQueue;

                printerStatus = (IPrinterStatus)o;
                run = true;

                BitmapImage logo = FileUtils.LoadImage(WechatPrinterConf.LogoFilepath);
                BitmapImage qr = FileUtils.LoadImage(WechatPrinterConf.PrintQRCodeFilepath);
                double logoWidth = WechatPrinterConf.PrinterLogoHeight * ((double)logo.PixelWidth / (double)logo.PixelHeight);

                //qr.UriSource = new Uri(FileUtils.GetLatestFile(FileUtils.ResPathsEnum.QR));

                DefaultDrawingGroup = new DrawingGroup();
                DefaultDrawingGroup.Children.Add(new ImageDrawing(logo, new Rect(WechatPrinterConf.PrinterLogoWidthPos, WechatPrinterConf.PrinterLogoHeightPos, logoWidth * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), WechatPrinterConf.PrinterLogoHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));
                DefaultDrawingGroup.Children.Add(new ImageDrawing(qr, new Rect(WechatPrinterConf.PrinterQrWidthPos, WechatPrinterConf.PrinterQrHeightPos, WechatPrinterConf.PrinterQrHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), WechatPrinterConf.PrinterQrHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));

                Work();
            }, ps);
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

                        using (MemoryStream ms = new MemoryStream())
                        {

                            try
                            {
                                Bitmap bm = new Bitmap(filepath);
                                bm.Save(ms, ImageFormat.Png);
                                int oriWidth = bm.Width;
                                int oriHeight = bm.Height;
                                bm.Dispose();


                                BitmapImage bi = new BitmapImage();
                                bi.BeginInit();
                                bi.StreamSource = ms;
                                //旧版用于旋转
                                //if (oriWidth > oriHeight)
                                //{
                                //    bi.Rotation = Rotation.Rotate90;
                                //}

                                bi.EndInit();
                                bi.Freeze();

                                TransformedBitmap tbi = new TransformedBitmap();
                                tbi.BeginInit();
                                //tbi.Source = bi;

                                int x, y, width;
                                if (bi.PixelWidth > bi.PixelHeight) // 横向图片
                                {
                                    x = (bi.PixelWidth - bi.PixelHeight) / 2;
                                    y = 0;
                                    width = bi.PixelHeight;
                                }
                                else // 纵向图片
                                {
                                    x = 0;
                                    y = (bi.PixelHeight - bi.PixelWidth) / 2;
                                    width = bi.PixelWidth;
                                }

                                tbi.Source = new CroppedBitmap(bi, new Int32Rect(x, y, width, width));
                                double targetScale;
                                //if (WechatPrinterConf.PrinterWidth / bi.PixelWidth * bi.PixelHeight > WechatPrinterConf.PrinterHeight)
                                //{
                                //    targetScale = WechatPrinterConf.PrinterHeight / bi.PixelHeight;
                                //}
                                //else
                                //{
                                //    targetScale = WechatPrinterConf.PrinterWidth / bi.PixelWidth;
                                //}
                                //tbi.Transform = new ScaleTransform(targetScale, targetScale);

                                targetScale = WechatPrinterConf.PrinterWidth / width;
                                tbi.Transform = new ScaleTransform(targetScale, targetScale);
                                tbi.EndInit();
                                tbi.Freeze();
                                
                                var group = DefaultDrawingGroup.Clone();
                                group.Children.Add(new ImageDrawing(tbi, new Rect(WechatPrinterConf.PrinterWidthPos + ((WechatPrinterConf.PrinterWidth - tbi.PixelWidth) / 2d) * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), WechatPrinterConf.PrinterHeightPos + ((WechatPrinterConf.PrinterHeight - tbi.PixelHeight) / 2d) * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), tbi.PixelWidth * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), tbi.PixelHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));
                                //group.Children.Add(new ImageDrawing(tbi, new Rect(WechatPrinterConf.PrinterWidthPos + WechatPrinterConf.PrinterWidth / 2d - (tbi.PixelWidth / 2d), WechatPrinterConf.PrinterHeightPos, tbi.PixelWidth * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi), tbi.PixelHeight * (WechatPrinterConf.ScreenDpi / WechatPrinterConf.PrinterDpi))));

                                var vis = new DrawingVisual();

                                using (DrawingContext dc = vis.RenderOpen())
                                {
                                    dc.DrawDrawing(group);
                                }

                                PrintTicket pt = FileUtils.GetPrintTicket();
                                if (pt == null)
                                {
                                    printerStatus.PrinterConf();
                                    while (pt == null)
                                    {
                                        Thread.Sleep(500);
                                        pt = FileUtils.GetPrintTicket();
                                    }
                                }


                                printDialog.PrintTicket = pt;

                                isPrinting = true;
                                printDialog.PrintVisual(vis, "Wechat Printer Image");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                }
                else if (printQueue.IsPrinting || printQueue.IsBusy || printQueue.IsProcessing)
                {
                    if (!isPrinting)
                    {
                        isPrinting = true;
                    }
                    Console.WriteLine("Printing");
                    //if (printerStatus != null)
                    //    printerStatus.PrinterAvailable();
                }
                else
                {
                    if (isPrinting)
                    {
                        if (printerStatus != null)
                            printerStatus.PrinterCompeleted(imgId);
                        PrinterUtils.imgId = EmptyImgId;
                        isPrinting = false;
                    }
                    //if (printerStatus != null)
                    //    printerStatus.PrinterAvailable();
                }
                //else if (printQueue.IsWaiting || printQueue.QueueStatus == PrintQueueStatus.None)
                //{
                //    if (isPrinting)
                //    {
                //        if (printerStatus != null)
                //            printerStatus.PrinterCompeleted(imgId);
                //        PrinterUtils.imgId = EmptyImgId;
                //        isPrinting = false;
                //    }
                //    if (printerStatus != null)
                //        printerStatus.PrinterAvailable();
                //}
                //else if (printQueue.IsTonerLow)
                //{
                //    if (isPrinting)
                //    {
                //        if (printerStatus != null)
                //            printerStatus.PrinterCompeleted(imgId);
                //        PrinterUtils.imgId = EmptyImgId;
                //        isPrinting = false;
                //    }
                //    if (printerStatus != null)
                //        printerStatus.PrinterError(PrintQueueStatus.TonerLow, imgId);
                //}
                //else
                //{
                //    if (printerStatus != null)
                //        printerStatus.PrinterError(printQueue.QueueStatus, imgId);
                //    PrinterUtils.imgId = EmptyImgId;

                //    //TODO 打印机错误


                //}
                Thread.Sleep(PRINTER_CHECK_INTERVAL);
            }
        }

        public static PrintQueue GetPrintQueueByName()
        {
            PrintQueue queue = null;
            foreach (PrintQueue pq in new LocalPrintServer().GetPrintQueues())
            {
                if (pq.Name.Equals(WechatPrinterConf.PrinterName))
                {
                    queue = pq;
                }
            }
            return queue;
        }

        public static bool Check()
        {
            return GetPrintQueueByName() != null ? true : false;
        }

        public static void Stop()
        {
            run = false;
        }
    }



    public interface IPrinterStatus
    {
        void PrinterError(PrintQueueStatus error, int imgId);
        void PrinterAvailable();
        void PrinterCompeleted(int imgId);
        void PrinterConf();
    }
}
