using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WechatPrinter.Support
{
    public class HttpUtils
    {

        public static WebResponse DoGet(string url, Dictionary<string, string> param, bool noRetry = false)
        {
            if (url == null || url.Equals(""))
            {
                throw new ArgumentException();
            }

            int retry = WechatPrinterConf.HttpRetryTimes;

            while (retry > 0)
            {
                WebResponse response = null;
                try
                {
                    StringBuilder sb = new StringBuilder(url);
                    //sb.Append("?");
                    //sb.Append(WechatPrinterConf.ParamKeys.Id).Append("=").Append(WechatPrinterConf.Id).Append("&");
                    //sb.Append(WechatPrinterConf.ParamKeys.Token).Append("=").Append(WechatPrinterConf.Token);

                    sb.Append(EncodeParamFromMap(param, false));

                    Console.WriteLine("DoGet Url: " + sb.ToString());

                    WebRequest request = WebRequest.Create(sb.ToString());
                    request.Timeout = WechatPrinterConf.HttpTimeout;
                    request.Method = "GET";
                    response = request.GetResponse();
                    return response;
                }
                catch (Exception e)
                {
                    if (!noRetry)
                    {
                        retry--;
                        Console.WriteLine("[HttpUtils: DoGet Error: Retry[{0}/{1}]", WechatPrinterConf.HttpRetryTimes - retry, WechatPrinterConf.HttpRetryTimes);
                        if (retry == 0)
                        {

                            if (WechatPrinterConf.DEBUG)
                            {
                                if (!File.Exists(FileUtils.LogPath))
                                {
                                    File.Create(FileUtils.LogPath).Close();
                                }
                                using (StreamWriter sw = File.AppendText(FileUtils.LogPath))
                                {
                                    sw.WriteLine(@"+GET++++++++++++++++++++++++++++++++++++++++");
                                    sw.WriteLine(url);
                                    sw.WriteLine(e.Source);
                                    sw.WriteLine(e.Message);
                                    sw.WriteLine(e.StackTrace);
                                    sw.WriteLine();
                                }
                            }

                            Console.WriteLine("[HttpUtils: DoGet Error: " + url + "\n\t" + e.Message + "\n\t" + e.StackTrace);

                            throw e;
                        }
                    }
                    else
                    {
                        throw e;
                    }

                }
            }
            throw new Exception();
        }

        public static WebResponse DoPost(string url, Dictionary<string, string> param, bool noRetry = false)
        {
            if (url == null || url.Equals(""))
            {
                throw new ArgumentException();
            }

            int retry = WechatPrinterConf.HttpRetryTimes;

            while (retry > 0)
            {
                WebResponse response = null;
                try
                {
                    StringBuilder sb = new StringBuilder(url);

                    Console.WriteLine("DoPost Url: " + sb.ToString());

                    WebRequest request = WebRequest.Create(sb.ToString());
                    request.Timeout = WechatPrinterConf.HttpTimeout;
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";

                    byte[] postParam = Encoding.UTF8.GetBytes(EncodeParamFromMap(param, true));

                    request.ContentLength = postParam.Length;
                    using (Stream writer = request.GetRequestStream())
                    {
                        writer.Write(postParam, 0, postParam.Length);
                    }

                    response = request.GetResponse();
                    return response;
                }
                catch (Exception e)
                {
                    if (!noRetry)
                    {
                        retry--;
                        Console.WriteLine("[HttpUtils: DoPost Error: Retry[{0}/{1}]", WechatPrinterConf.HttpRetryTimes - retry, WechatPrinterConf.HttpRetryTimes);
                        if (retry == 0)
                        {
                            Console.WriteLine("[HttpUtils: DoPost Error: Exit]");

                            if (WechatPrinterConf.DEBUG)
                            {
                                if (!File.Exists(FileUtils.LogPath))
                                {
                                    File.Create(FileUtils.LogPath).Close();
                                }
                                using (StreamWriter sw = File.AppendText(FileUtils.LogPath))
                                {
                                    sw.WriteLine(@"++++++++++++++++++++POST++++++++++++++++++++");
                                    sw.WriteLine(url);
                                    sw.WriteLine(e.Source);
                                    sw.WriteLine(e.Message);
                                    sw.WriteLine(e.StackTrace);
                                    sw.WriteLine();
                                }
                            }

                            throw e;
                        }
                    }
                    else
                    {
                        throw e;
                    }

                }
            }
            throw new Exception();
        }


        public static string GetText(string url, Dictionary<string, string> param, bool isPost = false, bool noRetry = false)
        {
            try
            {
                if (!isPost)
                {
                    using (WebResponse response = DoGet(url, param, noRetry))
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string text = reader.ReadToEnd();
                                return text;
                            }
                        }
                    }
                }
                else
                {
                    using (WebResponse response = DoPost(url, param, noRetry))
                    {


                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string text = reader.ReadToEnd();
                                return text;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetText Error]");
                throw;
            }
        }

        public static T GetJson<T>(string url, Dictionary<string, string> param, bool isPost = false)
        {
            return GetJson<T>(url, param, isPost, false);
        }

        public static T GetJson<T>(string jsonString)
        {
            Console.WriteLine("Get Json: " + jsonString);
            return new JavaScriptSerializer().Deserialize<T>(jsonString);
        }

        public static T GetJson<T>(string url, Dictionary<string, string> param, bool isPost = false, bool noRetry = false)
        {
            T bean = default(T);
            try
            {
                string text = GetText(url, param, isPost, noRetry);
                Console.WriteLine("Get Json: " + text);
                if (text != null && !text.Equals(String.Empty))
                {
                    bean = new JavaScriptSerializer().Deserialize<T>(text);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetJson Error]");
                throw;
            }
            return bean;
        }

        public static string GetFile(FileUtils.ResPathsEnum path, string url, Dictionary<string, string> param, bool isPost = false, IDownloadProgress progress = null)
        {
            string filename;
            if (path == FileUtils.ResPathsEnum.PrintImg)
            {
                filename = url.Substring(0, url.LastIndexOf("/"));
                filename = filename.Substring(filename.LastIndexOf("/") + 1);
            }
            else
            {
                filename = url.Substring(url.LastIndexOf("/") + 1);
            }
            try
            {
                Console.WriteLine("GetFile: [" + path + "] " + filename);
                if (!isPost)
                {
                    WebResponse response = DoGet(url, param);
                    //if(progress!=null)
                    //    progress.Progress(response.ContentLength,-2);
                    using (Stream stream = response.GetResponseStream())
                    {
                        return FileUtils.SaveFile(stream, path, filename, progress);
                    }
                }
                else
                {
                    WebResponse response = DoPost(url, param);
                    if (progress != null)
                        progress.Progress(response.ContentLength, -2);
                    using (Stream stream = response.GetResponseStream())
                    {
                        return FileUtils.SaveFile(stream, path, filename, progress);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetFile Error]");
                throw;
            }
        }

        public static StringCollection GetFiles(FileUtils.ResPathsEnum path, StringCollection urls, Dictionary<string, string> param, bool isPost = false, IDownloadProgress progress = null)
        {
            StringCollection filepaths = new StringCollection();
            foreach (string url in urls)
            {
                try
                {
                    string filepath = HttpUtils.GetFile(path, url, param, isPost, progress);
                    if (!filepath.Equals(String.Empty))
                        filepaths.Add(filepath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[HttpUtils: GetFiles:for Error: {0}\n\t{1}", url,ex.Message);
                    throw ex;
                }
            }
            return filepaths;
        }

        private static string EncodeParamFromMap(Dictionary<string, string> param, bool isPost = false)
        {
            //if (param == null)
            //{
            //    param = new Dictionary<string, string>();
            //}
            //if (!param.ContainsKey(WechatPrinterConf.ParamKeys.Id))
            //    param.Add(WechatPrinterConf.ParamKeys.Id, WechatPrinterConf.Id);
            //if (!param.ContainsKey(WechatPrinterConf.ParamKeys.Token))
            //    param.Add(WechatPrinterConf.ParamKeys.Token, WechatPrinterConf.Token);

            //if (param == null || param.Count == 0)
            //    return "";

            StringBuilder sb = new StringBuilder("");

            if (param != null)
            {
                bool isFrist = true;

                if (!isPost)
                {
                    foreach (var item in param)
                    {
                        if (isFrist)
                        {
                            sb.Append("?");
                            isFrist = false;
                        }
                        else
                            sb.Append("&");
                        sb.Append(System.Web.HttpUtility.UrlEncode(item.Key, Encoding.UTF8)).Append("=").Append(System.Web.HttpUtility.UrlEncode(item.Value, Encoding.UTF8));
                    }
                }
                else
                {
                    foreach (var item in param)
                    {
                        if (isFrist)
                        {
                            isFrist = false;

                        }
                        else
                            sb.Append("&");
                        sb.Append(item.Key).Append("=").Append(item.Value);
                    }
                }

                Console.WriteLine("[HttpUtils:Encode]: " + sb.ToString());
            }
            return sb.ToString();
        }
    }

    public interface IDownloadProgress
    {
        void Progress(long total, long read);
    }
}
