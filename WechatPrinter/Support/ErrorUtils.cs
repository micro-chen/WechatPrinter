using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string HandleError(Error error)
        {
            switch (error)
            {
                case Error.NetworkFileNotFound:
                case Error.NetworkGatewayTimeout:
                case Error.NetworkRequestTimeout:
                case Error.NetworkUnavailable:
                    return "网络错误";
                case Error.PrinterDoorOpened:
                case Error.PrinterOutOfInk:
                case Error.PrinterOutOfPaper:
                case Error.PrinterPaperJammed:
                case Error.PrinterUnavailable:
                    return "打印出错";
                default:
                    return "未知错误";
            }
        }
    }

}
