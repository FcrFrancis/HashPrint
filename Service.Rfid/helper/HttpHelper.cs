using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Service.Rfid
{
    /// <summary>
    /// Http请求响应帮助类
    /// </summary>
    public class HttpHelper
    {

        /// <summary>
        /// HTTP简单的GET请求
        /// 编码采用默认
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="timeout">超时时间，以秒为单位</param>
        /// <returns>响应文本</returns>
        public static string HttpGet(string url, int timeout)
        {
            return HttpGet(url, timeout, Encoding.Default);
        }

        /// <summary>
        /// HTTP简单的GET请求      
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="timeout">超时时间，以秒为单位</param>
        /// <returns>响应文本</returns>
        public static string HttpGetUTF8(string url, int timeout)
        {
            return HttpGet(url, timeout, Encoding.UTF8);
        }

        /// <summary>
        /// HTTP简单的GET请求
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="timeout">超时时间，以秒为单位</param>
        /// <param name="encoding">编码</param>
        /// <returns>响应文本</returns>
        public static string HttpGet(string url, int timeout, Encoding encoding)
        {
            StringBuilder returnResult = new StringBuilder();

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = timeout * 1000;
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            using (Stream responseStream = httpWebResponse.GetResponseStream())
            using (StreamReader sr = new StreamReader(responseStream, encoding))
            {
                //return sr.ReadToEnd();
                while (-1 != sr.Peek())
                {
                    returnResult.Append(sr.ReadLine());
                }
            }
            return returnResult.ToString();
        }

        /// <summary>
        /// 此方法针对WebService，自动剥离string根节点，并执行解码
        /// </summary>
        /// <param name="url"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static string HttpGetForWS(string url, int timeout = 60)
        {
            string strResponse = HttpGet(url, timeout);
            Regex reg = new Regex(@"<string[^>]([\s\S]*?)</string>");
            Match match = reg.Match(strResponse);
            string result = match.Groups[1].Value.Trim();
            result = result.Substring(result.IndexOf('>') + 1);
            result = result.Replace("&lt;", "<").Replace("&gt;", ">");
            return result;
        }


        /// <summary>
        /// POST方式
        /// </summary>
        /// <param name="strURL">URI资源</param>
        /// <param name="strParm">POST参数</param>
        /// <param name="timeOut">超时值：以秒为单位</param>
        /// <returns></returns>
        public static string HttpPostUTF8(string strURL, string strParm, int timeOut, Dictionary<string, string> headers = null)
        {
            return HttpPost(strURL, strParm, timeOut, Encoding.UTF8, "application/x-www-form-urlencoded", headers);
        }

        /// <summary>
        /// POST方式
        /// </summary>
        /// <param name="strURL">URI资源</param>
        /// <param name="strParm">POST参数</param>
        /// <param name="timeOut">超时值：以秒为单位</param>
        /// <returns></returns>
        public static string HttpPostUTF8WithJson(string strURL, string strParm, int timeOut, Dictionary<string, string> headers = null)
        {
            return HttpPost(strURL, strParm, timeOut, Encoding.UTF8, "application/json", headers);
        }

        /// <summary>
        /// POST方式
        /// </summary>
        /// <param name="strURL">URI资源</param>
        /// <param name="strParm">POST参数</param>
        /// <param name="timeOut">超时值：以秒为单位</param>
        /// <param name="timeOut">编码</param>内容类型</param>
        /// <returns></returns>
        public static string HttpPost(string strURL, string strParm, int timeOut, Encoding encoding, string contentType, Dictionary<string, string> headers)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            //增加请求头 
            if (headers != null && headers.Any())
            {
                foreach (var kv in headers)
                {
                    request.Headers.Add(kv.Key, kv.Value);
                }
            }

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            byte[] postBytes = encoding.GetBytes(strParm);
            request.Timeout = timeOut * 1000;
            request.Method = "POST";
            if (encoding.EncodingName == Encoding.UTF8.EncodingName)
            {
                contentType += ";charset=UTF-8";
            }
            request.ContentType = contentType;
            request.ContentLength = postBytes.Length;

            using (Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(postBytes, 0, postBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
