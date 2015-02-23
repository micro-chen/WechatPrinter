﻿using System;
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

        public static WebResponse DoGet(string url, Dictionary<string, string> param)
        {
            if (url == null || url.Equals(""))
            {
                throw new ArgumentException();
            }
            try
            {
                StringBuilder sb = new StringBuilder(url);
                sb.Append("?");
                sb.Append(WechatPrinterConf.ParamKeys.Id).Append("=").Append(WechatPrinterConf.Id).Append("&");
                sb.Append(WechatPrinterConf.ParamKeys.Token).Append("=").Append(WechatPrinterConf.Token);
                sb.Append(EncodeParamFromMap(param));

                Console.WriteLine("DoGet Url: " + sb.ToString());
                
                WebRequest request = WebRequest.Create(sb.ToString());
                request.Timeout = 10 * 1000;
                request.Method = "GET";
                return request.GetResponse();
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: DoGet Error]");
                throw;
            }

        }

        public static string GetText(string url, Dictionary<string, string> param)
        {
            try
            {
                using (WebResponse response = DoGet(url, param))
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
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetText Error]");
                throw;
            }
        }

        public static T GetJson<T>(string url, Dictionary<string, string> param)
        {
            T bean = default(T);
            try
            {
                string text = GetText(url, param);
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

        public static string GetFile(FileUtils.ResPathsEnum path, string url, Dictionary<string, string> param)
        {
            string filename = url.Substring(url.LastIndexOf("/") + 1);
            try
            {
                Console.WriteLine("GetFile: " + path + filename);
                using (Stream stream = DoGet(url, param).GetResponseStream())
                {
                    return FileUtils.SaveFile(stream, path, filename);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetFile Error]");
                throw;
            }
        }

        public static StringCollection GetFiles(FileUtils.ResPathsEnum path, StringCollection urls, Dictionary<string, string> param)
        {
            StringCollection filepaths = new StringCollection();
            foreach (string url in urls)
            {
                try
                {
                    string filepath = HttpUtils.GetFile(path, url, param);
                    if (!filepath.Equals(String.Empty))
                        filepaths.Add(filepath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[HttpUtils: GetFiles:for Error: {0}]", url);
                    Console.WriteLine(ex.Message);
                }
            }
            return filepaths;
        }

        private static string EncodeParamFromMap(Dictionary<string, string> param)
        {
            if (param == null || param.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            foreach (var item in param)
            {
                sb.Append("&");
                sb.Append(System.Web.HttpUtility.UrlEncode(item.Key, Encoding.UTF8)).Append("=").Append(System.Web.HttpUtility.UrlEncode(item.Value, Encoding.UTF8));
            }

            return sb.ToString();
        }
    }
}