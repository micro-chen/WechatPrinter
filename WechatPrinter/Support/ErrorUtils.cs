using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WechatPrinter.Support
{
    public class ErrorUtils
    {
        private const int ERROR_TIMER_INTERVAL = 2 * 1000;
        public enum Error { NetworkGatewayTimeout, NetworkRequestTimeout, NetworkFileNotFound, NetworkUnavailable, PrinterOutOfPaper, PrinterOutOfInk, PrinterPaperJammed, PrinterDoorOpened, PrinterUnavailable };

        public static string HandleNetworkError(Error error)
        {
            switch (error)
            {
                case Error.NetworkFileNotFound:
                case Error.NetworkGatewayTimeout:
                case Error.NetworkRequestTimeout:
                case Error.NetworkUnavailable:
                    return "网络错误";
                //case Error.PrinterDoorOpened:
                //case Error.PrinterOutOfInk:
                //case Error.PrinterOutOfPaper:
                //case Error.PrinterPaperJammed:
                //case Error.PrinterUnavailable:
                //    return "打印出错";
                default:
                    return "未知错误";
            }
        }

        public static string HandlePrinterError(PrintQueueStatus status)
        {
            switch (status)
            {
                case PrintQueueStatus.PaperOut:
                    return "打印机缺纸";
                case PrintQueueStatus.PaperJam:
                    return "打印机卡纸";
                case PrintQueueStatus.PaperProblem:
                    return "打印机纸张错误";
                case PrintQueueStatus.TonerLow:
                    return "打印机少墨";
                case PrintQueueStatus.NoToner:
                    return "打印机缺墨";
                case PrintQueueStatus.ServerUnknown:
                case PrintQueueStatus.NotAvailable:
                case PrintQueueStatus.Offline:
                    return "打印机连接错误";
                default:
                    return "打印机未知错误";
            }
        }
    }

}
