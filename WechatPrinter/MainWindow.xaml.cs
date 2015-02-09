using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace WechatPrinter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        WechatPrinterServer server;

        public MainWindow()
        {
            InitializeComponent();
            //this.WindowState = WindowState.Maximized;
            //this.WindowStyle = WindowStyle.None;
            //this.ResizeMode = System.Windows.ResizeMode.NoResize;
            //this.Topmost = true;
            
        }

        private void MainWindow1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
            else if (e.Key == Key.Enter)
                server.GetAdImg();
            else if (e.Key == Key.J)
                server.StopAdImgTimer();
            else if (e.Key == Key.Space)
            {
                server.GetToPrintImg();
                Console.WriteLine("space");
            }
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            mediaElement_ad.Source = new Uri("http://ww1.sinaimg.cn/bmiddle/6cf8a22bjw1eodufymcyqj218g0tndqr.jpg");
            mediaElement_ad.Play();
            mediaElement_ad2.Source = new Uri("http://ww1.sinaimg.cn/bmiddle/6cf8a22bjw1eodufymcyqj218g0tndqr.jpg");
            mediaElement_ad2.Play();

            server = new WechatPrinterServer(this);
            //server.PrintImg("E:\\肇庆\\000002.JPG");
        }

        private void mediaElement_AdVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement_ad.Position = TimeSpan.Zero;
            mediaElement_ad.Play();
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
