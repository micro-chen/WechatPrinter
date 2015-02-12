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
    public partial class LoadingPage : Page, ILoadStage
    {
        ILoadStatus status;

        //"http://ww1.sinaimg.cn/bmiddle/6cf8a22bjw1eodufymcyqj218g0tndqr.jpg"

        public static string INFO_URL = "http://joewoo.pw/printer/info.php";
        private MainPage page;

        public LoadingPage(ILoadStatus status)
        {
            InitializeComponent();
            this.status = status;
        }

        private void page_loading_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Page Init");

            InfoBean info = null;
            int retry = 0;
            label_loading.Content = "连接微信打印服务器...";

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, ee) =>
            {
                try
                {
                    ee.Result = HttpUtils.GetJson<InfoBean>(INFO_URL);
                }
                catch (Exception ex)
                {
                    retry++;
                    Console.WriteLine("[LoadingPage:Get InfoBean Error {0}]", retry);
                    Console.WriteLine(ex.Message);
                    ee.Result = null;
                }
            };
            bw.RunWorkerCompleted += (o, ee) =>
            {
                if (ee.Result == null)
                {
                    StringBuilder sb = new StringBuilder("重试连接微信打印服务器...[");
                    sb.Append(retry).Append("/3]");
                    label_loading.Content = sb.ToString();
                    if (retry < 3)
                    {
                        bw.RunWorkerAsync();
                    }
                    else
                    {
                        Window window = (MainWindow)Window.GetWindow(this);
                        MessageBox.Show("连接微信打印错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        window.Close();
                    }
                }
                else
                {
                    info = (InfoBean)ee.Result;

                    page = new MainPage();
                    WechatPrinterServer server = new WechatPrinterServer(page, info);
                    page.Server = server;

                    server.ShowAdVid(info.AdVideosUrl, this);
                    page.mediaElement_ad.Visibility = Visibility.Visible;
                    page.mediaElement_ad.Play();

                    server.ShowAdImg(info.AdImagesUrl, this);
                    page.mediaElement_ad2.Visibility = Visibility.Visible;
                    page.mediaElement_ad2.Play();

                    page.mediaElement_QR.Source = new Uri(info.QRCodeUrl);
                    page.mediaElement_QR.Visibility = Visibility.Visible;

                    server.ShowCaptcha(info.CaptchaUrl);
                    page.label_captcha.Visibility = Visibility.Visible;

                    Stage(0);
                }
            };
            bw.RunWorkerAsync();
        }

        private const int LOADING_WAIT_TIME = 0 * 1000;
        private static int sum = 0;
        public void Stage(int stage)
        {
            sum += stage;
            page.Dispatcher.BeginInvoke(new Action(delegate
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



    public class InfoBean
    {
        public string AdImagesUrl { get; set; }
        public string AdVideosUrl { get; set; }
        public string QRCodeUrl { get; set; }
        public string CaptchaUrl { get; set; }
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
