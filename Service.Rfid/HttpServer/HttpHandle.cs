using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using ReaderUart;

namespace Service.Rfid
{
    public class HttpHandle
    {
        private static readonly ILog s_log = LogManager.GetLogger(typeof(HttpHandle));
        private HttpListener m_listerner;
        //Rfid通讯类
        private UartHelper uart;

        private const string WEB_PRINT_EPC = "/api/rfid/read/";
        private const string WEB_INFO = "/info/";
        private string m_webUrl = AppConfiguration.RequestUrl;
        private bool rfidConnect;

        private static SemaphoreSlim semaphore;
        private int _currentEpcCount;
        private List<string> _epcList;

        public HttpHandle()
        {
            uart = new UartHelper(Print, Execute);
            _epcList = new List<string>();
            //初始化信号量为0
            semaphore = new SemaphoreSlim(0);
        }

        public void Start()
        {
            s_log.Info("WebServer Start ...");
            if (!HttpListener.IsSupported)
            {
                s_log.Warn("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            m_listerner = new HttpListener();

            //指定身份验证 Anonymous匿名访问
            //m_listerner.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            string printUrl = m_webUrl.TrimEnd('/') + WEB_PRINT_EPC;
            string webInfo = m_webUrl.TrimEnd('/') + WEB_INFO;

            s_log.Info($"添加URL：{printUrl}");
            s_log.Info($"添加URL：{webInfo}");
            m_listerner.Prefixes.Add(printUrl);
            m_listerner.Prefixes.Add(webInfo);
            //1. http开启
            m_listerner.Start();
            s_log.Info("WebServer Start Successed");
            //2. 打印机初始化
            Init();
        }

        public void Listen(object state)
        {
            //连接RFID读写器
            ConnectReader();
            while (m_listerner?.IsListening == true)
            {
                s_log.Info("Listening...");
                try
                {
                    //注意: 等待请求连接,没有请求则GetContext处于阻塞状态
                    var context = m_listerner.GetContext();

                    // 处理请求的线程
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (Exception ex)
                {
                    s_log.Error($"Error while listening: {ex}");
                }
            }
            s_log.Info("Listen Exit.");
        }

        public void Close()
        {
            s_log.Info("WebServer Close ...");
            DisConnectReader();
            // 停止监听器
            m_listerner?.Stop();
            //关闭
            m_listerner?.Close();
            s_log.Info("WebServer Close Successed");
        }

        private void Init()
        {
            uart.Init();
            s_log.Info("Rfid初始化");
        }

        private void ConnectReader()
        {
            try
            {
                s_log.Info("Rfid 连接...");
                string strComPort = AppConfiguration.RfidCom;
                int nBaudrate = AppConfiguration.Baudrate;

                var powers = AppConfiguration.RfidAntPower.Select(x => x.Power).ToList();
                //int nRet = reader.OpenCom(strComPort, nBaudrate, out strException);
                string msg = uart.Connect(strComPort, nBaudrate, powers);
                if (!string.IsNullOrEmpty(msg))
                {
                    s_log.Error($"Rfid connect fail,error: {msg}");
                    return;
                }
                else
                {
                    rfidConnect = true;
                    s_log.Info("Rfid 连接成功");
                }
            }
            catch (Exception ex)
            {
                s_log.Info($"Rfid 连接异常: {ex.Message}");
            }
        }


        private void DisConnectReader()
        {
            try
            {
                if (rfidConnect)
                {
                    s_log.Info("Rfid 断开连接...");
                    uart.Disconnect();
                    s_log.Info("Rfid 断开连接完成");
                }
            }
            catch (Exception ex)
            {
                s_log.Info($"Rfid 断开连接异常: {ex.Message}");
            }
        }

        private void Print(string strText, int nType)
        {
            s_log.Info($"Info: {strText},nType: {nType}");
        }

        /// <summary>
        /// 异步回调核心方法
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="info"></param>
        private void Execute(byte cmd, string info)
        {
            switch (cmd)
            {
                case 0x72:
                    GetFirmwareVersion(info);
                    break;
                case 0x77:
                    GetPower(info);
                    break;
                case 0x7A:
                    break;
                case 0x7B:
                    GetTemperature(info);
                    break;
                case 0x79:
                    GetFrequencyRegion(info);
                    break;
                case 0x89:
                case 0x8A:
                case 0x8B:
                    GetData(info);
                    break;
                default: break;
            }
        }

        private void GetFirmwareVersion(string info)
        {
            s_log.Info($"FirmwareVersion: {info}");
        }

        private void GetTemperature(string info)
        {
            s_log.Info($"ReaderTemperature: {info}");
        }

        private void GetPower(string info)
        {
            s_log.Info($"Power: {info}");
        }

        private void GetFrequencyRegion(string info)
        {
            var s = info.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            s_log.Info($"StartFreq: {s[0]},FreqInterval: {s[1]},FreqQuantity: {s[2]}");
        }

        /// <summary>
        /// 数据解析
        /// string info = string.Format("{0}-{1}-{2}-{3}-{4}", strPC, strEPC, strAntId, strFreq, strRSSI);
        /// string.Format("{0}-{1}", nReadRate, nDataCount)
        /// </summary>
        /// <param name="info"></param>
        private void GetData(string info)
        {
            string[] data = null;
            if (!string.IsNullOrEmpty(info))
            {
                data = info.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (data != null && data.Length >= 5)
            {
                var epc = data[1];
                if (!_epcList.Contains(epc))
                {
                    _epcList.Add(epc);
                }
            }
            else if (data != null && data.Length == 2)
            {
                //记录本次询检的epc数量
                _currentEpcCount = Convert.ToInt32(data[1]);
            }

            if (_epcList.Count == _currentEpcCount)
            {
                //释放信号量，允许其他线程再次获取信号量
                semaphore.Release();
            }
        }



        private void ProcessRequest(object state)
        {
            try
            {
                s_log.Info("Current request handle ...");

                var context = (HttpListenerContext)state;
                var request = context.Request;
                var response = context.Response;

                // 检查 Origin 头是否存在
                var origin = request.Headers["Origin"];
                //if (!string.IsNullOrEmpty(origin))
                //{
                // 设置同源策略
                //response.Headers.Add("Access-Control-Allow-Origin", origin);
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.Headers.Add("Access-Control-Allow-Credentials", true.ToString());
                //response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Test");
                //}

                // 获取请求的方法（GET 或 POST）
                string method = request.HttpMethod.ToUpper();
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.OutputStream.Close();
                    return;
                }

                // 设置响应内容和类型
                string contentType;
                if (request.ContentType == "application/json")
                    contentType = "application/json";
                else
                    contentType = "text/plain";//文本（text）响应类型

                string responseStr;
                try
                {
                    bool success = Handle(context);
                    if (success)
                    {
                        responseStr = Success(context, "");
                    }
                    else
                    {
                        responseStr = Fail(context, "fail");
                    }
                }
                catch (BusinessException ex)
                {
                    responseStr = Fail(context, ex.Message);
                }


                // 构造响应内容
                var buffer = Encoding.UTF8.GetBytes(responseStr);
                // 设置响应头和内容长度           
                response.ContentLength64 = buffer.Length;
                response.ContentType = contentType;

                s_log.Info("Current request handle finish.");
                // 将响应内容写入输出流
                response.OutputStream.Write(buffer, 0, buffer.Length);
                // 关闭输出流
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                s_log.Info($"Current request handle exception, Ex[{ex}]");
            }
        }

        private bool Handle(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.RawUrl.Equals("/info"))
            {
                return true;
            }
            //重置epc状态
            _currentEpcCount = 0;
            _epcList.Clear();
            //询检
            var ants = AppConfiguration.RfidAntPower.Select(x =>
            {
                if (x.Power == 0) return 0;  //功率等于0的天线不轮询
                else return x.Ant;
            }).ToList();

            uart.StartInventory(1);

            //uart.StartInventoryFast(ants);
            //uart.StopInventory();
            //阻塞当前线程，等待异步数据返回
            semaphore.Wait(TimeSpan.FromMilliseconds(750));
            //semaphore.Wait();
            return true;
        }

        private string Success(HttpListenerContext ctx, object state)
        {
            Response<List<string>> response = new Response<List<string>>();
            response.Code = 0;
            response.Message = "";
            response.Data = new List<string>();

            string responseStr;
            StringBuilder sb = new StringBuilder();
            if (ctx.Request.RawUrl.Equals("/info"))
            {
                //sb.Append("<html><head><title>The WebServer V1.0</title></head><body>");
                sb.Append("The WebServer runs normally!");
                //sb.Append("</body></html>");
                responseStr = sb.ToString();
            }
            else
            {
                response.Data = _epcList;
                responseStr = JsonConvert.SerializeObject(response);
            }
            return responseStr;
        }

        private string Fail(HttpListenerContext ctx, string msg)
        {
            Response response = new Response();
            response.Code = 100;
            response.Message = msg;

            var responseStr = JsonConvert.SerializeObject(response);
            return responseStr;
        }

        /*   
        private void Success(HttpListenerContext ctx)
            {
                try
                {
                    //设置返回给客服端http状态代码
                    ctx.Response.StatusCode = 200;
                    //使用Writer输出http响应代码
                    using (StreamWriter writer = new StreamWriter(ctx.Response.OutputStream, Encoding.UTF8))
                    {
                        //writer.WriteLine("<html><head><title>The WebServer RfidTest</title></head><body>");
                        if (ctx.Request.RawUrl.Equals("/info"))
                        {
                            string responseStr = JsonConvert.SerializeObject(new { code = 0, msg = "Hello Web!", errMsg = "" });
                            writer.WriteLine(responseStr);
                        }
                        else
                        {
                            string responseStr = JsonConvert.SerializeObject(new { code = 0, msg = "" });
                            writer.WriteLine(responseStr);
                        }
                        //writer.WriteLine("</body></html>");
                        writer.Close();
                        ctx.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    s_log.Error(ex);
                }
            }

            private void Fail(HttpListenerContext ctx, string msg)
            {
                try
                {
                    HttpListenerResponse response = ctx.Response;
                    ctx.Response.StatusCode = 200;
                    // Construct a response.
                    //string responseString = "<HTML><BODY>0</BODY></HTML>";

                    string responseStr = JsonConvert.SerializeObject(new Response { code = 500, msg = msg });
                    byte[] buffer = Encoding.UTF8.GetBytes(responseStr);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    // You must close the output stream.
                    response.OutputStream.Close();
                }
                catch (Exception ex)
                {
                    s_log.Error(ex);
                }
            }*/


        private void CheckSign(HttpListenerRequest request)
        {
            string message = "";

            string model = request.QueryString["model"];
            string epc = request.QueryString["epc"];
            string matCode = request.QueryString["matCode"];
            string size = request.QueryString["size"];
            string price = request.QueryString["price"];
            string discountPrice = request.QueryString["discountPrice"];
            string reqTime = request.QueryString["reqTime"];
            string sign = request.QueryString["sign"];


            //记录请求参数
            s_log.Info($"request-param: model={model},epc={epc},matCode={matCode},size={size}," +
                $"price={price},discountPrice={discountPrice},reqTime={reqTime},sign={sign}");

            if (string.IsNullOrWhiteSpace(model))
            {
                message += " model 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(epc))
            {
                message += " epc 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(matCode))
            {
                message += " matCode 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(size))
            {
                message += " size 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(price))
            {
                message += " price 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(discountPrice))
            {
                message += " discountPrice 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(reqTime))
            {
                message += " reqTime 不能为空 ";
            }
            if (string.IsNullOrWhiteSpace(sign))
            {
                message += " sign 不能为空 ";
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                s_log.Error($"{message}");
                throw new BusinessException(message);
            }

            long reqTimeInt = Convert.ToInt64(reqTime);
            try
            {
                long current = GetTimestamp();
                long spanTime = (long)(new TimeSpan(current - reqTimeInt).TotalSeconds);
                //服务器时间差，可能为负数
                if (Math.Abs(spanTime) > AppConfiguration.RequestTimeout)
                {
                    message = $"请求超时...  请求 spanTime={spanTime}, 请求配置 REQ_TIMEOUT={AppConfiguration.RequestTimeout}";
                    s_log.Error(message);
                    throw new BusinessException(message);
                }
            }
            catch (Exception ex)
            {
                message = $"时间戳转换异常...  请求 reqTime={reqTime},reqTimeInt={reqTimeInt}, 异常：{ex}";
                s_log.Error(message);
                throw new BusinessException(message);
            }


            SortedSet<string> sortSet = new SortedSet<string>();
            sortSet.Add($"model={model}");
            sortSet.Add($"epc={epc}");
            sortSet.Add($"matCode={matCode}");
            sortSet.Add($"size={size}");
            sortSet.Add($"price={price}");
            sortSet.Add($"discountPrice={discountPrice}");
            sortSet.Add($"reqTime={reqTime}");

            string[] array = new string[sortSet.Count + 1];
            sortSet.CopyTo(array);
            array[array.Length - 1] = AppConfiguration.SecretKey;
            string content = string.Join("^", array);
            s_log.Info($"待签名字符串: {content}");

            string mksign = MD5Helper.MakeMD5(content);
            s_log.Info($"原sign={sign} , 生成mksign={mksign}");

            var ret = sign.Equals(mksign);
            if (!ret)
            {
                throw new BusinessException("签名失败!");
            }
        }

        /// <summary>
        /// 获取时间戳,精确到ms
        /// </summary>
        /// <returns></returns>
        private long GetTimestamp()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timestamp = (long)timeSpan.TotalMilliseconds;
            return timestamp;
        }


    }
}
