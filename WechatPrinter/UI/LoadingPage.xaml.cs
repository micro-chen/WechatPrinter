﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WechatPrinter.Support;

namespace WechatPrinter
{
    /// <summary>
    /// Loading.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingPage : Page, IDownloadProgress
    {
        ILoadStatus status;

        //"http://ww1.sinaimg.cn/bmiddle/6cf8a22bjw1eodufymcyqj218g0tndqr.jpg"

        private MainPage page = null;
        private WechatPrinterServer server = null;

        public LoadingPage(ILoadStatus status)
        {
            InitializeComponent();
            this.status = status;
        }

        private void page_loading_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Page Init");

            if (!PrinterUtils.Check())
            {
                MessageBox.Show("连接打印机错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Window.GetWindow(this).Close();
            }
            else
            {

                InfoBean info = null;
                //int retry = 0;
                label_loading.Content = "连接微信打印服务器...";

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (o, ee) =>
                {
                    try
                    {
                        Dictionary<string, string> param = new Dictionary<string, string>();
                        param.Add(WechatPrinterConf.ParamKeys.Id, WechatPrinterConf.Id);
                        ee.Result = HttpUtils.GetJson<InfoBean>(WechatPrinterConf.InitUrl, param, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("LoadingPage:Get InfoBean Error");
                        Console.WriteLine(ex.Message);
                        ee.Result = null;
                    }
                };
                bw.RunWorkerCompleted += (o, ee) =>
                {
                    if (ee.Result == null)
                    {
                        MessageBox.Show("连接微信打印服务器错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        Window.GetWindow(this).Close();
                    }
                    else
                    {
                        info = (InfoBean)ee.Result;

                        if (info.qrcodeUrl == null || info.verifyCode == 0)
                        {
                            MessageBox.Show("微信打印服务器内部错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            Window.GetWindow(this).Close();
                        }
                        else
                        {

                            for (int i = 0; i < info.picUrl.Count; i++) { info.picUrl[i] = WechatPrinterConf.PRE_URL + info.picUrl[i]; }
                            for (int i = 0; i < info.videoUrl.Count; i++) { info.videoUrl[i] = WechatPrinterConf.PRE_URL + info.videoUrl[i]; }
                            info.qrcodeUrl = WechatPrinterConf.PRE_URL + info.qrcodeUrl;
                            info.logoUrl = WechatPrinterConf.PRE_URL + info.logoUrl;

                            Stage(0);

                            page = new MainPage();

                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                try
                                {
                                    StringCollection adImgFilepaths = LoadAdImg(info.picUrl);
                                    StringCollection adVidFilepaths = LoadAdVid(info.videoUrl);

                                    #region SM
                                    //string qrCodeFilepath = "pack://application:,,,/Resource/Image/smkj_printQR.jpg";
                                    //    string printQRCodeFilepath = "pack://application:,,,/Resource/Image/smkj_printQR.jpg";
                                    //string logoFilepath = "pack://application:,,,/Resource/Image/smkj_printLogo.png";
                                    #endregion

                                    #region CoolMore
                                    string qrCodeFilepath = LoadQRCode(info.qrcodeUrl);
                                    string printQRCodeFilepath = qrCodeFilepath;
                                    //string logoFilepath = "pack://application:,,,/Resource/Image/printLogo.jpg";
                                    string logoFilepath = LoadLogo(info.logoUrl);
                                    #endregion

                                    if (WechatPrinterConf.Init(
                                            adImgFilepaths,
                                            adVidFilepaths,
                                            logoFilepath,
                                            qrCodeFilepath,
                                            printQRCodeFilepath,
                                            info.name,
                                            info.verifyCode))
                                    {
                                        page.Dispatcher.BeginInvoke(new Action(delegate
                                        {
                                            server = new WechatPrinterServer(page);
                                        }));
                                        Stage(1 << 2);
                                    }
                                    else
                                    {
                                        throw (new Exception());
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show("微信打印服务器正在维护", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                    Environment.Exit(1);
                                }
                            });

                        }
                    }
                };
                bw.RunWorkerAsync();
            }
        }

        private string LoadQRCode(string url)
        {
            string s = String.Empty;
            try
            {
                s = HttpUtils.GetFile(FileUtils.ResPathsEnum.QR, url, null);

            }
            catch
            {
            }
            return s;
        }
        private string LoadLogo(string url)
        {
            string s = String.Empty;
            try
            {
                s = HttpUtils.GetFile(FileUtils.ResPathsEnum.Logo, url, null);
            }
            catch
            {

            }
            return s;
        }
        private StringCollection LoadAdImg(StringCollection urls)
        {
            StringCollection sc = new StringCollection();
            try
            {
                sc = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdImg, urls, null);
                Stage(1);
            }
            catch
            {
                Stage(-1);
                Console.WriteLine("Load AdImg ERROR");
            }
            return sc;
        }
        private StringCollection LoadAdVid(StringCollection urls)
        {
            StringCollection sc = new StringCollection();
            try
            {
                sc = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdVid, urls, null, false, this);
                Stage(1 << 1);
            }
            catch
            {
                Stage(-1);
                Console.WriteLine("Load AdVid ERROR");
            }
            return sc;
        }

        private int stage;
        public void Stage(int s)
        {
            stage = s;
            if (s == -1)
            {
                if (MessageBox.Show("因网络原因加载资源失败，重试？", "错误", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    System.Windows.Forms.Application.Restart();
                }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    Application.Current.Shutdown();
                }));
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    switch (s)
                    {
                        case 0:
                            label_loading.Content = "加载图片...";
                            break;
                        case 1:
                            label_loading.Content = "图片加载完成，加载视频...";
                            break;
                        case 1 << 1:
                            label_loading.Content = "视频加载完成，加载图片...";
                            break;
                        case 1 << 2:

                            server.ShowCoName();
                            server.ShowAdVid();
                            page.mediaElement_ad.Opacity = 1d;
                            page.mediaElement_ad.Play();
                            server.ShowAdImg();
                            page.image_ad1.Opacity = 1d;
                            page.image_ad2.Opacity = 1d;
                            page.image_ad3.Opacity = 1d;
                            server.ShowCaptcha();
                            page.label_captcha.Opacity = 1d;
                            server.ShowQRImg();

                            label_loading.Content = "资源加载完成，启动中...";
                            DispatcherTimer timer = new DispatcherTimer();
                            timer.Tick += (o, e) =>
                            {
                                ((DispatcherTimer)o).Stop();
                                status.LoadCompleted(page, server);
                            };
                            timer.Interval = new TimeSpan(0, 0, 0, 0, WechatPrinterConf.LoadedWaitTime);
                            timer.Start();
                            break;
                    }
                }));
            }

        }

        private long vidLength = 0;
        private int currentProgress = 0;
        public void Progress(long total, long read)
        {
            if (total > 0 && read == -2)
            {
                vidLength = total;
            }
            if (total == -2 && read > 0)
            {

                Console.WriteLine(read);
                Console.WriteLine(vidLength);

                currentProgress = (int)(((double)read / (double)vidLength) * 100);
                if (stage == 1)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        label_loading.Content = "图片加载完成，加载视频... " + Convert.ToString(currentProgress) + "%";
                    }));
                }

            }
        }
    }



    public class InfoBean
    {
        public StringCollection videoUrl { get; set; }
        public StringCollection picUrl { get; set; }
        public string qrcodeUrl { get; set; }
        public string printQrCodeUrl { get; set; }
        public int verifyCode { get; set; }
        public string name { get; set; }
        public string logoUrl { get; set; }
    }

    public interface ILoadStatus
    {
        void LoadCompleted(MainPage page, WechatPrinterServer server);
    }

    public interface ILoadStage
    {
        void Stage(int stage);
    }
}
