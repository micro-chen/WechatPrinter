using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WechatPrinter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, ILoadStatus
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

        }
        public void LoadCompleted(MainPage page, WechatPrinterServer server)
        {
            Content = page;
            page.Opacity = 0d;
            page.BeginAnimation(OpacityProperty, new DoubleAnimation(1d, TimeSpan.FromMilliseconds(1000)));

            server.StartCheckPrintImg();
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            Content = new LoadingPage(this);
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
