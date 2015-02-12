using System;
using System.Collections.Generic;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            //this.WindowState = WindowState.Maximized;
            //this.WindowStyle = WindowStyle.None;
            //this.ResizeMode = System.Windows.ResizeMode.NoResize;
            //this.Topmost = true;       

        }
        public void LoadCompleted(MainPage page)
        {
            page.Opacity = 0d;
            page.BeginAnimation(Page.OpacityProperty, new DoubleAnimation(1d, TimeSpan.FromMilliseconds(1000)));
            this.Content = page;
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            this.Content = new LoadingPage(this);
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
