using log4net;
using Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReaderUart
{
    /// <summary>
    /// 读写器辅助类
    /// </summary>    
    public class UartHelper
    {
        private static readonly ILog s_log = LogManager.GetLogger(typeof(UartHelper));
        private ReaderSetting m_curSetting = new ReaderSetting();
        private ReaderMethod reader = new ReaderMethod();

        private Action<string, int> print;
        //异步回调函数
        private Action<byte, string> callBack;
        //循环盘存标识
        private volatile bool bLoopInventory;
        private int nRepeat;
        //连接时重置 功率、天线、频点
        private bool isFirstSet;
        private List<int> fastAnts;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="log">输出日志信息</param>
        /// <param name="action">异步回调</param>
        public UartHelper(Action<string, int> log, Action<byte, string> action)
        {
            print = log;
            callBack = action;
        }

        public void Init()
        {
            reader = new Reader.ReaderMethod();

            //The callback function
            reader.AnalyCallback = AnalyData;
            //放开，可以调试串口数据
            //reader.SendCallback = SendData;
            //reader.ReceiveCallback = RecvData;
            reader.ErrCallback = OnError;
        }


        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="comPort">串口</param>
        /// <param name="baudrate">波特率</param>
        /// <returns>成功返回信息空，否则异常信息</returns>
        public string Connect(string comPort, int baudrate, List<int> power)
        {
            s_log.Info("1.串口连接");
            string exmsg;
            int nbaudrate = Convert.ToInt32(baudrate);
            int nRet = reader.OpenCom(comPort, nbaudrate, out exmsg);
            if (nRet != 0)
            {
                s_log.Info($"2.连接异常：{exmsg}");
                return exmsg;
            }

            s_log.Info($"2.设置输出功率：{power}");
            isFirstSet = true;
            //TODO 连接时不设置功率
            //SetOutputPower(power);
            SetWorkAntenna();
            return string.Empty;
        }

        /// <summary>
        /// 断开设备
        /// </summary>
        public void Disconnect()
        {
            bLoopInventory = false;
            reader.CloseCom();
        }


        /// <summary>
        /// 设置1号工作天线
        /// </summary>
        /// <param name="power"></param>
        public void SetWorkAntenna()
        {
            m_curSetting.btWorkAntenna = 0x00;
            reader.SetWorkAntenna(m_curSetting.btReadId, m_curSetting.btWorkAntenna);
        }

        /// <summary>
        /// 设置功率
        /// </summary>
        /// <param name="power"></param>
        public void SetOutputPower(int power)
        {
            byte[] OutputPower = new byte[1];
            OutputPower[0] = (byte)power;
            reader.SetOutputPower(m_curSetting.btReadId, OutputPower);
        }

        /// <summary>
        /// 设置功率
        /// </summary>
        /// <param name="power"></param>
        public void SetOutputPower(List<int> powers)
        {
            byte[] OutputPower = new byte[powers.Count];
            for (int i = 0; i < powers.Count; i++)
            {
                OutputPower[i] = (byte)powers[i];
            }
            reader.SetOutputPower(m_curSetting.btReadId, OutputPower);
        }

        /// <summary>
        /// 获取功率
        /// </summary>
        /// <param name="power"></param>
        public void GetOutputPower()
        {
            reader.GetOutputPowerSingle(m_curSetting.btReadId);
        }


        /// <summary>
        /// 获取固件版本
        /// </summary>
        public void GetFirmwareVersion()
        {
            reader.GetFirmwareVersion(m_curSetting.btReadId);
        }

        /// <summary>
        /// 获取温度
        /// </summary>
        public void GetTemperature()
        {
            reader.GetReaderTemperature(m_curSetting.btReadId);
        }

        /// <summary>
        /// 设置频谱
        /// </summary>
        public void SetUserDefineFrequency(int nStartFrequency, int nFrequencyInterval, int channelQuantity)
        {
            byte btFrequencyInterval = (byte)(nFrequencyInterval / 10);
            byte btChannelQuantity = (byte)channelQuantity;
            reader.SetUserDefineFrequency(m_curSetting.btReadId, nStartFrequency, btFrequencyInterval, btChannelQuantity);
            m_curSetting.btRegion = 4;
            m_curSetting.nUserDefineStartFrequency = nStartFrequency;
            m_curSetting.btUserDefineFrequencyInterval = btFrequencyInterval;
            m_curSetting.btUserDefineChannelQuantity = btChannelQuantity;
        }

        /// <summary>
        /// 设置频谱
        /// </summary>
        public void SetFrequencyRegion(int nStartFrequency, int nFrequencyInterval, int endRegion)
        {
            byte btRegion = (byte)nStartFrequency;
            byte btStartRegion = (byte)nFrequencyInterval;
            byte btEndRegion = (byte)endRegion;
            reader.SetFrequencyRegion(m_curSetting.btReadId, btRegion, btStartRegion, btEndRegion);
            m_curSetting.btRegion = btRegion;
            m_curSetting.btFrequencyStart = btStartRegion;
            m_curSetting.btFrequencyEnd = btEndRegion;
        }

        /// <summary>
        /// 获取频谱
        /// </summary>
        public void GetFrequencyRegion()
        {
            reader.GetFrequencyRegion(m_curSetting.btReadId);
        }

        public void SetBeeperMode(int beepMode)
        {
            byte btBeepMode = (byte)beepMode;
            reader.SetBeeperMode(m_curSetting.btReadId, btBeepMode);
            m_curSetting.btBeeperMode = btBeepMode;
        }

        /// <summary>
        /// 设备重启
        /// </summary>
        public int Reset()
        {
            int nRet = reader.Reset(m_curSetting.btReadId);
            var content = $"设备重启,nRet:{nRet == 0}";
            s_log.Info(content);
            if (nRet == 0)
            {
                m_curSetting.btReadId = (byte)0xFF;
            }
            return nRet;
        }

        /// <summary>
        /// 实时盘存
        /// </summary>
        /// <param name="repeat">
        /// 盘存过程重复的次数。        
        /// Repeat = 0xFF则此轮盘存时间为最短时间。
        /// 如果射频区域内只有一张标签，则此轮的盘存约耗时为30-50mS。
        /// 一般在四通道机器上快速轮询多个天线时使用此参数值
        /// </param>
        public void StartInventory(int repeat)
        {
            this.nRepeat = repeat;
            this.bLoopInventory = true;
            byte btRepeat = (byte)repeat;

            reader.InventoryReal(m_curSetting.btReadId, btRepeat);
        }

        /// <summary>
        /// 多天线轮询快速盘存,数组必须是4的倍数
        /// 天线数值必须大于0
        /// </summary>
        /// <param name="ants">天线数组,4的倍数</param>
        public void StartInventoryFast(List<int> ants)
        {
            this.fastAnts = ants;
            this.bLoopInventory = true;

            byte[] bts = new byte[ants.Count * 2 + 2];
            int j = 0;
            for (int i = 0; i < ants.Count; i++)
            {
                int ant = ants[i];
                bts[j++] = (byte)i;
                bts[j++] = (byte)(ant == 1 ? 0x01 : 0x00);
            }
            bts[j++] = 0x00;  //Interval 天线间的休息时间。单位是mS。休息时无射频输出，可降低功耗。
            bts[j++] = 0x01;  //Repeat	重复以上天线切换顺序次数。

            reader.FastSwitchInventory(m_curSetting.btReadId, bts);
        }

        /// <summary>
        /// 停止实时盘存
        /// </summary>
        public void StopInventory()
        {
            bLoopInventory = false;
        }

        /// <summary>
        /// 设置蜂鸣器
        /// </summary>
        //public void SetBeep()
        //{
        //}

        private void SetUserDefineFrequencyWithCN()
        {
            //1.设置国内频点
            int nStartFrequency = 920000;
            int nFrequencyInterval = 500;
            int channelQuantity = 16;
            SetUserDefineFrequency(nStartFrequency, nFrequencyInterval, channelQuantity);
        }

        private void AnalyData(object sender, Reader.MessageTran msgTran)
        {

            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            switch (msgTran.Cmd)
            {
                #region 0x7x
                case 0x71:
                    //ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                    //ProcessSetReadAddress(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    //ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x97:
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                    ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7E:
                    //ProcessGetImpedanceMatch(msgTran);
                    break;
                #endregion //0x7x

                #region 0x8x  
                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x8A:
                    ProcessFastSwitch(msgTran);
                    break;
                #endregion //0x8x  

                default:
                    break;
            }
        }

        private void SendData(object sender, byte[] data)
        {
            string content = string.Format("{0}:{1}", "Send", ReaderUtils.ToHex(data, "", " "));
            s_log.Info(content);
            print(content, 0);
        }

        private void RecvData(object sender, TransportDataEventArgs e)
        {
            string content = string.Format("{0}:{1}", e.Tx ? "Send" : "Recv", ReaderUtils.ToHex(e.Data, "", " "));
            s_log.Info(content);
            print(content, e.Tx ? 0 : 1);
        }


        private void OnError(object sender, ErrorReceivedEventArgs e)
        {
            string content = $"异常 {e.Err} ";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessSetWorkAntenna(Reader.MessageTran msgTran)
        {
            int intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            string strCmd = "设置工作天线,当前工作天线: 天线" + intCurrentAnt.ToString();
            string content = string.Empty;
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    content = $"{strCmd} 成功";
                    s_log.Info(content);
                    print(content, 0);

                    if (isFirstSet)
                    {
                        SetUserDefineFrequencyWithCN();
                        isFirstSet = false;
                    }
                    return;
                }
                else
                {
                    strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }
            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        /// <summary>
        /// 设置输出功率
        /// 结果解析
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessSetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "设置输出功率";
            string strErrorCode = string.Empty;
            string content = string.Empty;
            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    content = $"{strCmd} 成功";
                    s_log.Info(content);
                    print(content, 0);

                    //设置工作天线
                    if (isFirstSet)
                    {
                       SetWorkAntenna();
                    }

                    return;
                }
                else
                {
                    strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessGetFirmwareVersion(MessageTran msgTran)
        {
            string strCmd = "获取版本";
            string strErrorCode = string.Empty;
            string content = string.Empty;
            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btMajor = msgTran.AryData[0];
                m_curSetting.btMinor = msgTran.AryData[1];
                m_curSetting.btReadId = msgTran.ReadId;

                var ver = m_curSetting.btMajor.ToString() + "." + m_curSetting.btMinor.ToString();
                //异步回调
                callBack(msgTran.Cmd, ver);

                content = $"{strCmd} {ver}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = ReaderUtils.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = string.Format("{0}", "未知错误");
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessGetReaderTemperature(Reader.MessageTran msgTran)
        {
            string strCmd = "获取读写器温度";
            string strErrorCode = string.Empty;
            string content = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btPlusMinus = msgTran.AryData[0];
                m_curSetting.btTemperature = msgTran.AryData[1];

                var strTemperature = "";
                if (m_curSetting.btPlusMinus == 0x0)
                {
                    strTemperature = "-" + m_curSetting.btTemperature.ToString() + "℃";
                }
                else
                {
                    strTemperature = m_curSetting.btTemperature.ToString() + "℃";
                }

                //异步回调
                callBack(msgTran.Cmd, strTemperature);

                content = $"{strCmd} {strTemperature}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = ReaderUtils.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = string.Format("{0}", "未知错误");
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }


        private void ProcessGetOutputPower(MessageTran msgTran)
        {
            string strCmd = "获取输出功率";
            string strErrorCode = string.Empty;
            string content = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPower = msgTran.AryData[0];

                string strpower = string.Format("{0}", m_curSetting.btOutputPower);
                //异步回调
                callBack(msgTran.Cmd, strpower);

                content = $"{strCmd} {strpower}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else if (msgTran.AryData.Length == 8 || msgTran.AryData.Length == 4)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPowers = msgTran.AryData;

                string strpower = string.Format("{0}", m_curSetting.btOutputPowers[0]);
                //异步回调
                callBack(msgTran.Cmd, strpower);
                content = $"{strCmd} {strpower}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else
            {
                strErrorCode = string.Format("{0}", "未知错误");
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessSetFrequencyRegion(Reader.MessageTran msgTran)
        {
            string strCmd = "设置射频规范";
            string strErrorCode = string.Empty;
            string content = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    content = $"{strCmd} 成功";
                    s_log.Info(content);
                    print(content, 0);
                    return;
                }
                else
                {
                    strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessSetBeeperMode(Reader.MessageTran msgTran)
        {
            string strCmd = "设置蜂鸣器";
            string strErrorCode = string.Empty;
            string content = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    content = $"{strCmd} 成功";
                    s_log.Info(content);
                    print(content, 0);
                    return;
                }
                else
                {
                    strErrorCode = ReaderUtils.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }
            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessGetFrequencyRegion(Reader.MessageTran msgTran)
        {
            string strCmd = "获取射频规范";
            string strErrorCode = string.Empty;
            string content = string.Empty;

            if (msgTran.AryData.Length == 3)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btFrequencyStart = msgTran.AryData[1];
                m_curSetting.btFrequencyEnd = msgTran.AryData[2];

                string info = string.Format("{0}-{1}-{2}", m_curSetting.btFrequencyStart, m_curSetting.btFrequencyEnd = msgTran.AryData[2], 0);
                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd} {info}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else if (msgTran.AryData.Length == 6)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btUserDefineFrequencyInterval = msgTran.AryData[1];
                m_curSetting.btUserDefineChannelQuantity = msgTran.AryData[2];
                m_curSetting.nUserDefineStartFrequency = msgTran.AryData[3] * 256 * 256 + msgTran.AryData[4] * 256 + msgTran.AryData[5];

                string info = string.Format("{0}-{1}-{2}", m_curSetting.nUserDefineStartFrequency, m_curSetting.btUserDefineFrequencyInterval * 10,
                    m_curSetting.btUserDefineChannelQuantity);

                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd} {info}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = string.Format("{0}", "未知错误");
            }

            content = $"{strCmd} 失败，错误描述：{strErrorCode}";
            s_log.Error(content);
            print(content, 1);
        }

        private void ProcessInventoryReal(Reader.MessageTran msgTran)
        {
            string strCmd = "实时盘存";
            string strErrorCode = string.Empty;
            string content = string.Empty;
            //读写器询检失败
            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                content = $"{strCmd} {strLog}";
                s_log.Info(content);
                print(content, 1);

                RunLoopInventroy();
            }
            //读写器询检成功，状态信息返回
            else if (msgTran.AryData.Length == 7)
            {
                int nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                int nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                string info = string.Format("{0}-{1}", nReadRate, nDataCount);
                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd}  ReadRate:{nReadRate},DataCount:{nDataCount}";
                s_log.Info(content);
                print(content, 0);

                RunLoopInventroy();
            }
            //读写器询检成功，数据信息返回
            else
            {
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;

                //1.获取epc信息
                string strEPC = CommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                //2.获取pc信息
                string strPC = CommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                //3.获取RSSI信息
                string strRSSI = (msgTran.AryData[nLength - 1] & 0x7F).ToString();


                byte btTemp = msgTran.AryData[0];
                //4.获取天线号
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                if ((msgTran.AryData[nLength - 1] & 0x80) != 0) btAntId += 4;
                string strAntId = btAntId.ToString();
                //5.获取频点
                byte btFreq = (byte)(btTemp >> 2);
                string strFreq = GetFreqString(btFreq);

                string info = string.Format("{0}-{1}-{2}-{3}-{4}", strPC, strEPC, strAntId, strFreq, strRSSI);

                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd} {info}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
        }

        private void ProcessFastSwitch(Reader.MessageTran msgTran)
        {
            string strCmd = "快速盘存";
            string strErrorCode = string.Empty;
            string content = string.Empty;
            //读写器询检失败
            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                content = $"{strCmd} {strLog}";
                s_log.Info(content);
                print(content, 1);

                //TODO 取消循环盘存,适用于web端调用
                RunLoopFastInventroy();
            }
            //读写器询检成功，状态信息返回
            else if (msgTran.AryData.Length == 7)
            {
                int nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                int nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                string info = string.Format("{0}-{1}", nReadRate, nDataCount);
                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd}  ReadRate:{nReadRate},DataCount:{nDataCount}";
                s_log.Info(content);
                print(content, 0);

                //TODO 取消循环盘存, 适用于web端调用
                RunLoopFastInventroy();
            }
            //读写器询检成功，数据信息返回
            else
            {
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;

                //1.获取epc信息
                string strEPC = CommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                //2.获取pc信息
                string strPC = CommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                //3.获取RSSI信息
                string strRSSI = (msgTran.AryData[nLength - 1] & 0x7F).ToString();


                byte btTemp = msgTran.AryData[0];
                //4.获取天线号
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                if ((msgTran.AryData[nLength - 1] & 0x80) != 0) btAntId += 4;
                string strAntId = btAntId.ToString();
                //5.获取频点
                byte btFreq = (byte)(btTemp >> 2);
                string strFreq = GetFreqString(btFreq);

                string info = string.Format("{0}-{1}-{2}-{3}-{4}", strPC, strEPC, strAntId, strFreq, strRSSI);

                //异步回调
                callBack(msgTran.Cmd, info);

                content = $"{strCmd} {info}";
                s_log.Info(content);
                print(content, 0);
                return;
            }
        }
        private string GetFreqString(byte btFreq)
        {
            string strFreq = string.Empty;

            if (m_curSetting.btRegion == 4)
            {
                float nExtraFrequency = btFreq * m_curSetting.btUserDefineFrequencyInterval * 10;
                float nstartFrequency = ((float)m_curSetting.nUserDefineStartFrequency) / 1000;
                float nStart = nstartFrequency + nExtraFrequency / 1000;
                string strTemp = nStart.ToString("0.000");
                return strTemp;
            }
            else
            {
                if (btFreq < 0x07)
                {
                    float nStart = 865.00f + Convert.ToInt32(btFreq) * 0.5f;
                    string strTemp = nStart.ToString("0.00");
                    return strTemp;
                }
                else
                {
                    float nStart = 902.00f + (Convert.ToInt32(btFreq) - 7) * 0.5f;
                    string strTemp = nStart.ToString("0.00");
                    return strTemp;
                }
            }
        }

        private void RunLoopInventroy()
        {
            string strCmd = "实时盘存";
            //盘存
            if (bLoopInventory)
            {
                StartInventory(this.nRepeat);
                print(strCmd, 0);
                s_log.Info(strCmd);
            }
        }

        private void RunLoopFastInventroy()
        {
            string strCmd = "快速盘存";
            //盘存
            if (bLoopInventory)
            {
                StartInventoryFast(this.fastAnts);

                print(strCmd, 0);
                s_log.Info(strCmd);
            }
        }
    }
}
