using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Configuration;
using Newtonsoft.Json;

namespace UHFDemo
{
    public class Echo : WebSocketBehavior
    {
        static string printerName = ConfigurationManager.AppSettings["PrinterName"].ToString().Trim();
        static string moduleType = ConfigurationManager.AppSettings["ModuleType"].ToString().Trim();
        private static List<WebSocket> _clients = new List<WebSocket>();
        public static bool IsSend { get; set; } = true; //是否发送给客户端的标识
        public static bool IsRefresh { get; set; } = false;
        protected override void OnMessage(MessageEventArgs e)
        {

            string msg = e.Data;
            try
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    if (!msg.Contains("arguments"))
                    {
                        if (msg == "{\"protocol\":\"json\",\"version\":1}\u001e")
                        {
                            Send(msg);
                        }
                        return;
                    }
                    msg = msg.Replace("\u001e", "");
                    if (msg.Contains("{\"arguments\":[1],\"target\":\"echo\",\"type\":1}"))
                    {
                        IsSend = true;
                    }
                    if (msg.Contains("{\"arguments\":[9],\"target\":\"echo\",\"type\":1}"))
                    {
                        IsSend = false;
                    }
                    if (msg.Contains("{\"arguments\":[5],\"target\":\"echo\",\"type\":1}"))
                    {
                        IsRefresh = true;
                    }
                    if (1 == 1)
                    {
                        var list = JsonConvert.DeserializeObject<SocketPrintInfo>(msg);
                        foreach (var argumentList in list.Arguments)
                        {
                            foreach (var item in argumentList)
                            {
                                //品名
                                var sku = item.sku;
                                //验证码
                                var snCode = item.snCode;
                                //二维码链接
                                var link = item.link;
                                //RFID
                                var rfid = item.rfid;

                                var printText = string.Empty;
                                if (moduleType == "1")
                                {
                                    //模板1，需要打印机设置逆转标签
                                    printText = $@"^XA
                                                        ^PW800
                                                        ^LL420
                                                        ^FT25,170
                                                        ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                        ^FD货  号：^FS^CI27
                                                        ^FT26,170
                                                        ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                        ^FD货  号：^FS^CI27
                                                        ^FO135,115
                                                        ^BY2
                                                        ^BCN,90,Y,N,N
                                                        ^FD{sku}^FS
                                                        ^FT25,280
                                                        ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                        ^FD验证码：{snCode}^FS^CI27
                                                        ^FT26,280
                                                        ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                        ^FD验证码：{snCode}^FS^CI27
                                                        ^FO600,228 
                                                        ^BY3
                                                        ^BQN,2,3,,6
                                                        ^FDQA,{link}^FS^CI27
                                                        ^RS8
                                                        ^RFW^FD{rfid}^FS
                                                        ^PQ1,0,1,Y
                                                        ^XZ";
                                }
                                else if (moduleType == "2")
                                {
                                    //模板2，不需要逆转标签
                                    printText = $@"^XA
                                                    ^PW800
                                                    ^LL420
                                                    ^FT50,200
                                                    ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                    ^FD货  号：^FS^CI27
                                                    ^FT51,200
                                                    ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                    ^FD货  号：^FS^CI27
                                                    ^FO160,165
                                                    ^BY2
                                                    ^BCN,50,Y,N,N
                                                    ^A0N,50,40
                                                    ^FD{sku}^FS
                                                    ^FT50,300
                                                    ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                    ^FD验证码：{snCode}^FS^CI27
                                                    ^FT51,300
                                                    ^A@N,25,25,SIMSUN.TTF^FH\^CI28
                                                    ^FD验证码：{snCode}^FS^CI27
                                                    ^FO630,248 
                                                    ^BY2
                                                    ^BQN,2,3,,6
                                                    ^FDQA,{link}^FS^CI27
                                                    ^RS8
                                                    ^RFW^FD{rfid}^FS
                                                    ^PQ1,0,1,Y
                                                    ^XZ";
                                }
                                SendZebraPrint(printText);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 调用打印机打印
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SendZebraPrint(string sb)
        {
            ZebraPrintHelper.PrinterProgrammingLanguage = ProgrammingLanguage.ZPL;
            ZebraPrintHelper.PrinterName = printerName;
            ZebraPrintHelper.PrinterType = DeviceType.DRV;
            ZebraPrintHelper.PrintCommand(sb.ToString());
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            lock (_clients)
            {
                _clients.Add(this.Context.WebSocket); // 注意：这里应该使用 this.Context.WebSocket 而不是仅仅 this，因为 this 是 WebSocketBehavior 的实例
                Console.WriteLine($"Client connected: {this.ID}"); // 通常，ID 是由服务器自动分配的，但你也可以在 OnOpen 中设置自定义的 Session ID
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            lock (_clients)
            {
                _clients.Remove(this.Context.WebSocket);
                Console.WriteLine($"Client disconnected: {this.ID}");
            }
        }

        // 提供一个公共的静态方法来获取客户端列表（线程安全）
        public static List<WebSocket> GetClients()
        {
            lock (_clients)
            {
                // 返回一个浅拷贝以避免外部代码直接修改列表
                return new List<WebSocket>(_clients);
            }
        }
    }
}
