using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Collections.Generic;
using System.Printing;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Collections;
using System.ComponentModel;
using System.Windows.Media.Animation;
using WechatPrinter.Support;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WechatPrinter
{
    public class WechatPrinterServer : IDisposable, IPrinterStatus
    {

        private void BackgroundRun(Action action)
        {
            ThreadPool.QueueUserWorkItem(delegate { action(); });
        }
        public WechatPrinterServer(MainPage page)
        {
            this.page = page;
            PrinterUtils.Start(this);
        }

        #region 发送请求
        #region 定时查询打印图片
        private Timer printImgTimer;

        public void StartCheckPrintImg()
        {
            printImgTimer = new Timer(GetPrintImg, null, 0, WechatPrinterConf.PrintImgInterval);
        }
        public void StopCheckPrintImg()
        {
            printImgTimer.Dispose();
        }
        #endregion

        #region 获得打印图片
        private bool isGettingPrintImg = false;
        public void GetPrintImg(Object o)
        {
            if (!WechatPrinterConf.IsPrinting && !isGettingPrintImg)
            {
                BackgroundRun(delegate
                {
                    Console.WriteLine("Start to get print images");
                    isGettingPrintImg = true;
                    try
                    {
                        HideNetworkError();

                        PrintImgBean bean = null;

                        string jsonString = HttpUtils.GetText(WechatPrinterConf.PrintImgUrl, null, true, true);

                        if (!jsonString.Contains("id") && !jsonString.Contains("url") && jsonString.Contains("verifyCode"))
                        {
                            jsonString = jsonString.Replace("[", "").Replace("]", "");
                            PrintStatusBean printStatusBean = HttpUtils.GetJson<PrintStatusBean>(jsonString);
                            WechatPrinterConf.Captcha = printStatusBean.Captcha;
                            ShowCaptcha();
                        }
                        else
                        {
                            bean = HttpUtils.GetJson<PrintImgBean>(WechatPrinterConf.PrintImgUrl, null, true, true);
                        }

                        if (bean != null && bean.ImgUrl != null && !bean.ImgUrl.Equals(String.Empty))
                        {
                            WechatPrinterConf.IsPrinting = true;
                            ShowDownloading();
                            try
                            {
                                string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.PrintImg, bean.ImgUrl, null);
                                if (!filepath.Equals(String.Empty))
                                {
                                    HideDownloading();
                                    PrintImg(filepath, bean.ImgId);
                                    ShowPrintImg(filepath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[GetPrintImg Error]");
                                Console.WriteLine(ex.Message);

                                //TODO 下载出错提示
                                WechatPrinterConf.IsPrinting = false;
                                HideDownloading();
                                ShowNetworkError(ErrorUtils.HandleNetworkError(ErrorUtils.Error.NetworkFileNotFound));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[GetPrintImg Error]");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        ShowNetworkError(ErrorUtils.HandleNetworkError(ErrorUtils.Error.NetworkUnavailable));
                    }
                    finally
                    {
                        isGettingPrintImg = false;
                    }
                });
            }
        }
        #endregion
        #region 返回打印状态
        #region 打印成功
        private void SendPrintSuccess(int imgId)
        {
            BackgroundRun(delegate
            {
                try
                {
                    HideNetworkError();
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param.Add(WechatPrinterConf.ParamKeys.Status, ((int)WechatPrinterConf.PrintImgStatus.Success).ToString());
                    Console.WriteLine("PRINT SUCCESS IMAGE ID: " + imgId.ToString());
                    param.Add(WechatPrinterConf.ParamKeys.Id, imgId.ToString());
                    PrintStatusBean bean = HttpUtils.GetJson<PrintStatusBean>(WechatPrinterConf.PrintImgCallBackUrl, param, true);//TODO 打印成功
                    Console.WriteLine("PRINT SUCCESS RETURN CAPTCHA: " + bean.Captcha);
                    WechatPrinterConf.Captcha = bean.Captcha;
                    ShowCaptcha();
                }
                catch (Exception ex)
                {
                    ShowNetworkError(ErrorUtils.HandleNetworkError(ErrorUtils.Error.NetworkFileNotFound));
                    Console.WriteLine("[SendPrintSuccess Error]");
                    Console.WriteLine(ex.StackTrace);
                }
            });

        }
        #endregion
        #region 打印失败
        private void SendPrintFailed()
        {
            BackgroundRun(delegate
            {
                try
                {
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param.Add(WechatPrinterConf.ParamKeys.Status, ((int)WechatPrinterConf.PrintImgStatus.Fail).ToString());
                    //PrintStatusBean bean = HttpUtils.GetJson<PrintStatusBean>(WechatPrinterConf.PrintImgCallBackUrl, param);//TODO 打印失败

                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SendPrintFail Error]");
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }
        #endregion
        #endregion
        #endregion

        #region 界面更新
        private MainPage page;
        private const double FADE_TIME = 0.5 * 1000;
        private const int PRINT_IMG_DECODE_WIDTH = 600;
        private const int QR_IMG_DECODE_WIDTH = 350;
        private DoubleAnimation fadeInAnim = new DoubleAnimation(1d, TimeSpan.FromMilliseconds(FADE_TIME));
        private DoubleAnimation fadeOutAnim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(FADE_TIME));

        #region 更新二维码
        public void ShowQRImg(string url)
        {
            BackgroundRun(delegate
            {
                try
                {
                    string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.QR, url, null);
                    if (filepath != null && !filepath.Equals(String.Empty))
                    {
                        BitmapImage bi = FileUtils.LoadImage(filepath, QR_IMG_DECODE_WIDTH);
                        page.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            page.image_QR.Source = bi;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ShowQRImg Error");
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }
        #endregion

        #region 更新打印图片
        private void ShowPrintImg(string filepath)
        {
            BitmapImage bi = FileUtils.LoadImage(filepath, PRINT_IMG_DECODE_WIDTH);
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.image_print.Source = bi;
                page.mediaElement_ad.BeginAnimation(System.Windows.Controls.MediaElement.OpacityProperty, fadeOutAnim);
                page.image_print.BeginAnimation(System.Windows.Controls.Image.OpacityProperty, fadeInAnim);
            }));
        }
        private void HidePrintImg()
        {

            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.image_print.BeginAnimation(System.Windows.Controls.Image.OpacityProperty, fadeOutAnim);
                page.mediaElement_ad.BeginAnimation(System.Windows.Controls.MediaElement.OpacityProperty, fadeInAnim);
            }));
        }

        #endregion

        #region 获得并更新广告图片
        private const int AD_IMG_TIMER_INTERVAL = 15 * 1000;
        private const int AD_IMG_DECODE_WIDTH = 300;
        private const int AD_IMG_WAIT_BEFORE_IN = 300;
        private const int AD_IMG_FADE_TIME = 1 * 1000;
        private bool adImgTimerFlag;
        public void ShowAdImg(StringCollection urls, ILoadStage stage)
        {
            BackgroundRun(delegate
            {
                try
                {
                    adImgTimerFlag = true;
                    TimerState adImgTimerState = new TimerState();

                    adImgTimerState.Filepaths = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdImg, urls, null);

                    Timer adImgTimer = new Timer(AdImgTimerCallBack, adImgTimerState, 0, AD_IMG_TIMER_INTERVAL);
                    adImgTimerState.MainTimer = adImgTimer;

                    if (stage != null)
                    {
                        stage.Stage(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ShowAdImg Error]");
                    Console.WriteLine(ex.StackTrace);
                    stage.Stage(-1);
                }
            });

        }

        DoubleAnimation adFadeInAnim = new DoubleAnimation(1d, TimeSpan.FromMilliseconds(AD_IMG_FADE_TIME));
        private void AdImgTimerCallBack(Object state)
        {
            TimerState s = (TimerState)state;
            BitmapImage[] bis = new BitmapImage[3];
            for (int i = 0; i < 3; i++)
            {
                if (s.Counter == s.Filepaths.Count)
                    s.Counter = 0;
                bis[i] = FileUtils.LoadImage(s.Filepaths[s.Counter++], AD_IMG_DECODE_WIDTH);
            }

            if (adImgTimerFlag)
            {
                page.Dispatcher.BeginInvoke(new Action(delegate
                {
                    DoubleAnimation adFadeOutAnim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(AD_IMG_FADE_TIME));
                    adFadeOutAnim.Completed += (o, e) =>
                    {
                        page.image_ad1.Source = bis[0];
                        page.image_ad2.Source = bis[1];
                        page.image_ad3.Source = bis[2];
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Tick += (oo, ee) =>
                        {
                            ((DispatcherTimer)oo).Stop();
                            page.image_ad1.BeginAnimation(MediaElement.OpacityProperty, adFadeInAnim);
                            page.image_ad2.BeginAnimation(MediaElement.OpacityProperty, adFadeInAnim);
                            page.image_ad3.BeginAnimation(MediaElement.OpacityProperty, adFadeInAnim);
                        };
                        timer.Interval = new TimeSpan(0, 0, 0, 0, AD_IMG_WAIT_BEFORE_IN);
                        timer.Start();
                    };
                    page.image_ad1.BeginAnimation(MediaElement.OpacityProperty, adFadeOutAnim);
                    page.image_ad2.BeginAnimation(MediaElement.OpacityProperty, adFadeOutAnim);
                    page.image_ad3.BeginAnimation(MediaElement.OpacityProperty, adFadeOutAnim);
                }));
            }
            else
            {
                s.MainTimer.Dispose();
            }
        }

        public void HideAdImg()
        {
            adImgTimerFlag = false;
        }
        #endregion

        #region 获得并更新广告视频
        private bool adVidInit = false;
        private static StringCollection adVidFilepaths = null;
        private int adVidCounter = 0;
        public void ShowAdVid(StringCollection urls, ILoadStage stage, IDownloadProgress progress = null)
        {
            BackgroundRun(delegate
            {
                try
                {
                    //bool flag = true;
                    //if (adVidFilepaths != null && adVidFilepaths.Count > 0 && adVidFilepaths.Count == urls.Count)
                    //{
                    //    for (int i = 0; i < (adVidFilepaths.Count >= urls.Count ? urls.Count : adVidFilepaths.Count); i++)
                    //    {
                    //        if (adVidFilepaths[i].Equals(urls[i].Substring(urls[i].LastIndexOf("/") + 1)))
                    //        {
                    //            flag = false;
                    //            break;
                    //        }
                    //    }
                    //}
                    //if (flag)
                    //{
                    StringCollection filepaths = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdVid, urls, null, false, progress);
                    adVidCounter = 0;
                    if (filepaths.Count > 0)
                    {
                        adVidFilepaths = filepaths;
                        AdVidEnded(null, null);
                    }
                    if (!adVidInit)
                    {
                        page.mediaElement_ad.MediaEnded += AdVidEnded;
                        adVidInit = true;
                    }
                    //}
                    if (stage != null)
                        stage.Stage(1 << 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ShowAdVid Error]");
                    Console.WriteLine(ex.StackTrace);
                }
            });

        }

        public void AdVidEnded(Object o, EventArgs a)
        {
            if (adVidCounter == adVidFilepaths.Count)
                adVidCounter = 0;
            Uri uri = new Uri(adVidFilepaths[adVidCounter++]);
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.mediaElement_ad.Source = uri;
                //page.mediaElement_ad.Source = new Uri("C:\\Users\\Joe\\Desktop\\深米游戏.mov");
            }));
        }
        #endregion

        #region 更新验证码
        public void ShowCaptcha()
        {
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (WechatPrinterConf.Captcha != -1)
                {
                    page.textBlock_captcha.Text = WechatPrinterConf.Captcha.ToString();
                }
                else
                {
                    page.textBlock_captcha.Text = "服务器出错";
                }

            }));
        }
        #endregion

        #region 更新公司名称
        public void ShowCoName()
        {
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                //if (!WechatPrinterConf.CoName.Equals(String.Empty))
                {
                    page.label_shop.Content = WechatPrinterConf.CoName;
                }
                //else
                {
                    //page.label_shop.Content = "";
                }

            }));
        }
        #endregion
        #endregion


        #region 更新错误
        private const int ERROR_TIMER_INTERVAL = 2 * 1000;

        #region 网络错误
        private bool networkErrorTimerFlag = false;
        public void ShowNetworkError(string errorString)
        {
            if (!networkErrorTimerFlag)
            {
                networkErrorTimerFlag = true;

                TimerState errorTimerState = new TimerState();
                errorTimerState.Filepaths = new StringCollection() { errorString };
                Timer errorTimer = new Timer(NetworkErrorTimerCallBack, errorTimerState, 0, ERROR_TIMER_INTERVAL);
                errorTimerState.MainTimer = errorTimer;
            }
        }
        private void NetworkErrorTimerCallBack(Object state)
        {
            TimerState s = (TimerState)state;
            page.Dispatcher.BeginInvoke(new Action(delegate
                {
                    page.label_network_error.Content = s.Filepaths[0];
                    if (networkErrorTimerFlag)
                    {
                        if (s.Switcher)
                        {
                            page.label_network_error.Opacity = 1d;
                        }
                        else
                        {
                            page.label_network_error.Opacity = 0d;
                        }
                        s.Switcher = !s.Switcher;
                    }
                    else
                    {
                        s.MainTimer.Dispose();
                        page.label_network_error.Opacity = 0d;
                    }
                }));
        }
        public void HideNetworkError()
        {
            networkErrorTimerFlag = false;
        }
        #endregion

        #region 打印机错误
        private bool printerErrorTimerFlag;
        public void ShowPrinterError(PrintQueueStatus status)
        {
            printerErrorTimerFlag = true;
            TimerState errorTimerState = new TimerState();
            errorTimerState.Filepaths = new StringCollection() { ErrorUtils.HandlePrinterError(status) };
            Timer errorTimer = new Timer(PrinterErrorTimerCallBack, errorTimerState, 0, ERROR_TIMER_INTERVAL);
            errorTimerState.MainTimer = errorTimer;
        }
        private void PrinterErrorTimerCallBack(Object state)
        {
            TimerState s = (TimerState)state;
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.label_printer_error.Content = s.Filepaths[0];
                if (printerErrorTimerFlag)
                {
                    if (s.Switcher)
                    {
                        page.label_printer_error.Opacity = 1d;
                    }
                    else
                    {
                        page.label_printer_error.Opacity = 0d;
                    }
                    s.Switcher = !s.Switcher;
                }
                else
                {
                    s.MainTimer.Dispose();
                    page.label_printer_error.Opacity = 0d;
                }
            }));
        }
        public void HidePrinterError()
        {
            printerErrorTimerFlag = false;
        }
        #endregion

        #region 更新正在下载打印图片
        private void ShowDownloading()
        {
            Console.WriteLine("Show Downloading");
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                //page.label_downloading.BeginAnimation(Label.OpacityProperty, fadeInAnim);
                page.label_downloading.Opacity = 1d;
            }));
        }
        private void HideDownloading()
        {
            Console.WriteLine("Hide Downloading");
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                //page.label_downloading.BeginAnimation(Label.OpacityProperty, fadeOutAnim);
                page.label_downloading.Opacity = 0d;
            }));
        }
        #endregion
        #endregion

        #region 打印
        public void PrinterError(PrintQueueStatus status, int imgId)
        {
            ShowPrinterError(status);
        }
        public void PrinterAvailable()
        {
            HidePrinterError();
        }
        public void PrinterCompeleted(int imgId)
        {
            Console.WriteLine("Print Compelete");
            BackgroundRun(delegate
            {
                Thread.Sleep(WechatPrinterConf.PrintWaitTime);
                Console.WriteLine("Print Compelete - Hide");
                HidePrintImg();
                SendPrintSuccess(imgId);
                WechatPrinterConf.IsPrinting = false;
            });
        }
        private void PrintImg(string filepath, int imgId)
        {
            PrinterUtils.Print(filepath, imgId);
        }
        #endregion

        #region Support Classes
        #region JsonBean
        public class PrintImgBean
        {
            public int id { private get; set; }
            public string url { private get; set; }

            public int ImgId { get { return id; } }
            public string ImgUrl { get { return url; } }
            public string Uid { get; set; }
            public int State { get; set; }

        }
        public class PrintStatusBean
        {
            public int verifyCode { private get; set; }
            public int Captcha { get { return verifyCode; } }
        }
        #endregion
        #region TimerState
        public class TimerState
        {
            public bool Switcher = true;
            public int Counter = 0;
            public Timer MainTimer;
            public StringCollection Filepaths;
        }
        #endregion
        #endregion

        public void Dispose()
        {
            if (printImgTimer != null)
                printImgTimer.Dispose();
            adImgTimerFlag = false;
            networkErrorTimerFlag = false;
        }
    }
}