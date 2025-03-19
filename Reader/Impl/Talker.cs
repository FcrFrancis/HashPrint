using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Reader
{
    public class Talker : ITalker
    {
        #region Interface
        public event EventHandler<TransportDataEventArgs> EvRecvData;
        public event EventHandler<ErrorReceivedEventArgs> EvException;
        #endregion //Interface

        protected void OnTransport(bool tx, byte[] data)
        {
            EvRecvData?.Invoke(this, new TransportDataEventArgs(tx, data));
        }

        protected void OnReadException(string exStr, Exception e)
        {
            EvException?.Invoke(this, new ErrorReceivedEventArgs(exStr, e));
        }

        //TcpClient client;
        Socket tcpClient;
        private Thread waitThread = null;
        private bool firstConnect = true;
        private bool bIsConnect = false;
        private IPAddress ipAddress;
        private int nPort;
        private int tryReconnectTimes = 0;
        private bool isRecv = false;
        private bool isReconnect = false;
        private bool reconnecting = false;
        private Thread reconnectThread = null;

        private const int connectTimeout = 1000; // connect timeout

        #region Interface
        public int OpenCom(string strPort, int nBaudrate, out string strException)
        {
            throw new Exception(ReaderUtils.FormatCommException(CommExceptionCode.NotSerialObj));
        }

        public bool Connect(IPAddress ipAddress, int nPort, out string strException)
        {
            bool ret = false;
            if (firstConnect)
            {
                this.ipAddress = ipAddress;
                this.nPort = nPort;
                firstConnect = false;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
            strException = String.Empty;

            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult ar = tcpClient.BeginConnect(ipAddress, nPort, null, null);
            bool success = ar.AsyncWaitHandle.WaitOne(connectTimeout);
            if (!success)
            {
                strException = String.Format("[{0}@{1}] {2}", ipAddress.ToString(), nPort,
                    ReaderUtils.FormatCommException(CommExceptionCode.ConnectToServerFail));
                ret = false;
            }
            else
            {
                try
                {
                    // Start KeppAlive detection
                    if (tcpClient == null)
                    {
                        return false;
                    }
                    tcpClient.EndConnect(ar);
                    tcpClient.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 300, 300), null);//Set the keep-alive parameter

                    if (!bIsConnect)
                    {
                        // Set up a thread to receive data from the server
                        ThreadStart stThead = new ThreadStart(ReceivedData);
                        waitThread = new Thread(stThead);
                        waitThread.IsBackground = true;
                        waitThread.Start();
                    }

                    bIsConnect = true;
                    ret = true;
                }
                catch (Exception e)
                {
                    strException = String.Format("[{0}@{1}] {2}: {3}", ipAddress.ToString(), nPort,
                        ReaderUtils.FormatCommException(CommExceptionCode.ConnectError), e.Message);
                    Thread.Sleep(connectTimeout);
                    ret = false;
                }
            }
            return ret;
        }

        public bool SendMessage(byte[] btAryBuffer)
        {
            try
            {
                lock (tcpClient)
                {
                    tcpClient.Send(btAryBuffer);
                    return true;
                }
            }
            catch (Exception e)
            {
                OnReadException("SendMessage", 
                    new CommException(CommExceptionCode.DataSendError, 
                    string.Format("[{0}@{1}] {2} {3}", ipAddress.ToString(), nPort,
                    ReaderUtils.FormatCommException(CommExceptionCode.DataSendError), e.Message)));
                return false;
            }
        }

        public void Disconnect()
        {
            isReconnect = false;
            isRecv = false;
            bIsConnect = false;
            //firstConnect = true;

            //if (tcpClient != null)
            //{
            //    tcpClient.Close();
            //    tcpClient = null;
            //}
        }

        public bool IsConnect()
        {
            return bIsConnect;
        }
        #endregion //Interface

        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(buffer, 0); // Whether to enable Keep-alive
            BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4); // How long will it take for the first probe to start (in milliseconds)
            BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);// Detection time interval (in milliseconds)
            return buffer;
        }

        private void ReceivedData()
        {
            isRecv = true;
            while (isRecv)
            {
                if (reconnecting)
                    continue;
                if (tcpClient!=null && tcpClient.Poll(3000, SelectMode.SelectRead))
                {
                    try
                    {
                        byte[] btAryBuffer = new byte[4096 * 10];
                        int nLenRead = tcpClient.Receive(btAryBuffer);
                        if (nLenRead == 0)
                        {
                            continue;
                        }
                        if (EvRecvData != null)
                        {
                            byte[] btAryReceiveData = new byte[nLenRead];

                            Array.Copy(btAryBuffer, btAryReceiveData, nLenRead);

                            OnTransport(false, btAryReceiveData);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException)
                        {
                            SocketError err = ((SocketException)ex).SocketErrorCode;
                            if (err.Equals(SocketError.ConnectionReset))
                            {
                                OnReadException("ReceivedData", 
                                    new CommException(CommExceptionCode.DataRecvError,
                                    string.Format("[{0}@{1}] {2}: {3}", ipAddress.ToString(), nPort,
                                    ReaderUtils.FormatCommException(CommExceptionCode.DataRecvError), ex.Message)));
                                Reconnect();
                            }
                        }
                        else
                        {
                            OnReadException("ReceivedData",
                                new CommException(CommExceptionCode.CommError));
                        }
                    }
                }
            }
            if(!bIsConnect)
            {
                firstConnect = true;

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                OnReadException("Disconnect",
                    new CommException(CommExceptionCode.TcpLogout,
                    string.Format("[{0}@{1}] {2}", ipAddress.ToString(), nPort,
                    ReaderUtils.FormatCommException(CommExceptionCode.TcpLogout))));
            }
        }

        private void Reconnect()
        {
            reconnecting = true;
            reconnectThread = new Thread(new ThreadStart(TryReconnect));
            reconnectThread.Start();
        }

        private void TryReconnect()
        {
            isReconnect = true;
            reconnecting = true;
            CommExceptionCode code;
            while (isReconnect)
            {
                if (Connect(this.ipAddress, this.nPort, out string strException))
                {
                    code = CommExceptionCode.ReconnectSuccess;
                    isReconnect = false;
                }
                else
                {
                    code = CommExceptionCode.ReconnectFailed;
                    tryReconnectTimes++;
                }
                OnReadException("TryReconnect", 
                    new CommException(code, 
                    string.Format("[{0}@{1}] [{2}] {3} {4}", ipAddress.ToString(), nPort, 
                    tryReconnectTimes, ReaderUtils.FormatCommException(code), strException)));
            }
            reconnecting = false;
            tryReconnectTimes = 0;
        }
    }
}
