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

        public static WebResponse DoGet(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
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

        public static string GetText(string url)
        {
            try
            {
                using (WebResponse response = DoGet(url))
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

        public static T GetJson<T>(string url)
        {
            T bean = default(T);
            try
            {
                string text = GetText(url);
                Console.WriteLine("Get Json: "+ text);
                bean = new JavaScriptSerializer().Deserialize<T>(text);
            }
            catch (Exception)
            {
                Console.WriteLine("[HttpUtils: GetJson Error]");
                throw;
            }
            return bean;
        }

        public static string GetFile(FileUtils.ResPathsEnum path, string url)
        {
            string filename = url.Substring(url.LastIndexOf("/") + 1);
            try
            {
                using (Stream stream = DoGet(url).GetResponseStream())
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

        public static StringCollection GetFiles(FileUtils.ResPathsEnum path, StringCollection urls)
        {
            StringCollection filepaths = new StringCollection();
            foreach (string url in urls)
            {
                try
                {
                    //Console.WriteLine("File Url: " + url + "\t");
                    string filepath = HttpUtils.GetFile(FileUtils.ResPathsEnum.AdImg, url);
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

    }
}
