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
            //int retry = 0;
            label_loading.Content = "连接微信打印服务器...";

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, ee) =>
            {
                try
                {
                    Dictionary<string, string> param = new Dictionary<string,string>();
                    param.Add(WechatPrinterConf.ParamKeys.Id, WechatPrinterConf.Id);
                    ee.Result = HttpUtils.GetJson<InfoBean>(WechatPrinterConf.InitUrl, param, true);
                }
                catch (Exception ex)
                {
                    //retry++;
                    //Console.WriteLine("[LoadingPage:Get InfoBean Error {0}]", retry);
                    Console.WriteLine("LoadingPage:Get InfoBean Error");
                    Console.WriteLine(ex.Message);
                    ee.Result = null;
                }
            };
            bw.RunWorkerCompleted += (o, ee) =>
            {
                if (ee.Result == null)
                {
                    //StringBuilder sb = new StringBuilder("重试连接微信打印服务器...[");
                    //sb.Append(retry).Append("/3]");
                    //label_loading.Content = sb.ToString();
                    //if (retry < 3)
                    //{
                    //    bw.RunWorkerAsync();
                    //}
                    //else
                    //{
                        Window window = (MainWindow)Window.GetWindow(this);
                        MessageBox.Show("连接微信打印服务器错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        window.Close();
                    //}
                }
                else
                {
                    info = (InfoBean)ee.Result;

                    WechatPrinterConf.Init(info);

                    page = new MainPage();
                    WechatPrinterServer server = new WechatPrinterServer(page);
                    page.Server = server;

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
        public StringCollection videoUrl { get; set; }
        public StringCollection picUrl { get; set; }
        public string qrcodeUrl { get; set; }
        public int verifyCode { get; set; }
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
