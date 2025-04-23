using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Resources;
using System.Globalization;
using ReaderUart;
using log4net;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Linq;
using System.Timers;
using System.Configuration;
using UHFDemo.Model;
using Newtonsoft.Json;

namespace UHFDemo
{
    public partial class RFIDUartDemo : Form
    {
        private static readonly ILog s_log = LogManager.GetLogger(typeof(RFIDUartDemo));

        private UartHelper uart;
        private List<string> epcList = new List<string>();
        private long readTotal = 0;
        #region FindResource
        ResourceManager LocRM;

        private WebSocketServer _wssv;
        private CancellationTokenSource _cts;
        private List<WebSocket> _clients = new List<WebSocket>(); // 存储当前连接的客户端
        private System.Windows.Forms.Timer timer;
        private static int interval = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"].ToString().Trim());
        private static string webSocketAddress = ConfigurationManager.AppSettings["WebScoketAddress"].ToString().Trim();
        private void StartWebSocketServer()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                _wssv = new WebSocketServer(webSocketAddress);
                _wssv.AddWebSocketService<Echo>("/echo");
                Task.Run(() =>
                {
                    _wssv.Start();
                    Console.WriteLine("WebSocket server started at ws://localhost:5000/echo");

                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                    }
                    _wssv.Stop();
                    Console.WriteLine("WebSocket server stopped.");
                }, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start WebSocket server: {ex.Message}");
            }
        }

        private void initFindResource()
        {
            LocRM = new ResourceManager("UHFDemo.WinFormString", typeof(RFIDUartDemo).Assembly);
        }

        public string FindResource(string key)
        {
            try
            {
                return LocRM.GetString(key);
            }
            catch (Exception ex)
            {
                WriteLog(richTextBox1, ex.Message, 1);
                //throw new Exception(string.Format("{0}= {1}, {2}", FindResource("tipNotContainsKey"), key, ex.Message));
            }
            return "No";
        }
        #endregion //FindResource

        public RFIDUartDemo()
        {
            //CultureInfo.DefaultThreadCurrentUICulture
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");//zh-CN
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");//en-US
            InitializeComponent();
            initFindResource();
            Text = string.Format("{0}{1}.{2}",
                FindResource("DemoName"),
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor);

            DoubleBuffered = true;
            uart = new UartHelper(Print, Execute);
            uart.Init();
            //开启websocket
            StartWebSocketServer();
        }
        private void R2000UartDemo_Load(object sender, EventArgs e)
        {
            //Sets the validity of interface elements
            radio_btn_rs232.Checked = true;
            cmbBaudrate.SelectedIndex = 1; // 115200
            tabCtrMain.TabPages[1].Enabled = false;

            //Initializes the default configuration of the connection reader
            RefreshComPorts();
            //GenerateColmnsDataGridForInv();

            // 创建并配置Timer
            timer = new System.Windows.Forms.Timer();
            timer.Interval = interval;
            timer.Tick += Timer_Tick; // 订阅Tick事件
            timer.Start(); // 启动Timer
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //发送数据
            if (Echo.IsSend)
            {
                SendData();
            }
            //刷新界面
            if (Echo.IsRefresh)
            {
                refresh();
                Echo.IsRefresh = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 确保在Form关闭时停止Timer
            timer.Stop();
            timer.Dispose();
            base.OnFormClosed(e);
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
                case 0x97:
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
                case 0x8B:
                case 0x8A:
                    GetData(info);
                    break;
                default: break;
            }
        }

        private void GetFirmwareVersion(string info)
        {
            this.Invoke(new Action(() => txtFirmwareVersion.Text = info));
        }

        private void GetTemperature(string info)
        {
            this.Invoke(new Action(() => txtReaderTemperature.Text = info));
        }

        private void GetPower(string info)
        {
            this.Invoke(new Action(() => txtPower.Text = info));
        }

        private void GetFrequencyRegion(string info)
        {

            this.Invoke(new Action(() =>
            {
                var s = info.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                txtStartFreq.Text = s[0];
                txtFreqInterval.Text = s[1];
                txtFreqQuantity.Text = s[2];
            }));
        }

        private bool FilterEpc(string epc)
        {
            string filter = txtEpc.Text;
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (chkNot.Checked)
            {
                return !epc.Trim().StartsWith(filter);
            }
            else
            {
                return epc.Trim().StartsWith(filter);
            }
        }

        /// <summary>
        /// 数据解析
        /// string info = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", strPC, strEPC, strAntId, strFreq, strRSSI,strTid);
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
            Console.WriteLine(info);
            if (data != null && data.Length >= 6)
            {
                var epc = data[1];
                readTotal++;
                //过滤epc
                if (!FilterEpc(epc)) return;

                if (!epcList.Contains(epc))
                {
                    epcList.Add(epc);
                    this.Invoke(new Action(() =>
                    {
                        lock (dgvInventoryTagResults)
                        {
                            var num = dgvInventoryTagResults.Rows.Count;
                            var item = new object[8];
                            item[0] = num;
                            item[1] = 1;
                            item[2] = data[0];
                            item[3] = data[1];
                            item[4] = data[5];
                            item[5] = data[2];
                            item[6] = data[3];
                            item[7] = (Convert.ToInt32(data[4]) - 129).ToString();
                            dgvInventoryTagResults.Rows.Add(item);
                            txtCmdTagCount.Text = epcList.Count.ToString();
                        }
                    }));
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        lock (dgvInventoryTagResults)
                        {
                            var row = FindDataGridViewRow(epc);
                            if (row != null)
                            {
                                var times = Convert.ToInt32(row.Cells[1].Value);
                                times++;
                                row.Cells[1].Value = times;
                            }
                        }
                    }));
                }
            }
            else if (data != null && data.Length == 2)
            {
                this.Invoke(new Action(() =>
                {
                    int dataRate = Convert.ToInt32(data[0]);
                    int dataCount = Convert.ToInt32(data[1]);
                    lblRate.Text = data[0];
                    lblCnt.Text = readTotal.ToString();
                    if (dataRate != 0)
                    {
                        lblExecTime.Text = (dataCount * 1000 / dataRate).ToString();
                    }

                }));
            }
        }

        private DataGridViewRow FindDataGridViewRow(string epc)
        {
            try
            {
                for (int i = 0; i < dgvInventoryTagResults.Rows.Count; i++)
                {
                    var vepc = dgvInventoryTagResults.Rows[i].Cells[3].Value;
                    if (vepc == null) return null;

                    if (vepc.Equals(epc))
                    {
                        return dgvInventoryTagResults.Rows[i];
                    }
                }
            }
            catch (Exception ex)
            {
                s_log.Error(ex);
            }

            return null;
        }


        private void RefreshComPorts()
        {
            cmbComPort.Items.Clear();
            string[] portNames = SerialPort.GetPortNames();
            if (portNames != null && portNames.Length > 0)
            {
                cmbComPort.Items.AddRange(portNames);
            }
            cmbComPort.SelectedIndex = cmbComPort.Items.Count - 1;
        }

        public void Print(string strText, int nType)
        {
            this.Invoke(new Action(() =>
                WriteLog(richTextBox1, strText, nType)
            ));
        }

        public void WriteLog(RichTextBox logRichTxt, string strText, int nType)
        {
            Color color;
            if (nType == 0)
            {
                color = Color.Indigo;
            }
            else if (nType == 2)
            {
                color = Color.Blue;
            }
            else //if (nType == 1)
            {
                color = Color.Red;
            }

            //大于100条自动清空
            if (logRichTxt.Lines.Length > 100)
            {
                logRichTxt.Clear();
            }

            int nLen = logRichTxt.TextLength;

            if (nLen != 0)
            {
                logRichTxt.AppendText(string.Format("\r\n{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), strText));
            }
            else
            {
                logRichTxt.AppendText(string.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), strText));
            }

            logRichTxt.Select(nLen, logRichTxt.TextLength - nLen);
            logRichTxt.SelectionColor = color;

            //滚动到下面
            logRichTxt.Select(logRichTxt.TextLength, 0);
            logRichTxt.ScrollToCaret();
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text.Equals(FindResource("Connect")))
            {
                ConnectReader();
                uart.SetTid(false);
            }
            else if (btnConnect.Text.Equals(FindResource("Disconnect")))
            {
                DisConnectReader();
            }
        }


        private void ConnectReader()
        {
            string strComPort = cmbComPort.Text;
            int nBaudrate = Convert.ToInt32(cmbBaudrate.Text);
            int power = Convert.ToInt32(numericUpDown1.Text);

            //int nRet = reader.OpenCom(strComPort, nBaudrate, out strException);
            string msg = uart.Connect(strComPort, nBaudrate, new List<int> { power });
            if (!string.IsNullOrEmpty(msg))
            {
                string strLog = string.Format("{0} {1}", FindResource("tipConnectFailedCause"), msg);
                WriteLog(richTextBox1, strLog, 1);
                return;
            }
            else
            {
                tabCtrMain.TabPages[1].Enabled = true;
                string strLog = string.Format("{0} {1}@{2}", FindResource("tipConnect"), strComPort, nBaudrate);
                WriteLog(richTextBox1, strLog, 0);
                btnConnect.Text = FindResource("Disconnect");
            }
        }


        private void DisConnectReader()
        {
            try
            {
                uart.Disconnect();
                string strLog = string.Format("{0} {1}", FindResource("tipConnect"), "设备断开");
                WriteLog(richTextBox1, strLog, 0);
                btnConnect.Text = FindResource("Connect");
                tabCtrMain.TabPages[1].Enabled = false;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                string strLog = string.Format("{0} {1}", FindResource("tipConnectFailedCause"), msg);
                WriteLog(richTextBox1, strLog, 1);

            }
        }

        private void btnResetReader_Click(object sender, EventArgs e)
        {
            int nRet = uart.Reset();
            if (nRet != 0)
            {
                string strLog = string.Format("{0} Failed", FindResource("tipResetReader"));
                WriteLog(richTextBox1, strLog, 1);
            }
            else
            {
                string strLog = string.Format("{0}", FindResource("tipResetReader"));
                WriteLog(richTextBox1, strLog, 0);
            }
        }


        private void btnGetFirmwareVersion_Click(object sender, EventArgs e)
        {
            uart.GetFirmwareVersion();
        }

        private void btnGetReaderTemperature_Click(object sender, EventArgs e)
        {
            uart.GetTemperature();
        }

        private void btn_refresh_comports_Click(object sender, EventArgs e)
        {
            RefreshComPorts();
        }

        private void btnInventory_Click(object sender, EventArgs e)
        {
            if (btnInventory.Text.Equals(FindResource("StartInventory")))
            {
                //1.实时盘存 ,需要设置工作天线
                //uart.StartInventory(1);
                //2.多天线轮询开启天线1和天线2 轮询
                uart.StartInventoryFast(new List<int> { 1, 1, 1, 1 });
                btnInventory.Text = "停止盘存";
                uart.SetTid(true);
            }
            else if (btnInventory.Text.Equals(FindResource("StopInventory")))
            {
                StopInventory();
            }
        }

        private void StopInventory()
        {
            if (btnInventory.Text.Equals(FindResource("StopInventory")))
            {
                uart.StopInventory();
                btnInventory.Text = "开始盘存";
            }
        }

        private void btnGetPower_Click(object sender, EventArgs e)
        {
            uart.GetOutputPower();
        }

        private void btnSetPower_Click(object sender, EventArgs e)
        {
            if (txtPower.Text.Trim() == "")
            {
                MessageBox.Show("设置功率不可为空", "系统提示");
                return;
            }

            var power = Convert.ToInt32(txtPower.Text);
            if (power < 18 || power > 33)
            {
                MessageBox.Show("功率范围只能在18-26之间", "系统提示");
                return;
            }
            uart.SetOutputPower(new List<int>() { power, power, power, power });
            //uart.SetOutputPower(power);
        }

        private void btnGetFrequency_Click(object sender, EventArgs e)
        {
            uart.GetFrequencyRegion();
        }

        private void btnSetFrequency_Click(object sender, EventArgs e)
        {
            if (txtStartFreq.Text.Trim() == ""
                || txtFreqInterval.Text.Trim() == ""
                || txtFreqQuantity.Text.Trim() == "")
            {
                MessageBox.Show("参数不可为空", "系统提示");
                return;
            }

            int nStartFrequency = Convert.ToInt32(txtStartFreq.Text);
            int nFrequencyInterval = Convert.ToInt32(txtFreqInterval.Text);
            int channelQuantity = Convert.ToInt32(txtFreqQuantity.Text);

            uart.SetUserDefineFrequency(nStartFrequency, nFrequencyInterval, channelQuantity);
        }


        private void GenerateColmnsDataGridForInv()
        {
            dgvInventoryTagResults.AutoGenerateColumns = false;
            dgvInventoryTagResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvInventoryTagResults.BackgroundColor = Color.White;

            SerialNumber_fast_inv.DataPropertyName = "SerialNumber";
            SerialNumber_fast_inv.HeaderText = "#";
            SerialNumber_fast_inv.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            PC_fast_inv.DataPropertyName = "PC";
            PC_fast_inv.HeaderText = FindResource("PC");

            EPC_fast_inv.DataPropertyName = "EPC";
            EPC_fast_inv.HeaderText = FindResource("EPC");
            EPC_fast_inv.MinimumWidth = 200;

            TID_fast_inv.DataPropertyName = "TID";
            TID_fast_inv.HeaderText = "TID";
            TID_fast_inv.MinimumWidth = 100;

            ReadCount_fast_inv.DataPropertyName = "ReadCount";
            ReadCount_fast_inv.HeaderText = FindResource("ReadCount");
            ReadCount_fast_inv.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            Rssi_fast_inv.DataPropertyName = "Rssi";
            Rssi_fast_inv.HeaderText = FindResource("Rssi");
            Rssi_fast_inv.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            Freq_fast_inv.DataPropertyName = "Freq";
            Freq_fast_inv.HeaderText = FindResource("Freq");
            Freq_fast_inv.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;


            Antenna_fast_inv.DataPropertyName = "Antenna";
            Antenna_fast_inv.HeaderText = FindResource("Antenna");
            Antenna_fast_inv.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

        }

        //save tag as excel
        public DataTable ListViewToDataTable(ListView listView)
        {
            DataTable table = new DataTable();

            foreach (ColumnHeader header in listView.Columns)
            {
                table.Columns.Add(header.Text, typeof(string));
            }

            foreach (ListViewItem item in listView.Items)
            {
                DataRow row = table.NewRow();
                for (int i = 0; i < item.SubItems.Count; i++)
                {
                    //MessageBox.Show(item.SubItems[i].Text);
                    row[i] = item.SubItems[i].Text;
                }

                table.Rows.Add(row);
            }
            return table;
        }

        public void refresh()
        {
            lock (dgvInventoryTagResults)
            {
                dgvInventoryTagResults.Rows.Clear();
                epcList.Clear();
            }

            readTotal = 0;
            lblRate.Text = "0";
            lblCnt.Text = "0";
            lblExecTime.Text = "0";
            txtCmdTagCount.Text = "0";
        }

        private void buttonFastFresh_Click(object sender, EventArgs e)
        {
            refresh();
        }

        private void tabCtrMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabCtrMain.SelectedIndex == 0)
            {
                StopInventory();
            }
        }

        private void btnClearText_Click(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                SetTextBox(c);
            }
        }

        private void SetTextBox(Control subControl)
        {
            if (subControl.Controls.Count > 0)
            {
                foreach (Control c1 in subControl.Controls)
                {
                    SetTextBox(c1);
                }
            }
            else
            {
                TextBox tb = subControl as TextBox;
                if (tb != null)
                {
                    if (tb.GetType().Name == "UpDownEdit") return;
                    tb.Text = "";
                }
            }
        }

        private void btnSetBeeperMode_Click(object sender, EventArgs e)
        {
            byte btBeeperMode = 0xFF;

            if (rdbBeeperModeSlient.Checked)
            {
                btBeeperMode = 0;
            }
            else if (rdbBeeperModeInventory.Checked)
            {
                btBeeperMode = 1;
            }
            else if (rdbBeeperModeTag.Checked)
            {
                btBeeperMode = 2;
            }
            else
            {
                return;
            }
            uart.SetBeeperMode(btBeeperMode);
        }

        private List<string> GetColumnData(DataGridView dgv, string columnName)
        {
            List<string> columnData = new List<string>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells[columnName].Value != null)
                {
                    columnData.Add(row.Cells[columnName].Value.ToString());
                }
                else
                {
                    columnData.Add(string.Empty); // 或者你可以添加null，取决于你的需求
                }
            }

            return columnData;
        }

        private List<RFIDModel> GetColumnData(DataGridView dgv, string columnName, string tid)
        {
            List<RFIDModel> columnData = new List<RFIDModel>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells[columnName].Value != null)
                {
                    columnData.Add(new RFIDModel()
                    {
                        RFID = row.Cells[columnName].Value.ToString(),
                        Sequence = row.Cells[tid].Value.ToString()
                    });

                }
                else
                {
                    columnData.Add(new RFIDModel()); // 或者你可以添加null，取决于你的需求
                }
            }

            return columnData;
        }

        private void SendData()
        {
            var data = GetColumnData(dgvInventoryTagResults, "EPC_fast_inv", "TID_fast_inv");
            //IEnumerable<string> trimmedData = data.Select(item => item.Replace(" ", ""));
            //string msg = string.Join(",", trimmedData).TrimEnd(',');

            RequestModel request = new RequestModel();
            request.RFIDStr = string.Join(",", data.Select(item => item.RFID + ":" + item.Sequence)).TrimEnd(',');
            //request.RFIDInfo = data;
            string msg = JsonConvert.SerializeObject(data);

            if (!string.IsNullOrEmpty(msg) && data.Count > 0 && !string.IsNullOrEmpty(data[0].RFID))
            {
                //执行发送
                SendToClients(request.RFIDStr);
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            SendData();
        }

        private void SendToClients(string message)
        {
            _clients = Echo.GetClients();
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client.IsAlive) // 检查客户端是否仍然连接
                    {
                        client.Send("{\"arguments\":[\"" + message + "\"],\"target\":\"echo\",\"type\":1}" + "\u001e");
                        //client.Send("{\"arguments\":" + message + ",\"target\":\"echo\",\"type\":1}" + "\u001e");
                    }
                }
            }
        }
    }


}