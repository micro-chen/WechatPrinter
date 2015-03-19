﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatPrinter.Support
{
    class WechatPrinterConf
    {
        private const ulong WECHAT_PRINTER_ID = 1L;
        private const string WECHAT_PRINTER_TOKEN = ""; //TODO token

        private const string INIT_URL = "http://114.215.80.157/Home/Printer/login";
        private const string PRINT_IMG_URL = "http://joewoo.pw/printer/printimg.php"; //TODO 打印接口
        private const string PRINT_IMG_CALLBACK_URL = ""; //TODO 打印返回接口

        //TODO 打印机名
        private const string PRINTER_NAME = "Brother DCP-1510 series";
        //private static string PRINTER_NAME = "Microsoft XPS Document Writer";

        private const double PRINT_WIDTH = 1205;
        private const double PRINT_HEIGHT = 1795;
        private const double PRINT_DPI = 300;
        private const double SCREEN_DPI = 96;

        private const int PRINT_IMG_TIMER_INTERVAL = 5 * 1000;

        private static StringCollection adImgUrls = null;
        private static StringCollection adVidUrls = null;
        private static string qrCodeUrl = null;
        private static int captcha = -1;

        private static bool isPrinting = false;

        private const int HTTP_RETRY_TIMES = 3;
        private const int HTTP_TIMTOUT = 10 * 1000;

        public static void Init(InfoBean bean)
        {
            AdImgUrls = bean.picUrl;
            AdVidUrls = bean.videoUrl;
            QRCodeUrl = bean.qrcodeUrl;
            Captcha = bean.verifyCode;
        }

        public static string Id { get { return WECHAT_PRINTER_ID.ToString(); } }
        public static string Token { get { return WECHAT_PRINTER_TOKEN; } }

        public static StringCollection AdImgUrls { get { return adImgUrls; } private set { adImgUrls = value; } }
        public static StringCollection AdVidUrls { get { return adVidUrls; } private set { adVidUrls = value; } }
        public static string QRCodeUrl { get { return qrCodeUrl; } private set { qrCodeUrl = value; } }
        public static int Captcha { get { return captcha; } set { captcha = value; } }

        public static string InitUrl { get { return INIT_URL; } }
        public static string PrintImgUrl { get { return PRINT_IMG_URL; } }
        public static string PrintImgCallBackUrl { get { return PRINT_IMG_CALLBACK_URL; } }

        public static string PrinterName { get { return PRINTER_NAME; } }
        public static double PrinterDpi { get { return PRINT_DPI; } }
        public static double PrinterWidth { get { return PRINT_WIDTH; } }
        public static double PrinterHeight { get { return PRINT_HEIGHT; } }
        public static double ScreenDpi { get { return SCREEN_DPI; } }
        public static int PrintImgInterval { get { return PRINT_IMG_TIMER_INTERVAL; } }

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