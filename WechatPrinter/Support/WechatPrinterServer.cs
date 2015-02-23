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
    public class WechatPrinterServer : IPrinterStatus,IDisposable
    {

        private void BackgroundRun(Action action)
        {
            ThreadPool.QueueUserWorkItem(delegate { action(); });
        }
        public WechatPrinterServer(MainPage page)
        {
            this.page = page;
            PrinterUtils.Start(this, page);
        }


        #region 发送请求
        #region 定时查询打印图片
        private Timer printImgTimer;
        public void CheckPrintImg()
        {
            printImgTimer = new Timer(GetPrintImg, WechatPrinterConf.PrintImgUrl, 0, WechatPrinterConf.PrintImgInterval);
        }
        public void StopPrintImg()
        {
            printImgTimer.Dispose();
        }
        #endregion

        #region 获得打印图片
        private void GetPrintImg(Object o)
        {
            BackgroundRun(delegate
            {
                Console.WriteLine("Start to get print images");
                try
                {
                    PrintImgBean bean = HttpUtils.GetJson<PrintImgBean>((string)o, null);
                    if (bean.ImgUrl != null && bean.ImgUrl != String.Empty)
                    {
                        ShowDownloading();
                        try
                        {
                            //string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.PrintImg, bean.Url);
                            string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.PrintImg, WechatPrinterConf.PrintImgUrl, null);
                            if (!filepath.Equals(String.Empty))
                            {
                                HideDownloading();
                                PrintImg(filepath);
                                ShowPrintImg(filepath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[GetToPrintImg:for Error]");
                            Console.WriteLine(ex.Message);

                            //TODO 下载出错提示
                            HideDownloading();
                            ShowError(ErrorUtils.HandleError(ErrorUtils.Error.NetworkFileNotFound));
                        }


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[GetToPrintImg Error]");
                    Console.WriteLine(ex.StackTrace);
                    ShowError(ErrorUtils.HandleError(ErrorUtils.Error.NetworkUnavailable));
                }
            });
        }
        #endregion
        #region 返回打印状态
        #region 打印成功
        private void SendPrintSuccess()
        {
            BackgroundRun(delegate
            {
                try
                {
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param.Add(WechatPrinterConf.ParamKeys.Status, ((int)WechatPrinterConf.PrintImgStatus.Success).ToString());
                    PrintStatusBean bean = HttpUtils.GetJson<PrintStatusBean>(WechatPrinterConf.PrintImgCallBackUrl, param);//TODO 打印成功
                    if (bean.Status == (int)WechatPrinterConf.PrintImgStatus.Success)
                    {
                        ShowCaptcha(bean.Captcha);
                    }
                    else
                    {
                        ShowCaptcha(-1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SendPrintSuccess Error]");
                    Console.WriteLine(ex.StackTrace);
                }
            });

        }
        #endregion
        #region 打印失败
        private void SendPrintFailed()
        {

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
            BackgroundRun(delegate {
                try
                {
                    string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.QR, url, null);
                    if (filepath != null && !filepath.Equals(String.Empty))
                    {
                        BitmapImage bi = FileUtils.LoadImage(filepath, QR_IMG_DECODE_WIDTH);
                        page.Dispatcher.BeginInvoke(new Action(delegate {
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
                }
            });

        }

        DoubleAnimation adFadeInAnim = new DoubleAnimation(1d, TimeSpan.FromMilliseconds(AD_IMG_FADE_TIME));
        private void AdImgTimerCallBack(Object state)
        {
            TimerState s = (TimerState)state;
            BitmapImage bi = FileUtils.LoadImage(s.Filepaths[s.Counter++], AD_IMG_DECODE_WIDTH);
            if (adImgTimerFlag)
            {
                page.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (s.Counter == s.Filepaths.Count)
                        s.Counter = 0;
                    DoubleAnimation adFadeOutAnim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(AD_IMG_FADE_TIME));
                    adFadeOutAnim.Completed += (o, e) =>
                    {
                        page.image_ad1.Source = bi;
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Tick += (oo, ee) =>
                        {
                            ((DispatcherTimer)oo).Stop();
                            page.image_ad1.BeginAnimation(MediaElement.OpacityProperty, adFadeInAnim);
                        };
                        timer.Interval = new TimeSpan(0, 0, 0, 0, AD_IMG_WAIT_BEFORE_IN);
                        timer.Start();
                    };
                    page.image_ad1.BeginAnimation(MediaElement.OpacityProperty, adFadeOutAnim);
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
        public void ShowAdVid(StringCollection urls, ILoadStage stage)
        {
            BackgroundRun(delegate
            {
                try
                {
                    bool flag = true;
                    if (adVidFilepaths != null && adVidFilepaths.Count > 0 && adVidFilepaths.Count == urls.Count)
                    {
                        for (int i = 0; i < (adVidFilepaths.Count >= urls.Count ? urls.Count : adVidFilepaths.Count); i++)
                        {
                            if (adVidFilepaths[i].Equals(urls[i].Substring(urls[i].LastIndexOf("/") + 1)))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        StringCollection filepaths = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdVid, urls, null);
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
                    }
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
            StringCollection filepaths = adVidFilepaths;
            if (adVidCounter == filepaths.Count)
                adVidCounter = 0;
            Uri uri = new Uri(filepaths[adVidCounter++]);
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.mediaElement_ad.Source = uri;
            }));
        }
        #endregion

        #region 获得并更新验证码
        public void ShowCaptcha(int captcha)
        {
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (captcha != -1)
                {
                    page.textBlock_captcha.Text = captcha.ToString();
                }
                else
                {
                    page.textBlock_captcha.Text = "服务器出错";
                }

            }));
        }
        #endregion



        #region 更新错误
        private const int ERROR_TIMER_INTERVAL = 2 * 1000;
        private bool errorTimerFlag;
        public void ShowError(string errorString)
        {
            errorTimerFlag = true;
            TimerState errorTimerState = new TimerState();
            errorTimerState.Filepaths = new StringCollection() { errorString };
            Timer errorTimer = new Timer(ErrorTimerCallBack, errorTimerState, 0, ERROR_TIMER_INTERVAL);
            errorTimerState.MainTimer = errorTimer;
        }
        private void ErrorTimerCallBack(Object state)
        {
            TimerState s = (TimerState)state;
            page.Dispatcher.BeginInvoke(new Action(delegate
                {
                    page.label_error.Content = s.Filepaths[0];
                    if (errorTimerFlag)
                    {
                        if (s.Switcher)
                        {
                            page.label_error.Opacity = 1d;
                        }
                        else
                        {
                            page.label_error.Opacity = 0d;
                        }
                        s.Switcher = !s.Switcher;
                    }
                    else
                    {
                        s.MainTimer.Dispose();
                        page.label_error.Opacity = 0d;
                    }
                }));
        }
        public void HideError()
        {
            errorTimerFlag = false;
        }
        #endregion

        #region 更新正在下载打印图片
        private void ShowDownloading()
        {
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.label_downloading.BeginAnimation(Label.OpacityProperty, fadeInAnim);
            }));
        }
        private void HideDownloading()
        {
            page.Dispatcher.BeginInvoke(new Action(delegate
            {
                page.label_downloading.BeginAnimation(Label.OpacityProperty, fadeOutAnim);
            }));
        }
        #endregion
        #endregion

        #region 打印
        public void PrinterError(ErrorUtils.Error error)
        {
            //TODO 打印机错误
        }
        public void PrinterAvailable()
        {
            //TODO 打印机恢复
        }
        public void PrinterCompeleted()
        {
            Console.WriteLine("Printer Compelete");
            HidePrintImg();
            SendPrintSuccess();
        }
        private void PrintImg(string filepath)
        {
            PrinterUtils.Print(filepath);
        }
        #endregion

        #region Support Classes
        #region JsonBean
        public class PrintImgBean
        {
            public string ImgUrl { get; set; }
        }
        public class PrintStatusBean
        {
            public int Status { get; set; }
            public int Captcha { get; set; }
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
            if(printImgTimer!=null)
                printImgTimer.Dispose();
            adImgTimerFlag = false;
            errorTimerFlag = false;
        }
    }
}