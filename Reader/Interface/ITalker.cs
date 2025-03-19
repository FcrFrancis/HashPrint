using System;
using System.Net;

namespace Reader
{
    interface ITalker
    {
        event EventHandler<TransportDataEventArgs> EvRecvData;
        event EventHandler<ErrorReceivedEventArgs> EvException;

        bool Connect(IPAddress ip, int port, out string strException);// Connect to the server
        int OpenCom(string strPort, int nBaudrate, out string strException);
        bool SendMessage(byte[] btAryBuffer);//Send data
        void Disconnect();
        bool IsConnect();// Check whether the server is connected
    }
}
