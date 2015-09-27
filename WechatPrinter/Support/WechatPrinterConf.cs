using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace WechatPrinter.Support
{
    class WechatPrinterConf
    {

        public const bool DEBUG = false;

        private const ulong WECHAT_PRINTER_ID = 2L;
        private const string WECHAT_PRINTER_TOKEN = ""; //TODO token

        private const string INIT_URL = "http://114.215.80.157/Home/Printer/login";
        public const string PRE_URL = "http://114.215.80.157";
        private const string PRINT_IMG_URL = "http://114.215.80.157/Home/Printer/getpic";
        private const string PRINT_IMG_CALLBACK_URL = "http://114.215.80.157/Home/Printer/stateReceive";

        private const int PRINT_IMG_TIMER_INTERVAL = 5 * 1000;
        private const int LOADED_WAIT_TIME = 2 * 1000;
        private const int PRINT_WAIT_TIME = 10 * 1000;

        private const string PRINTER_NAME = "Canon iP2700 series";
        //private const string PRINTER_NAME = "EPSON L300 Series";
        //private const string PRINTER_NAME = "Microsoft XPS Document Writer";

        private const double PRINT_DPI = 300;
        private const double SCREEN_DPI = 96;
        private const double PRINT_RATIO = SCREEN_DPI / PRINT_DPI;
        private const double PRINT_EDGE = 832 * 0.0377;
        private const double PRINT_WIDTH = 832 - PRINT_EDGE;
        private const double PRINT_HEIGHT = PRINT_WIDTH;
        private const double PRINT_HEIGHT_POS = PRINT_EDGE * PRINT_RATIO;
        private const double PRINT_WIDTH_POS = (810 + PRINT_EDGE) * PRINT_RATIO;
        private const double PRINT_LOGO_HEIGHT = 300;
        //private const double PRINT_LOGO_WIDTH = PRINT_LOGO_HEIGHT * 1.8;
        private const double PRINT_LOGO_HEIGHT_POS = (PRINT_HEIGHT + PRINT_EDGE * 2.5) * PRINT_RATIO;
        private const double PRINT_LOGO_WIDTH_POS = PRINT_WIDTH_POS;
        private const double PRINT_QR_HEIGHT = 216;
        private const double PRINT_QR_HEIGHT_POS = (PRINT_HEIGHT + PRINT_EDGE * 2) * PRINT_RATIO;
        private const double PRINT_QR_WIDTH_POS = PRINT_WIDTH_POS + (PRINT_WIDTH - PRINT_QR_HEIGHT) * PRINT_RATIO;

        private static bool isPrinting = false;

        private const int HTTP_RETRY_TIMES = 1;
        private const int HTTP_TIMTOUT = 30 * 1000;

        public static bool Init(StringCollection adImgFilepaths, StringCollection adVidFilepaths, string logoFilepath, string qrCodeFilepath,string printQRCodeFilepath, string coName, int captcha)
        {
            bool flag = true;
            AdImgFilepaths = adImgFilepaths;
            AdVidFilepaths = adVidFilepaths;
            QRCodeFilepath = qrCodeFilepath;
            PrintQRCodeFilepath = printQRCodeFilepath;
            LogoFilepath = logoFilepath;
            Captcha = captcha;
            CoName = coName;
            
            if (QRCodeFilepath.Equals(String.Empty))
                flag = false;

            return flag;
        }

        public static string Id { get { return WECHAT_PRINTER_ID.ToString(); } }
        public static string Token { get { return WECHAT_PRINTER_TOKEN; } }

        private static StringCollection adImgFilepaths = null;
        private static StringCollection adVidFilepaths = null;
        private static string qrCodeFilepath = String.Empty;
        private static string printQrCodeFilepath = String.Empty;
        private static string logoFilepath = String.Empty;
        private static int captcha = -1;
        private static string coName = String.Empty;

        public static StringCollection AdImgFilepaths { get { return adImgFilepaths; } private set { adImgFilepaths = value; } }
        public static StringCollection AdVidFilepaths { get { return adVidFilepaths; } private set { adVidFilepaths = value; } }
        public static string QRCodeFilepath { get { return qrCodeFilepath; } private set { qrCodeFilepath = value; } }
        public static string PrintQRCodeFilepath { get { return printQrCodeFilepath; } private set { printQrCodeFilepath = value; } }
        public static string LogoFilepath { get { return logoFilepath; } set { logoFilepath = value; } }
        public static int Captcha { get { return captcha; } set { captcha = value; } }
        public static string CoName { get { return coName; } private set { coName = value; } }


        public static string InitUrl { get { return INIT_URL; } }
        public static string PrintImgUrl { get { return PRINT_IMG_URL; } }
        public static string PrintImgCallBackUrl { get { return PRINT_IMG_CALLBACK_URL; } }

        public static string PrinterName { get { return PRINTER_NAME; } }
        public static double PrinterDpi { get { return PRINT_DPI; } }
        public static double PrinterWidth { get { return PRINT_WIDTH; } }
        public static double PrinterHeight { get { return PRINT_HEIGHT; } }
        public static double PrinterHeightPos { get { return PRINT_HEIGHT_POS; } }
        public static double PrinterWidthPos { get { return PRINT_WIDTH_POS; } }
        public static double PrinterLogoHeight { get { return PRINT_LOGO_HEIGHT; } }
        //public static double PrinterLogoWidth { get { return PRINT_LOGO_WIDTH; } }
        public static double PrinterLogoHeightPos { get { return PRINT_LOGO_HEIGHT_POS; } }
        public static double PrinterLogoWidthPos { get { return PRINT_LOGO_WIDTH_POS; } }
        public static double PrinterQrHeight { get { return PRINT_QR_HEIGHT; } }
        public static double PrinterQrHeightPos { get { return PRINT_QR_HEIGHT_POS; } }
        public static double PrinterQrWidthPos { get { return PRINT_QR_WIDTH_POS; } }

        public static double ScreenDpi { get { return SCREEN_DPI; } }
        public static int PrintImgInterval { get { return PRINT_IMG_TIMER_INTERVAL; } }
        public static int PrintWaitTime { get { return PRINT_WAIT_TIME; } }
        public static int LoadedWaitTime { get { return LOADED_WAIT_TIME; } }

        public static bool IsPrinting { get { return isPrinting; } set { isPrinting = value; } }

        public static int HttpRetryTimes { get { return HTTP_RETRY_TIMES; } }
        public static int HttpTimeout { get { return HTTP_TIMTOUT; } }

        public class ParamKeys
        {
            public static string Status { get { return "status"; } }
            public static string Id { get { return "id"; } }
            public static string Token { get { return "token"; } }
            public static string Pid { get { return "pid"; } }
        }

        public enum PrintImgStatus { Fail, Success }
    }
}