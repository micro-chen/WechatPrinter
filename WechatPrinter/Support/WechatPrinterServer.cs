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
    public class WechatPrinterServer : IPrinterStatus
    {

        public static string PRINT_IMG_URL = "http://joewoo.pw/printer/printimg.php";
        private InfoBean info;

        private void BackgroundRun(Action action)
        {
            ThreadPool.QueueUserWorkItem(delegate { action(); });
        }
        public WechatPrinterServer(MainPage page, InfoBean info)
        {
            this.page = page;
            this.info = info;
            PrinterUtils.Start(this);
        }

        #region 监听请求
        //    private int listenPort = 2333;
        //    private string listenIp = "127.0.0.1";
        //    private string requireIpUrl = "http://clzroom.jd-app.com/printer/require_ip.php";
        //    private HttpListener listener;
        //    public void Listen()
        //    {
        //        if (!isListening)
        //        {
        //            try
        //            {
        //                new Thread(new ThreadStart(HandleListen)).Start();
        //            }
        //            catch
        //            {
        //                MessageBox.Show("ERROR");
        //            }
        //        }
        //    }
        //    private void HandleListen()
        //    {
        //        string localIp = GetLocalIp();
        //        if (localIp != null && localIp.Length > 0)
        //        {
        //            listenIp = GetLocalIp();
        //            Console.WriteLine("Local IP: " + GetLocalIp());
        //        }
        //        isListening = true;
        //        listener = new HttpListener();
        //        listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        //        listener.Prefixes.Add("http://" + listenIp + ":" + listenPort + "/");
        //        Console.WriteLine("Start Listen: http://" + listenIp + ":" + listenPort + "/");
        //        listener.Start();
        //        listener.BeginGetContext(new AsyncCallback(GetContextCallBack), listener);
        //    }
        //    private void GetContextCallBack(IAsyncResult ar)
        //    {
        //        try
        //        {
        //            listener = ar.AsyncState as HttpListener;
        //            HandleRequest(listener.EndGetContext(ar));
        //            listener.BeginGetContext(new AsyncCallback(GetContextCallBack), listener);
        //        }
        //        catch(Exception ex)
        //        {
        //            Console.WriteLine("ERROR - GetContextCallBack(): " + ex.Message);
        //        }
        //    }
        //    private void HandleRequest(HttpListenerContext context)
        //    {
        //        HttpListenerRequest request = context.Request;
        //        HttpListenerResponse response = context.Response;
        //        Console.WriteLine("Request URL: " + request.Url);
        //        if (request.HasEntityBody)
        //        {
        //            string json;
        //            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
        //            {
        //                json = reader.ReadToEnd();
        //                reader.Close();
        //                request.InputStream.Close();
        //                Console.WriteLine(json);
        //            }
        //            HandleResponse(response, 200);
        //        }
        //        else
        //            HandleResponse(response, 400);
        //    }
        //    private void HandleResponse(HttpListenerResponse response, int status)
        //    {
        //        response.StatusCode = status;
        //        using (StreamWriter writer = new StreamWriter(response.OutputStream))
        //        {
        //            if (status == 200)
        //                writer.Write("-SUCCESS-");
        //            else
        //                writer.Write("!FAIL!");
        //            writer.Close();
        //            response.Close();
        //        }
        //    }
        //    public void Stop()
        //    {
        //        isListening = false;
        //        if (!isListening)
        //            listener.Stop();
        //    }
        //    private string GetRemoteIp()
        //    {
        //        string realIp = listenIp;
        //        try
        //        {
        //            realIp = new WebClient().DownloadString(requireIpUrl);
        //        }
        //        catch
        //        {
        //            Console.Write("GetIp network failed. Return ");
        //        }
        //        Console.WriteLine("IP " + realIp);
        //        return realIp;
        //    }
        //    private string GetLocalIp()
        //    {
        //        IPAddress[] localIPs;
        //        localIPs = Dns.GetHostAddresses(Dns.GetHostName());
        //        string localIp = listenIp;
        //        foreach (IPAddress ip in localIPs)
        //        {
        //            if (ip.AddressFamily == AddressFamily.InterNetwork) {
        //                localIp = ip.ToString();
        //                break;
        //            }
        //        }
        //        return localIp;
        //    }
        #endregion

        #region 发送请求
        #region 获得打印图片
        public void GetToPrintImg(string url)
        {
            BackgroundRun(delegate
            {
                Console.WriteLine("Start to get print images");
                try
                {
                    PrintImgBean bean = HttpUtils.GetJson<PrintImgBean>(url);
                    if (bean.Url != null && bean.Url != String.Empty)
                    {
                        ShowDownloading();
                        try
                        {
                            string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.PrintImg, bean.Url);
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
                    Console.WriteLine(ex.Message);
                    ShowError(ErrorUtils.HandleError(ErrorUtils.Error.NetworkUnavailable));
                }
            });
        }
        #endregion

        //#region 请求广告图片
        //private AdImagesBean GetAdImg(string url)
        //{
        //    AdImagesBean bean = null;
        //    try
        //    {
        //        bean = HttpUtils.GetJson<AdImagesBean>(url);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("[SaveAdImg Error]");
        //        Console.WriteLine(ex.Message);
        //    }
        //    return bean;
        //}
        //#endregion

        #region 请求广告视频
        #endregion
        #endregion

        //#region 文件管理
        //#region 保存广告图片
        //private AdImagesBean SaveAdImg(AdImagesBean bean)
        //{
        //    StringCollection filepaths = new StringCollection();
        //    foreach (string url in bean.Urls)
        //    {
        //        try
        //        {
        //            Console.Write("Ad Image Url: " + url + "\t");
        //            string filepath = HttpUtils.GetImg(FileUtils.ResPathsEnum.AdImg, url);
        //            if (!filepath.Equals(String.Empty))
        //                filepaths.Add(filepath);
        //        }
        //        catch(Exception ex)
        //        {
        //            Console.WriteLine("[SaveAdImg Error]");
        //            Console.WriteLine(ex.Message);
        //        }
        //    }
        //    bean.Urls = filepaths;
        //    return bean;
        //}
        //#endregion

        //#region 保存打印图片
        //private string SavePrintImg(string url)
        //{
        //    return HttpUtils.GetImg(FileUtils.ResPathsEnum.PrintImg, url);
        //}
        //#endregion
        //#endregion

        #region 界面更新
        private MainPage page;
        private const double FADE_TIME = 0.5 * 1000;
        private const int PRINT_IMG_DECODE_WIDTH = 600;
        private DoubleAnimation fadeInAnim = new DoubleAnimation(1d, TimeSpan.FromMilliseconds(FADE_TIME));
        private DoubleAnimation fadeOutAnim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(FADE_TIME));

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
        private const int AD_IMG_DECODE_WIDTH = 700;
        private const int AD_IMG_WAIT_BEFORE_IN = 300;
        private const int AD_IMG_FADE_TIME = 1 * 1000;
        private bool adImgTimerFlag;
        public void ShowAdImg(string url, ILoadStage stage)
        {
            BackgroundRun(delegate
            {
                try
                {
                    adImgTimerFlag = true;
                    TimerState adImgTimerState = new TimerState();

                    adImgTimerState.Filepaths = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdImg, HttpUtils.GetJson<AdImagesBean>(url).Urls);

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
            if (adImgTimerFlag)
            {
                page.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (s.Counter == s.Filepaths.Count)
                        s.Counter = 0;
                    DoubleAnimation adFadeOutAnim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(AD_IMG_FADE_TIME));
                    adFadeOutAnim.Completed += (o, e) =>
                    {
                        page.mediaElement_ad2.Source = new Uri(s.Filepaths[s.Counter++]);
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Tick += (oo, ee) =>
                        {
                            ((DispatcherTimer)oo).Stop();
                            page.mediaElement_ad2.BeginAnimation(MediaElement.OpacityProperty, adFadeInAnim);
                        };
                        timer.Interval = new TimeSpan(0, 0, 0, 0, AD_IMG_WAIT_BEFORE_IN);
                        timer.Start();
                    };
                    page.mediaElement_ad2.BeginAnimation(MediaElement.OpacityProperty, adFadeOutAnim);
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
        private static AdVideosBean adVidLastBean = null;
        private int adVidCounter = 0;
        public void ShowAdVid(string url, ILoadStage stage)
        {
            BackgroundRun(delegate
            {
                try
                {
                    AdVideosBean bean = HttpUtils.GetJson<AdVideosBean>(url);
                    bool flag = true;
                    if (adVidLastBean != null && bean.Urls.Count > 0 && bean.Urls.Count == adVidLastBean.Urls.Count)
                    {
                        StringCollection last = adVidLastBean.Urls;
                        StringCollection now = bean.Urls;
                        for (int i = 0; i < (last.Count >= now.Count ? now.Count : last.Count); i++)
                        {
                            if (last[i].Equals(now[i].Substring(now[i].LastIndexOf("/") + 1)))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        StringCollection filepaths = HttpUtils.GetFiles(FileUtils.ResPathsEnum.AdVid, bean.Urls);
                        adVidCounter = 0;
                        if (filepaths.Count > 0)
                        {
                            adVidLastBean = bean;
                            adVidLastBean.Urls = filepaths;
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
            StringCollection filepaths = adVidLastBean.Urls;
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
        public void ShowCaptcha(string url)
        {
            BackgroundRun(delegate
            {
                string captcha = HttpUtils.GetText(url);
                if (captcha != null && !captcha.Equals(String.Empty))
                {
                    page.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            page.textBlock_captcha.Text = captcha;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[ShowCaptcha Error]");
                            Console.WriteLine(ex.Message);
                        }
                    }));
                }
            });
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
            HidePrintImg();
        }
        private void PrintImg(string filepath)
        {
            PrinterUtils.Print(filepath);
        }
        #endregion
    }

    #region Support Classes
    #region JsonBean
    public class AdImagesBean
    {
        public StringCollection Urls { get; set; }
    }
    public class AdVideosBean
    {
        public StringCollection Urls { get; set; }
    }
    public class PrintImgBean
    {
        public String Url { get; set; }
    }
    #endregion
    #region TimerState
    class TimerState
    {
        public bool Switcher = true;
        public int Counter = 0;
        public Timer MainTimer;
        public StringCollection Filepaths;
    }
    #endregion

    #endregion
}