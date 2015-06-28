using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WechatPrinter.Support;

namespace WechatPrinter
{
    /// <summary>
    /// Loading.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingPage : Page, ILoadStage, IDownloadProgress
    {
        ILoadStatus status;

        //"http://ww1.sinaimg.cn/bmiddle/6cf8a22bjw1eodufymcyqj218g0tndqr.jpg"

        private MainPage page;

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

                            WechatPrinterConf.Init(info);

                            page = new MainPage();
                            WechatPrinterServer server = new WechatPrinterServer(page);
                            page.Server = server;

                            try
                            {
                                server.ShowCoName();

                                server.ShowQRImg(WechatPrinterConf.QRCodeUrl);

                                server.ShowAdVid(WechatPrinterConf.AdVidUrls, this);
                                page.mediaElement_ad.Opacity = 1d;
                                page.mediaElement_ad.Play();

                                server.ShowAdImg(WechatPrinterConf.AdImgUrls, this);
                                page.image_ad1.Opacity = 1d;
                                page.image_ad2.Opacity = 1d;
                                page.image_ad3.Opacity = 1d;


                                server.ShowCaptcha();
                                page.label_captcha.Opacity = 1d;

                                Stage(0);
                            }
                            catch
                            {
                                MessageBox.Show("微信打印服务器正在维护", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                Window.GetWindow(this).Close();
                            }

                        }
                    }
                };
                bw.RunWorkerAsync();
            }
        }

        private const int LOADING_WAIT_TIME = 0 * 1000;
        private static int sum = 0;
        public void Stage(int stage)
        {
            if (stage == -1)
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
                sum += stage;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    switch (sum)
                    {
                        case 0:
                            label_loading.Content = "加载资源...";
                            break;
                        case 1:
                            label_loading.Content = "图片加载完成，加载视频...";
                            break;
                        case 1 << 1:
                            label_loading.Content = "视频加载完成，加载图片...";
                            break;
                        default:
                            label_loading.Content = "资源加载完成，启动中...";
                            DispatcherTimer timer = new DispatcherTimer();
                            timer.Tick += (o, e) =>
                            {
                                ((DispatcherTimer)o).Stop();
                                status.LoadCompleted(page);
                            };
                            timer.Interval = new TimeSpan(0, 0, 0, 0, LOADING_WAIT_TIME);
                            timer.Start();
                            break;
                    }
                }));
            }

        }

        private long vidLength = 0;
        public void Progress(long total, long read)
        {
            if (total > 0 && read == -2)
            {
                vidLength = total;
            }
            if (sum == 1 && total == -2 && read > 0)
            {
                Dispatcher.BeginInvoke(new Action(delegate
               { 
                   label_loading.Content = "图片加载完成，加载视频... " + Convert.ToString((int)(((double)read / (double)total) * 100)) + "%"; 
               }));
            }
        }
    }



    public class InfoBean
    {
        public StringCollection videoUrl { get; set; }
        public StringCollection picUrl { get; set; }
        public string qrcodeUrl { get; set; }
        public int verifyCode { get; set; }
        public string name { get; set; }
    }

    public interface ILoadStatus
    {
        void LoadCompleted(MainPage page);
    }

    public interface ILoadStage
    {
        void Stage(int stage);
    }
}
