using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Rfid
{
    public class AppConfiguration
    {
        private static string _secretKey;
        private static string _requestUrl;
        private static int _requestTimeout = -1;

        //RFID设备参数
        private static string _rfidCom;
        private static List<AntPower> _rfidAntPower = null;
        private static int _baudrate = -1;


        public static string RfidCom
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_rfidCom))
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("RfidCom"))
                    {
                        string temp = ConfigurationManager.AppSettings["RfidCom"];
                        _rfidCom = temp;
                    }
                }
                return _rfidCom;
            }
        }


        public static List<AntPower> RfidAntPower
        {
            get
            {
                if (_rfidAntPower == null)
                {
                    List<AntPower> list = new List<AntPower>();
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("RfidAntPower"))
                    {
                        //格式1:20,3:18
                        string temp = ConfigurationManager.AppSettings["RfidAntPower"];
                        string[] ants = temp.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (ants == null || ants.Length == 0)
                        {
                            return list;
                        }
                        foreach (string ant in ants)
                        {
                            string[] aps = ant.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (aps == null || aps.Length != 2)
                            {
                                return list;
                            }

                            AntPower antPow = new AntPower();
                            antPow.Ant = int.Parse(aps[0].Trim());
                            antPow.Power = int.Parse(aps[1].Trim());
                          
                            list.Add(antPow);
                        }
                    }
                    _rfidAntPower = list;
                }
                return _rfidAntPower;
            }
        }

        public static int Baudrate
        {
            get
            {
                if (_baudrate == -1)
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("Baudrate"))
                    {
                        string temp = ConfigurationManager.AppSettings["Baudrate"];
                        int.TryParse(temp, out _baudrate);
                    }
                }
                return _baudrate;
            }
        }



        public static string RequestUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_requestUrl))
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("RequestUrl"))
                    {
                        string requestUrl = ConfigurationManager.AppSettings["RequestUrl"];
                        _requestUrl = requestUrl;
                    }
                }
                return _requestUrl;
            }
        }

        public static int RequestTimeout
        {
            get
            {
                if (_requestTimeout == -1)
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("RequestTimeout"))
                    {
                        string temp = ConfigurationManager.AppSettings["RequestTimeout"];
                        int.TryParse(temp, out _requestTimeout);
                    }
                }

                return _requestTimeout;
            }
        }



        public static string SecretKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_secretKey))
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("SecretKey"))
                    {
                        string reuestKey = ConfigurationManager.AppSettings["SecretKey"];
                        _secretKey = reuestKey;
                    }
                }
                return _secretKey;
            }
        }





    }
}
