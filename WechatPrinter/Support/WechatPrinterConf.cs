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

        private const ulong WECHAT_PRINTER_ID = 1L;
        private const string WECHAT_PRINTER_TOKEN = ""; //TODO token

        private const string INIT_URL = "http://114.215.80.157/Home/Printer/login";
        private const string PRE_URL = "http://114.215.80.157";
        private const string PRINT_IMG_URL = "http://114.215.80.157/Home/Printer/getpic";
        private const string PRINT_IMG_CALLBACK_URL = "http://114.215.80.157/Home/Printer/stateReceive";

        private const string PRINTER_NAME = "EPSON L300 Series";
        //private const string PRINTER_NAME = "Microsoft XPS Document Writer";

        private const double PRINT_EDGE = 832 * 0.0377;
        private const double PRINT_RATIO = SCREEN_DPI / PRINT_DPI;
        private const double PRINT_WIDTH = 832 - PRINT_EDGE;
        //private const double PRINT_HEIGHT = 1107;
        //private const double PRINT_HEIGHT = PRINT_WIDTH * 1.33;
        private const double PRINT_HEIGHT = PRINT_WIDTH;
        private const double PRINT_HEIGHT_POS = PRINT_EDGE * PRINT_RATIO;
        private const double PRINT_WIDTH_POS = PRINT_EDGE * PRINT_RATIO;

        private const double PRINT_LOGO_HEIGHT = 55;
        private const double PRINT_LOGO_WIDTH = PRINT_LOGO_HEIGHT*8;

        private const double PRINT_LOGO_HEIGHT_POS = (PRINT_HEIGHT + PRINT_EDGE * 2) * PRINT_RATIO;
        private const double PRINT_LOGO_WIDTH_POS = PRINT_WIDTH_POS;
        //private const double PRINT_LOGO_WIDTH_POS = PRINT_WIDTH * (PRINT_RATIO) / 2 - (PRINT_LOGO_HEIGHT * (PRINT_RATIO) * 5.65) / 2 + PRINT_WIDTH_POS;
        //private const double PRINT_QR_HEIGHT = PRINT_LOGO_HEIGHT * 8;
        private const double PRINT_QR_HEIGHT = 216;
        private const double PRINT_QR_HEIGHT_POS = PRINT_LOGO_HEIGHT_POS;
        private const double PRINT_QR_WIDTH_POS = PRINT_WIDTH_POS + (PRINT_WIDTH - PRINT_QR_HEIGHT) * PRINT_RATIO;
        private const double PRINT_DPI = 300;
        private const double SCREEN_DPI = 96;

        private const int PRINT_IMG_TIMER_INTERVAL = 5 * 1000;

        private const int PRINT_WAIT_TIME = 30 * 1000;

        private static StringCollection adImgUrls = null;
        private static StringCollection adVidUrls = null;
        private static string qrCodeUrl = String.Empty;
        private static int captcha = -1;
        private static string coName = String.Empty;

        private static bool isPrinting = false;

        private const int HTTP_RETRY_TIMES = 1;
        private const int HTTP_TIMTOUT = 3 * 1000;

        public static void Init(InfoBean bean)
        {
            AdImgUrls = bean.picUrl;
            AdVidUrls = bean.videoUrl;
            QRCodeUrl = bean.qrcodeUrl;
            Captcha = bean.verifyCode;
            CoName = bean.name;

            for (int i = 0; i < AdImgUrls.Count; i++)
            {
                AdImgUrls[i] = PRE_URL + AdImgUrls[i];
            }
            for (int i = 0; i < AdVidUrls.Count; i++)
            {
                AdVidUrls[i] = PRE_URL + AdVidUrls[i];
            }
            QRCodeUrl = PRE_URL + QRCodeUrl;
        }

        public static string Id { get { return WECHAT_PRINTER_ID.ToString(); } }
        public static string Token { get { return WECHAT_PRINTER_TOKEN; } }

        public static StringCollection AdImgUrls { get { return adImgUrls; } private set { adImgUrls = value; } }
        public static StringCollection AdVidUrls { get { return adVidUrls; } private set { adVidUrls = value; } }
        public static string QRCodeUrl { get { return qrCodeUrl; } private set { qrCodeUrl = value; } }
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
        public static double PrinterLogoWidth { get { return PRINT_LOGO_WIDTH; } }
        public static double PrinterLogoHeightPos { get { return PRINT_LOGO_HEIGHT_POS; } }
        public static double PrinterLogoWidthPos { get { return PRINT_LOGO_WIDTH_POS; } }
        public static double PrinterQrHeight { get { return PRINT_QR_HEIGHT; } }
        public static double PrinterQrHeightPos { get { return PRINT_QR_HEIGHT_POS; } }
        public static double PrinterQrWidthPos { get { return PRINT_QR_WIDTH_POS; } }

        public static double ScreenDpi { get { return SCREEN_DPI; } }
        public static int PrintImgInterval { get { return PRINT_IMG_TIMER_INTERVAL; } }
        public static int PrintWaitTime { get { return PRINT_WAIT_TIME; } }

        public static bool IsPrinting { get { return isPrinting; } set { isPrinting = value; } }

        public static int HttpRetryTimes { get { return HTTP_RETRY_TIMES; } }
        public static int HttpTimeout { get { return HTTP_TIMTOUT; } }

        public class ParamKeys
        {
            public static string Status { get { return "status"; } }
            public static string Id { get { return "id"; } }
            public static string Token { get { return "token"; } }
        }

        public enum PrintImgStatus { Fail, Success }
    }
}