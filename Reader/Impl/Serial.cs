using System;
using System.IO.Ports;
using System.Net;

namespace Reader
{
    class Serial : ITalker
    {
        #region Interface
        public event EventHandler<TransportDataEventArgs> EvRecvData;
        public event EventHandler<ErrorReceivedEventArgs> EvException;
        #endregion //Interface

        private SerialPort iSerialPort = null;

        #region Interface
        public int OpenCom(string strPort, int nBaudrate, out string strException)
        {
            if (iSerialPort == null)
            {
                iSerialPort = new SerialPort();
                iSerialPort.DataReceived += ISerialPort_DataReceived;
                iSerialPort.ErrorReceived += ISerialPort_ErrorReceived;
            }
            strException = string.Empty;

            if (iSerialPort.IsOpen)
            {
                iSerialPort.Close();
            }

            try
            {
                iSerialPort.PortName = strPort;
                iSerialPort.BaudRate = nBaudrate;
                iSerialPort.DataBits = 8;
                iSerialPort.StopBits = StopBits.One;
                iSerialPort.Parity = Parity.None;
                iSerialPort.ReadTimeout = 500;
                iSerialPort.WriteTimeout = 5000;
                iSerialPort.ReadBufferSize = 4096 * 10;
                iSerialPort.Open();
            }
            catch (Exception ex)
            {
                strException = ex.Message;
                return -1;
            }
            return 0;
        }

        public bool Connect(IPAddress ip, int port, out string strException)
        {
            throw new Exception(ReaderUtils.FormatCommException(CommExceptionCode.NotTcpObj));
        }

        public bool IsConnect()
        {
            return iSerialPort.IsOpen;
        }

        public bool SendMessage(byte[] btArySenderData)
        {
            if (!iSerialPort.IsOpen)
            {
                return false;
            }

            iSerialPort.Write(btArySenderData, 0, btArySenderData.Length);
            return true;
        }

        public void Disconnect()
        {
            if (iSerialPort.IsOpen)
            {
                iSerialPort.Close();
            }
        }
        #endregion //Interface
        
        protected void OnTransport(bool tx, byte[] data)
        {
            EvRecvData?.Invoke(this, new TransportDataEventArgs(tx, data));
        }

        protected void OnReadException(string exStr, Exception e)
        {
            EvException?.Invoke(this, new ErrorReceivedEventArgs(exStr, e));
        }

        private void ISerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int nLenRead = iSerialPort.BytesToRead;

            if (nLenRead == 0)
            {
                return;
            }

            byte[] btAryBuffer = new byte[nLenRead];
            iSerialPort.Read(btAryBuffer, 0, nLenRead);
            if (EvRecvData != null)
            {
                byte[] btAryReceiveData = new byte[nLenRead];

                Array.Copy(btAryBuffer, btAryReceiveData, nLenRead);

                OnTransport(false, btAryReceiveData);
            }
        }

        private void ISerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            OnReadException(string.Format("{0}", ReaderUtils.FormatCommException(CommExceptionCode.CommError)),
                new CommException(CommExceptionCode.CommError));
        }
    }
}
