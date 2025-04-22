using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Reader
{
    public class ReaderUtils
    {
        private static bool useEnglish = false;
        ReaderUtils()
        {
        }
        public static byte[] StringArrayToByteArray(string[] strAryHex, int nLen)
        {
            if (strAryHex.Length < nLen)
            {
                nLen = strAryHex.Length;
            }

            byte[] btAryHex = new byte[nLen];

            try
            {
                int nIndex = 0;
                foreach (string strTemp in strAryHex)
                {
                    btAryHex[nIndex] = Convert.ToByte(strTemp, 16);
                    nIndex++;
                }
            }
            catch (System.Exception ex)
            {

            }

            return btAryHex;
        }

        public static string ByteArrayToString(byte[] btAryHex, int nIndex, int nLen)
        {
            if (nIndex + nLen > btAryHex.Length)
            {
                nLen = btAryHex.Length - nIndex;
            }

            string strResult = string.Empty;

            for (int nloop = nIndex; nloop < nIndex + nLen; nloop++)
            {
                string strTemp = string.Format(" {0:X2}", btAryHex[nloop]);

                strResult += strTemp;
            }

            return strResult;
        }

        /// <summary>
        /// Intercepts and converts a string to a specified length as an array of characters. Spaces are ignored
        /// </summary>
        /// <param name="strValue"></param>
        /// <param name="nLength"></param>
        /// <returns></returns>
        public static string[] StringToStringArray(string strValue, int nLength)
        {
            string[] strAryResult = null;

            if (!string.IsNullOrEmpty(strValue))
            {
                System.Collections.ArrayList strListResult = new System.Collections.ArrayList();
                string strTemp = string.Empty;
                int nTemp = 0;

                for (int nloop = 0; nloop < strValue.Length; nloop++)
                {
                    if (strValue[nloop] == ' ')
                    {
                        continue;
                    }
                    else
                    {
                        nTemp++;

                        // Check whether the intercepted characters are between A~F and 0~9, or exit directly if not
                        System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"^(([A-F])*(\d)*)$");
                        if (!reg.IsMatch(strValue.Substring(nloop, 1)))
                        {
                            return strAryResult;
                        }

                        strTemp += strValue.Substring(nloop, 1);

                        // Determine whether the interception length has been reached
                        if ((nTemp == nLength) || (nloop == strValue.Length - 1 && !string.IsNullOrEmpty(strTemp)))
                        {
                            strListResult.Add(strTemp);
                            nTemp = 0;
                            strTemp = string.Empty;
                        }
                    }
                }

                if (strListResult.Count > 0)
                {
                    strAryResult = new string[strListResult.Count];
                    strListResult.CopyTo(strAryResult);
                }
            }

            return strAryResult;
        }
        public static string FormatCommException(CommExceptionCode code)
        {
            return (useEnglish ? FormatCommExceptionEN(code) : FormatCommExceptionCN(code));
        }

        public static string FormatCommExceptionEN(CommExceptionCode code)
        {
            string strErrorCode = "";
            switch (code)
            {
                case CommExceptionCode.ConnectToServerFail:
                    strErrorCode = "Connect timeout，Failed to connect to the specified server";
                    break;
                case CommExceptionCode.ConnectError:
                    strErrorCode = "Connect Error";
                    break;
                case CommExceptionCode.DataRecvError:
                    strErrorCode = "Data Receive Error";
                    break;
                case CommExceptionCode.DataSendError:
                    strErrorCode = "Data Send Error";
                    break;
                case CommExceptionCode.ReconnectSuccess:
                    strErrorCode = "Reconnect Success";
                    break;
                case CommExceptionCode.ReconnectFailed:
                    strErrorCode = "Reconnect Failed";
                    break;
                case CommExceptionCode.TcpLogout:
                    strErrorCode = "Logout";
                    break;
                case CommExceptionCode.NotTcpObj:
                    strErrorCode = "Not a tcp object";
                    break;
                case CommExceptionCode.NotSerialObj:
                    strErrorCode = "Not a serial object";
                    break;
                case CommExceptionCode.CommError:
                    strErrorCode = "Communication error";
                    break;
            }
            return strErrorCode;
        }

        public static string FormatCommExceptionCN(CommExceptionCode code)
        {
            string strErrorCode = "";
            switch (code)
            {
                case CommExceptionCode.ConnectToServerFail:
                    strErrorCode = "连接超时，无法连接到指定的服务器";
                    break;
                case CommExceptionCode.ConnectError:
                    strErrorCode = "连接异常";
                    break;
                case CommExceptionCode.DataRecvError:
                    strErrorCode = "数据接收异常";
                    break;
                case CommExceptionCode.DataSendError:
                    strErrorCode = "数据发送异常";
                    break;
                case CommExceptionCode.ReconnectSuccess:
                    strErrorCode = "重连成功";
                    break;
                case CommExceptionCode.ReconnectFailed:
                    strErrorCode = "重连失败";
                    break;
                case CommExceptionCode.TcpLogout:
                    strErrorCode = "登出";
                    break;
                case CommExceptionCode.NotTcpObj:
                    strErrorCode = "不是一个Tcp对象";
                    break;
                case CommExceptionCode.NotSerialObj:
                    strErrorCode = "不是一个串口对象";
                    break;
                case CommExceptionCode.CommError:
                    strErrorCode = "通讯异常";
                    break;
            }
            return strErrorCode;
        }

        public static string FormatErrorCode(byte btErrorCode)
        {
            return (useEnglish ? FormatErrorCodeEN(btErrorCode) : FormatErrorCodeCN(btErrorCode));
        }

        public static string FormatErrorCodeEN(byte btErrorCode)
        {
            string strErrorCode = "";
            switch (btErrorCode)
            {
                case 0x10:
                    strErrorCode = "Command succeeded";
                    break;
                case 0x11:
                    strErrorCode = "Command failed";
                    break;
                case 0x20:
                    strErrorCode = "CPU reset error";
                    break;
                case 0x21:
                    strErrorCode = "Turn on CW error";
                    break;
                case 0x22:
                    strErrorCode = "Antenna is missing";
                    break;
                case 0x23:
                    strErrorCode = "Write flash error";
                    break;
                case 0x24:
                    strErrorCode = "Read flash error";
                    break;
                case 0x25:
                    strErrorCode = "Set output power error";
                    break;
                case 0x31:
                    strErrorCode = "Error occurred during inventory";
                    break;
                case 0x32:
                    strErrorCode = "Error occurred during read";
                    break;
                case 0x33:
                    strErrorCode = "Error occurred during write";
                    break;
                case 0x34:
                    strErrorCode = "Error occurred during lock";
                    break;
                case 0x35:
                    strErrorCode = "Error occurred during kill";
                    break;
                case 0x36:
                    strErrorCode = "There is no tag to be operated";
                    break;
                case 0x37:
                    strErrorCode = "Tag Inventoried but access failed";
                    break;
                case 0x38:
                    strErrorCode = "Buffer is empty";
                    break;
                case 0x40:
                    strErrorCode = "Access failed or wrong password";
                    break;
                case 0x41:
                    strErrorCode = "Invalid parameter";
                    break;
                case 0x42:
                    strErrorCode = "WordCnt is too long";
                    break;
                case 0x43:
                    strErrorCode = "MemBank out of range";
                    break;
                case 0x44:
                    strErrorCode = "Lock region out of range";
                    break;
                case 0x45:
                    strErrorCode = "LockType out of range";
                    break;
                case 0x46:
                    strErrorCode = "Invalid reader address";
                    break;
                case 0x47:
                    strErrorCode = "AntennaID out of range";
                    break;
                case 0x48:
                    strErrorCode = "Output power out of range";
                    break;
                case 0x49:
                    strErrorCode = "Frequency region out of range";
                    break;
                case 0x4A:
                    strErrorCode = "Baud rate out of range";
                    break;
                case 0x4B:
                    strErrorCode = "Buzzer behavior out of range";
                    break;
                case 0x4C:
                    strErrorCode = "EPC match is too long";
                    break;
                case 0x4D:
                    strErrorCode = "EPC match length wrong";
                    break;
                case 0x4E:
                    strErrorCode = "Invalid EPC match mode";
                    break;
                case 0x4F:
                    strErrorCode = "Invalid frequency range";
                    break;
                case 0x50:
                    strErrorCode = "Failed to receive RN16 from tag";
                    break;
                case 0x51:
                    strErrorCode = "Invalid DRM mode";
                    break;
                case 0x52:
                    strErrorCode = "PLL can not lock";
                    break;
                case 0x53:
                    strErrorCode = "No response from RF chip";
                    break;
                case 0x54:
                    strErrorCode = "Can't achieve desired output power level";
                    break;
                case 0x55:
                    strErrorCode = "Can't authenticate firmware copyright";
                    break;
                case 0x56:
                    strErrorCode = "Spectrum regulation wrong";
                    break;
                case 0x57:
                    strErrorCode = "Output power is too low";
                    break;
                case 0xFF:
                    strErrorCode = "Unknown error";
                    break;

                default:
                    strErrorCode = "Unknown error";
                    break;
            }

            return strErrorCode;
        }

        public static string FormatErrorCodeCN(byte btErrorCode)
        {
            string strErrorCode = "";
            switch (btErrorCode)
            {
                case 0x10:
                    strErrorCode = "命令已执行";
                    break;
                case 0x11:
                    strErrorCode = "命令执行失败";
                    break;
                case 0x20:
                    strErrorCode = "CPU 复位错误";
                    break;
                case 0x21:
                    strErrorCode = "打开CW 错误";
                    break;
                case 0x22:
                    strErrorCode = "天线未连接";
                    break;
                case 0x23:
                    strErrorCode = "写Flash 错误";
                    break;
                case 0x24:
                    strErrorCode = "读Flash 错误";
                    break;
                case 0x25:
                    strErrorCode = "设置发射功率错误";
                    break;
                case 0x31:
                    strErrorCode = "盘存标签错误";
                    break;
                case 0x32:
                    strErrorCode = "读标签错误";
                    break;
                case 0x33:
                    strErrorCode = "写标签错误";
                    break;
                case 0x34:
                    strErrorCode = "锁定标签错误";
                    break;
                case 0x35:
                    strErrorCode = "灭活标签错误";
                    break;
                case 0x36:
                    strErrorCode = "无可操作标签错误";
                    break;
                case 0x37:
                    strErrorCode = "成功盘存但访问失败";
                    break;
                case 0x38:
                    strErrorCode = "缓存为空";
                    break;
                case 0x40:
                    strErrorCode = "访问标签错误或访问密码错误";
                    break;
                case 0x41:
                    strErrorCode = "无效的参数";
                    break;
                case 0x42:
                    strErrorCode = "wordCnt 参数超过规定长度";
                    break;
                case 0x43:
                    strErrorCode = "MemBank 参数超出范围";
                    break;
                case 0x44:
                    strErrorCode = "Lock 数据区参数超出范围";
                    break;
                case 0x45:
                    strErrorCode = "LockType 参数超出范围";
                    break;
                case 0x46:
                    strErrorCode = "读卡器地址无效";
                    break;
                case 0x47:
                    strErrorCode = "Antenna_id 超出范围";
                    break;
                case 0x48:
                    strErrorCode = "输出功率参数超出范围";
                    break;
                case 0x49:
                    strErrorCode = "射频规范区域参数超出范围";
                    break;
                case 0x4A:
                    strErrorCode = "波特率参数超过范围";
                    break;
                case 0x4B:
                    strErrorCode = "蜂鸣器设置参数超出范围";
                    break;
                case 0x4C:
                    strErrorCode = "EPC 匹配长度越界";
                    break;
                case 0x4D:
                    strErrorCode = "EPC 匹配长度错误";
                    break;
                case 0x4E:
                    strErrorCode = "EPC 匹配参数超出范围";
                    break;
                case 0x4F:
                    strErrorCode = "频率范围设置参数错误";
                    break;
                case 0x50:
                    strErrorCode = "无法接收标签的RN16";
                    break;
                case 0x51:
                    strErrorCode = "DRM 设置参数错误";
                    break;
                case 0x52:
                    strErrorCode = "PLL 不能锁定";
                    break;
                case 0x53:
                    strErrorCode = "射频芯片无响应";
                    break;
                case 0x54:
                    strErrorCode = "输出达不到指定的输出功率";
                    break;
                case 0x55:
                    strErrorCode = "版权认证未通过";
                    break;
                case 0x56:
                    strErrorCode = "频谱规范设置错误";
                    break;
                case 0x57:
                    strErrorCode = "输出功率过低";
                    break;
                case 0xFF:
                    strErrorCode = "未知错误";
                    break;
                default:
                    break;
            }

            return strErrorCode;
        }

        #region FromHex
        /// <summary>
        /// Convert human-readable hex string to byte array;
        /// e.g., 123456 or 0x123456 -> {0x12, 0x34, 0x56};
        /// </summary>
        /// <param name="hex">Human-readable hex string to convert</param>
        /// <returns>Byte array</returns>
        public static byte[] FromHex(string hex)
        {
            int prelen = 0;

            if (hex.StartsWith("0x") || hex.StartsWith("0X"))
                prelen = 2;

            byte[] bytes = new byte[(hex.Length - prelen) / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                string bytestring = hex.Substring(prelen + (2 * i), 2);
                bytes[i] = byte.Parse(bytestring, System.Globalization.NumberStyles.HexNumber);
            }

            return bytes;
        }
        #endregion

        #region ToHex
        /// <summary>
        /// Convert byte array to human-readable hex string; e.g., {0x12, 0x34, 0x56} -> 123456
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <returns>Human-readable hex string</returns>
        public static string ToHex(byte[] bytes)
        {
            return ToHex(bytes, "0x", "");
        }

        /// <summary>
        /// Convert byte array to human-readable hex string; e.g., {0x12, 0x34, 0x56} -> 123456
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <param name="prefix">String to place before byte strings</param>
        /// <param name="separator">String to place between byte strings</param>
        /// <returns>Human-readable hex string</returns>
        public static string ToHex(byte[] bytes, string prefix, string separator)
        {
            if (null == bytes)
                return "null";

            List<string> bytestrings = new List<string>();

            foreach (byte b in bytes)
                bytestrings.Add(b.ToString("X2"));

            return prefix + String.Join(separator, bytestrings.ToArray());
        }

        /// <summary>
        /// Convert u16 array to human-readable hex string; e.g., {0x1234, 0x5678} -> 12345678
        /// </summary>
        /// <param name="words">u16 array to convert</param>
        /// <returns>Human-readable hex string</returns>
        public static string ToHex(UInt16[] words)
        {
            StringBuilder sb = new StringBuilder(4 * words.Length);

            foreach (UInt16 word in words)
                sb.Append(word.ToString("X4"));

            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// Extract unsigned 16-bit integer from big-endian byte string
        /// </summary>
        /// <param name="bytes">Source big-endian byte string</param>
        /// <param name="offset">Location to extract from.  Will be updated to post-decode offset.</param>
        /// <returns>Unsigned 16-bit integer</returns>
        public static UInt16 ToU16(byte[] bytes, ref int offset)
        {
            if (null == bytes) return default(byte);
            int hi = (UInt16)(bytes[offset++]) << 8;
            int lo = (UInt16)(bytes[offset++]);
            return (UInt16)(hi | lo);
        }
        #region ToU24
        public static UInt32 ToU24(byte[] bytes, ref int offset)
        {
            return (UInt32)(0
                | ((UInt32)(bytes[offset++]) << 16)
                | ((UInt32)(bytes[offset++]) << 8)
                | ((UInt32)(bytes[offset++]) << 0)
                );
        }
        #endregion

        #region ToU32
        /// <summary>
        /// Extract unsigned 32-bit integer from big-endian byte string
        /// </summary>
        /// <param name="bytes">Source big-endian byte string</param>
        /// <param name="offset">Location to extract from</param>
        /// <returns>Unsigned 32-bit integer</returns>
        public static UInt32 ToU32(byte[] bytes, ref int offset)
        {
            return (UInt32)(0
                | ((UInt32)(bytes[offset++]) << 24)
                | ((UInt32)(bytes[offset++]) << 16)
                | ((UInt32)(bytes[offset++]) << 8)
                | ((UInt32)(bytes[offset++]) << 0)
                );
        }
        #endregion

        public static byte[] GetIpAddrBytes(string ip)
        {
            return IPAddress.Parse(ip).GetAddressBytes();
            //List<byte> list = new List<byte>();
            //foreach(string str in ip.Split('.'))
            //{
            //    list.Add(byte.Parse(Convert.ToInt32(str).ToString("x"), System.Globalization.NumberStyles.HexNumber));
            //}
            //return list.ToArray() ;
        }

        public static bool CheckIpAddr(string ip)
        {
            IPAddress address;
            return IPAddress.TryParse(ip, out address);
        }

        public static bool CheckMacAddr(string macAddr)
        {
            string pattern = @"^([0-9a-fA-F]{2}:){5}([0-9a-fA-F]{2})$";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(macAddr);
        }

        public static byte CheckSum(byte[] btAryBuffer, int nStartPos, int nLen)
        {
            byte btSum = 0x00;

            for (int nloop = nStartPos; nloop < nStartPos + nLen; nloop++)
            {
                btSum += btAryBuffer[nloop];
            }

            return Convert.ToByte(((~btSum) + 1) & 0xFF);
        }

        public static bool CheckIsByteInt(string str)
        {
            str = str.Trim();
            if (str.Length == 0 || str.Length > 3)
                return false;
            Regex regex = new Regex(@"^(([0-9]){1}|([1-9][0-9]){1}|([1][0-9][0-9]){1}|(2[0-4][0-9]){1}|(25[0-5]){1})$");
            return regex.IsMatch(str);
        }

        public static bool CheckIsInt(string str)
        {
            str = str.Trim();
            if (str.Length == 0)
                return false;
            Regex regex = new Regex(@"^([0-9]*)$|^(-1){1}$");
            return regex.IsMatch(str);
        }

        public static bool CheckFourBytesPwd(string str)
        {
            str = str.Trim();
            if (str.Length != 8)
                return false;
            Regex regex = new Regex(@"^([0-9a-fA-F]){8}$");
            return regex.IsMatch(str);
        }
    }

    public enum CommExceptionCode
    {
        //TCP
        ConnectToServerFail = 0x00,
        ConnectError = 0x01,
        DataRecvError = 0x02,
        DataSendError = 0x03,
        ReconnectSuccess = 0x04,
        ReconnectFailed = 0x05,
        TcpLogout = 0x06,
        NotTcpObj = 0x7,
        NotSerialObj = 0x8,
        CommError = 0x9,
    }
}