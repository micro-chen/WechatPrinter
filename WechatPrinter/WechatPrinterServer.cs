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

namespace WechatPrinter
{
    public class WechatPrinterServer
    {

        //public volatile bool isListening = false;
        public WechatPrinterServer(MainWindow window) { this.window = window; }
        public delegate void BackgroundDelegate();
        private void BackgroundRun(BackgroundDelegate background)
        {
            new Thread(new ThreadStart(delegate
            {
                try
                {
                    background();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            })).Start();
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
        public bool IsPrintingImg = false;
        private const string toPrintImgRequestUrl = "http://joewoo.pw/printer/require_img.php";
        public void GetToPrintImg()
        {
            if (!IsPrintingImg)
            {
                Console.WriteLine("Start to get print images");
                IsPrintingImg = true;
                try
                {
                    BackgroundRun(delegate
                {
                    PrintImgBean bean = GetJson<PrintImgBean>(toPrintImgRequestUrl);
                    StringCollection filepaths = new StringCollection();
                    if (bean.Urls.Count > 0)
                    {
                        //TODO 获取全部图片并显示到主窗口后打印
                        foreach (string url in bean.Urls)
                        {
                            string filepath = SavePrintImg(url);
                            if (!filepath.Equals(String.Empty))
                            {
                                filepaths.Add(filepath);
                            }
                        }
                    }
                    bean.Urls = filepaths;
                    PrintImg(bean);
                });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[GetToPrintImg Error]");
                    Console.WriteLine(ex.Message);
                    IsPrintingImg = false;
                }
            }
        }
        #endregion
        #region 请求广告图片
        private const string adRequestUrl = "http://joewoo.pw/printer/require_img.php";
        public void GetAdImg()
        {
            try
            {
                BackgroundRun(delegate
                {
                    AdImagesBean bean = GetJson<AdImagesBean>(adRequestUrl);
                    bean = SaveAdImg(bean);
                    StartAdImgTimer(bean.Urls);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        #endregion
        #region 请求广告视频
        #endregion
        private WebResponse DoGet(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Timeout = 10 * 1000;
                request.Method = "GET";
                return request.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DoGet Error]");
                throw ex;
            }

        }
        private T GetJson<T>(string url)
        {
            T bean = default(T);
            try
            {
                using (WebResponse response = DoGet(url))
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            Console.WriteLine("Get Json: " + json);
                            bean = new JavaScriptSerializer().Deserialize<T>(json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[GetJson Error]");
                throw ex;
            }
            return bean;
        }

        #endregion

        #region 文件管理
        private static string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string resPath = rootPath + "res\\";
        private enum ResPaths { PrintImg, AdImg, AdVideo };
        private static string[] pathStr = { resPath + "print\\", resPath + "ad\\img\\", resPath + "ad\\vid\\" };
        private const int folderSizeLimit = 300;

        #region 保存广告图片
        private AdImagesBean SaveAdImg(AdImagesBean bean)
        {
            StringCollection filepaths = new StringCollection();
            foreach (string url in bean.Urls)
            {
                Console.Write("Ad Image Url: " + url + "\t");
                string filepath = SaveImg(ResPaths.AdImg, url);
                if (!filepath.Equals(String.Empty))
                    filepaths.Add(filepath);
            }
            bean.Urls = filepaths;
            return bean;
        }
        #endregion

        #region 保存打印图片
        private string SavePrintImg(string url)
        {
            return SaveImg(ResPaths.PrintImg, url);
        }
        #endregion

        #region Get并保存图片
        private string SaveImg(ResPaths path, string url)
        {
            DeleteOldFiles(path);
            string filepath = pathStr[(int)path];
            string filename = url.Substring(url.LastIndexOf("/") + 1);
            if (!Directory.Exists(filepath) || !File.Exists(filepath + filename))
            {
                Console.WriteLine("File or folder not exists. Start to get image\t" + filename);
                try
                {
                    using (Stream stream = DoGet(url).GetResponseStream())
                    {
                        using (Image img = Image.FromStream(stream))
                        {
                            if (!Directory.Exists(filepath))
                                Directory.CreateDirectory(filepath);
                            img.Save(filepath + filename);
                            return filepath + filename;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File\t{0}\texists. Skip to get image.", filename);
                return filepath + filename;
            }
            return String.Empty;
        }
        #endregion

        #region 删除旧文件
        private void DeleteOldFiles(ResPaths path)
        {
            string filepath = pathStr[(int)path];
            if (Directory.Exists(filepath))
            {
                long size = 0;
                FileInfo[] fis = new DirectoryInfo(filepath).GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                Console.WriteLine("Ad files size: {0}", size);
                if (size > folderSizeLimit * 1024 * 1024)
                {
                    Array.Sort(fis, new FileCreateTimeComparer());
                    foreach (FileInfo fi in fis)
                    {
                        Console.WriteLine("Delete file: " + fi.FullName + "\t\t" + fi.CreationTime);
                        size -= fi.Length;
                        fi.Delete();
                        if (size <= folderSizeLimit)
                            break;
                    }
                }
            }
        }
        #endregion
        #endregion

        #region 界面更新
        private MainWindow window;

        #region 更新图片
        private TimerState adImgTimerState;
        public void StartAdImgTimer(StringCollection filepaths)
        {
            adImgTimerState = new TimerState();
            adImgTimerState.Filepaths = filepaths;
            Timer adImgTimer = new Timer(AdImgTimer, adImgTimerState, 0, 5 * 1000);
            adImgTimerState.MainTimer = adImgTimer;

            Console.WriteLine("Timer started. Images to show:");
            foreach (string filepath in filepaths)
                Console.WriteLine(filepath);
        }
        private void AdImgTimer(Object state)
        {
            TimerState s = (TimerState)state;
            if (s.Run)
                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (s.Counter == s.Filepaths.Count)
                        s.Counter = 0;
                    window.mediaElement_ad2.Source = new Uri(s.Filepaths[s.Counter++]);
                }));
            else
                s.MainTimer.Dispose();
        }
        public void StopAdImgTimer()
        {
            adImgTimerState.Run = false;
        }
        #endregion

        #region 更新视频

        #endregion
        #endregion

        #region 打印
        public void PrintImg(PrintImgBean bean)
        {
            BackgroundRun(delegate
            {
                var pd = new System.Windows.Controls.PrintDialog();
                PrintQueueCollection pqs = new LocalPrintServer().GetPrintQueues();
                foreach (PrintQueue pq in pqs)
                {
                    if (pq.Name.Equals("Microsoft XPS Document Writer"))
                    {
                        pd.PrintQueue = pq;
                        foreach (string filepath in bean.Urls)
                        {
                            Console.WriteLine(pq.Name + " print\t" + filepath);
                            using (Image img = Image.FromFile(filepath))
                            {
                                if (img.Width > img.Height)
                                {
                                    img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                }

                                using (Graphics gr = Graphics.FromImage(img))
                                {
                                    gr.SmoothingMode = SmoothingMode.HighQuality;
                                    gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    gr.DrawImage(img, new Rectangle(0, 0, 1280, 1920));
                                }

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    img.Save(ms, ImageFormat.Bmp);
                                    ms.Seek(0, SeekOrigin.Begin);
                                    BitmapImage bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.StreamSource = ms;
                                    bi.EndInit();

                                    var vis = new DrawingVisual();
                                    var dc = vis.RenderOpen();
                                    dc.DrawImage(bi, new Rect { Width = bi.Width, Height = bi.Height });
                                    dc.Close();

                                    pd.PrintVisual(vis, "Wechat Printer Image");
                                }
                            }

                        }
                        IsPrintingImg = false;
                        break;
                    }
                }
            });

        }

        #endregion
    }

    #region Support Classes
    #region JsonBean
    public class AdImagesBean
    {
        public StringCollection Urls { get; set; }
    }
    public class PrintImgBean
    {
        public StringCollection Urls { get; set; }
    }
    #endregion
    #region TimerState
    class TimerState
    {
        public bool Run = true;
        public int Counter = 0;
        public Timer MainTimer;
        public StringCollection Filepaths;
    }
    #endregion
    #region FileCreateTimeComparer
    class FileCreateTimeComparer : IComparer
    {
        int IComparer.Compare(Object o1, Object o2)
        {
            FileInfo fi1 = o1 as FileInfo;
            FileInfo fi2 = o2 as FileInfo;
            return fi1.CreationTime.CompareTo(fi2.CreationTime);
        }
    }
    #endregion
    #endregion
}