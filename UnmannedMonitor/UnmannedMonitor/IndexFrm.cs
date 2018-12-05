using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Factorys;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace UnmannedMonitor
{
    public partial class IndexFrm : Form
    {
        /// <summary>
        /// 串口数据对象
        /// </summary>
        private CommonHelper commonHelper = new CommonHelper();

        /// <summary>
        /// 解析串口数据存储对象
        /// </summary>
        private List<UnmannedData> ulist = new List<UnmannedData>();

        private string sendStartCmd = " runtst -1";
        private string sendStopCmd = " stptst";

        public IndexFrm()
        {
            InitializeComponent();
            InitComBoxPortName();
        }

        private void IndexFrm_Load(object sender, EventArgs e)
        {
            txtFilePath.Text = StringUtil.ReadIniData("DataFile", "path");
            InitSerial();
            bindList();
        }


        private SerialPort serialPort = new SerialPort();
        public void InitSerial()
        {
            serialPort.StopBits = StopBits.One;
            serialPort.BaudRate = 1000000;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
        }

        public void SetSerialPortName(string portName)
        {
            if (!serialPort.IsOpen)
            {
                serialPort.PortName = portName;
            }
        }

        public void OpenOrSerialPort()
        {
            
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            } else
            {
                serialPort.Close();
            }
        }

        const int WM_SYSCOMMAND = 0x112;
        const int SC_CLOSE = 0xF060;
        const int SC_MINIMIZE = 0xF020;
        const int SC_MAXIMIZE = 0xF030;
        protected override void WndProc(ref Message m)
        {
            
            base.WndProc(ref m);
            if (m.Msg == WM_SYSCOMMAND)
            {
                //点击窗体最大化
                if (m.WParam.ToInt32() == SC_MAXIMIZE || m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    //重新绘制坐标系
                    pictureBox1.Image = drawXY(maxM, pictureBox1);
                    pictureBox2.Image = drawXY(maxM, pictureBox2);
                }
            }
        }

        /// <summary>
        /// 初始化串口选择Combox
        /// </summary>
        private void InitComBoxPortName()
        {
            string[] comDevices = GetCurrentComDevices();
            if (comDevices != null)
            {
                foreach(string str in comDevices)
                {
                    comboBoxPortSelect.Items.Add(str);   
                }
            }
        }

        private string _data = "";
        /// <summary>
        /// 接收串口数据
        /// </summary>
        /// <param name="data"></param>
        private void CommonHelper_getResultEvent(string data)
        {
            ulist.Clear();
            if (!isStart) return; 
            if (checkBox2.Checked)
            {
                //存储txt内容
                StringUtil.WriteLog(data, txtFilePath.Text);
                //存储CSV数据,并处理
                Dictionary<string, List<UnmannedData>> listNameData = StringUtil.MultipleDataSegmentation(data);
                foreach (KeyValuePair<string, List<UnmannedData>> pair in listNameData)
                {
                    foreach (UnmannedData u in pair.Value)
                    {
                        ulist.Add(u);
                        if (u.DataType.Equals("BK")) continue;
                        //StringUtil.WriteCSV(u, txtFilePath.Text);       
                    }
                    //DataSort();
                    SetDgvDataSourceAk(ulist);
                    //bindList();
                    if (checkBox1.Checked)
                    {
                        SetDgvDataSourceBk(ulist);
                    }
                    ulist.Clear();
                }  
            }
        }

        private Boolean isFirstWritesCsv = true;

        private void Comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!serialPort.IsOpen) return;
                //if (!isReveiveData) return;
                if (serialPort.IsOpen)
                {
                    //string datas = serialPort.ReadLine();
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, buffer.Length);
                    string tempResult = StringUtil.ByteToHex(buffer);
                    string data = Encoding.Default.GetString(buffer);
                   
                    //对返回的数据进行解析
                    //runst-1

                    //Command Error...
                    //#>
                    //if (data.Contains("Command Error..."))
                    //{
                    //    WriteDataToSerial(sendStartCmd,0);
                    //    return;
                    //}

                    ulist.Clear();
                    if (!isStart) return;
                    if (checkBox2.Checked)
                    {
                        //存储txt内容
                        StringUtil.WriteLog(data, txtFilePath.Text);
                    }
                    //存储CSV数据,并处理
                    Dictionary<string, List<UnmannedData>> listNameData = StringUtil.MultipleDataSegmentation(data);
                    foreach (KeyValuePair<string, List<UnmannedData>> pair in listNameData)
                    {
                        foreach (UnmannedData u in pair.Value)
                        {
                            ulist.Add(u);
                            if (u.DataType.Equals("BK")) continue;
                            if (checkBox2.Checked)
                            {
                                StringUtil.WriteCSV(u, txtFilePath.Text, isFirstWritesCsv);
                                isFirstWritesCsv = false;
                            }
                        }
                        DataSort();
                        SetDgvDataSourceAk(ulist);
                        //bindList();
                        if (checkBox1.Checked)
                        {
                            SetDgvDataSourceBk(ulist);
                        }

                        loadPoint();
                        ulist.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            } 
        }

        private int distance = 2;

        private double maxM = 0;

        private void IndexFrm_Shown(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = distance;
        }


        private int xLength = 0;//X轴的长度 
        private int yLength = 0;//Y轴长度
        private int xPerLength = 0;//X轴每份的长度
        private int yPerLength = 0;//y轴每份的长度
        private int xCenterPoint = 0;//x轴中心开始点位置
        private int yCenterPoint = 0;//y轴开始点位置
        /// <summary>
        /// 画坐标轴
        /// </summary>
        private Bitmap drawXY(double distance, Control control)
        {
            int startPoint = 30;
            int x = control.Width - startPoint;
            int y = control.Height - startPoint;

            Bitmap bitmap = new Bitmap(control.Width, control.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            Point px1 = new Point(startPoint, y);
            Point px2 = new Point(x, y);
            g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);//绘制X轴

            Point py1 = new Point(startPoint, 0);
            Point py2 = new Point(startPoint, y);
            g.DrawLine(new Pen(Brushes.Black, 2), py1, py2); //绘制Y轴

            xLength = x - startPoint;
            yLength = y;

            //X轴分割为4份
            //Y轴分割为7份
            xPerLength = xLength / 4;
            yPerLength = yLength / 7;

            xCenterPoint = startPoint + xPerLength * 2;
            yCenterPoint = y;

            int pictureBox1XValue = -50;
            double num = getNumMultiple(distance);
            double useNum = -2 * num; 
            //绘制X轴相关信息
            for (int i=0;i < 5; i++)
            {
                //画X轴刻度值
                if (control.Name == "pictureBox1")
                {
                    g.DrawString(Convert.ToString(pictureBox1XValue + (i * 25)), new Font("宋体", 10), Brushes.Black, new PointF(startPoint + (i * xPerLength) - 10, y + 6));
                } else
                {
                    g.DrawString((useNum + i * num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(startPoint + (i * xPerLength) - 10, y + 6));
                }
                //绘制X轴刻度
                g.DrawLine(new Pen(Brushes.Black, 2), new Point(startPoint + (i * xPerLength), y), new Point(startPoint + (i * xPerLength), y + 6));
                //绘制竖向的网格线
                g.DrawLine(new Pen(Brushes.Gainsboro, 1), new Point(startPoint + (i * xPerLength), 0), new Point(startPoint + (i * xPerLength), y));
            }

            
            //绘制Y轴相关信息
            for ( int i=0; i < 8; i++)
            {   
                if (i == 0) continue;
                //绘制Y轴刻度
                g.DrawLine(new Pen(Brushes.Black,2), new Point(startPoint,y - (i*yPerLength)),new Point(startPoint - 6, y - (i * yPerLength)));
                //绘制横向的网格线
                g.DrawLine(new Pen(Brushes.Gainsboro, 1), new Point(startPoint, y - (i * yPerLength)), new Point(x, y - (i * yPerLength)));
                //画Y轴数值
                g.DrawString(((i) * num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(5, y - (i * yPerLength) - 5));
            }

            if (control.Name == "pictureBox1")
            {

                //画单位
                g.DrawString("V", new Font("宋体", 10), Brushes.Red, new PointF(x + 5, y));
                g.DrawString("m/s", new Font("宋体", 8), Brushes.Red, new PointF(x + 10, y + 7));

                g.DrawString("R", new Font("宋体", 10), Brushes.Red, new PointF(3, 0));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(8, 7));
            }
            else
            {
                //画辅助线
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(startPoint, 30), new Point(startPoint + xPerLength * 2, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(startPoint + xPerLength, 0), new Point(startPoint + xPerLength * 2, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(startPoint + xPerLength * 3, 0), new Point(startPoint + xPerLength * 2, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(startPoint + xPerLength * 4, 30), new Point(startPoint + xPerLength * 2, y));

                //画虚线
                //均分为5份，6条线
                int dashedLinesDistance = (xPerLength * 2) / 5;
                Pen pen = new Pen(Color.Gray, 2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                pen.DashPattern = new float[] { 6, 6 };
                for (int i = 0; i < 6;i++)
                {
                    g.DrawLine(pen, new Point((startPoint + xPerLength) + i* dashedLinesDistance, 0), new Point(startPoint + xPerLength + i * dashedLinesDistance, y));
                }

                //画单位
                g.DrawString("X", new Font("宋体", 10), Brushes.Red, new PointF(x + 15, y));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(x + 20, y + 7));

                g.DrawString("Y", new Font("宋体", 10), Brushes.Red, new PointF(3, 0));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(8, 7));
            }
            g.Save();
            return bitmap;
        }   
        
        /// <summary>
        /// 获取倍数
        /// </summary>
        /// <returns></returns>
        private double getNumMultiple(double distance)
        {
            double num = 0;
            switch ((int)distance)
            {
                case 15: num = 2; break;
                case 30: num = 5; break;
                case 70: num = 10; break;
                case 100: num = 15; break;
                case 150: num = 20; break;
                case 200: num = 25; break;
                case 250: num = 33; break;
            }
            return num;
        }   

        /// <summary>
        /// 根据距离R 与 速度 进行数据排序
        /// </summary>
        /// <param name="data"></param>
        private void DataSort()
        {
            if(ulist != null && ulist.Count > 1)
            {
                ulist.Sort((a, b) => a.R.CompareTo(b.R));
                //ulist.Sort((a, b) => a.V.CompareTo(b.V));
            }
        }

        delegate void DelegateAk(List<UnmannedData> table);
        private void SetDgvDataSourceAk(List<UnmannedData> table)
        {

            dataGridView1.ClearSelection();
            if (dataGridView1.InvokeRequired)
            {
                BeginInvoke(new DelegateAk(SetDgvDataSourceAk), new object[] { table.Where(d => d.DataType == "AK").ToList() });
            }
            else
            {
                dataGridView1.DataSource = table.Where(d => d.DataType == "AK").ToList();
                dataGridView1.Refresh();
            }

            foreach (DataGridViewColumn item in dataGridView2.Columns)
            {
                //item.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (item.Name == "DataType" || item.Name == "FrameState" || item.Name == "SysFrameNo")
                {
                    item.Visible = false;
                }
            }
        }

        delegate void DelegateBk(List<UnmannedData> table);
        private void SetDgvDataSourceBk(List<UnmannedData> table)
        {
            dataGridView2.ClearSelection();
            if (dataGridView2.InvokeRequired)
            {
                BeginInvoke(new DelegateBk(SetDgvDataSourceBk), new object[] { table.Where(d => d.DataType == "BK").ToList() });
            }
            else
            {
                dataGridView2.DataSource = table.Where(d => d.DataType == "BK").ToList();
                dataGridView2.Refresh();

            }
            foreach (DataGridViewColumn item in dataGridView2.Columns)
            {
                //item.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (item.Name == "DataType" || item.Name == "FrameState" || item.Name == "SysFrameNo")
                {
                    item.Visible = false;
                }
            }
        }

        //private List<>
        /// <summary>
        /// 绑定列表
        /// </summary>
        private void bindList()
        {
            this.Invoke(new EventHandler(delegate
            {
                dataGridView1.DataSource = ulist.Where(d => d.DataType == "AK").ToList();
            }));

            foreach (DataGridViewColumn item in dataGridView1.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (item.Name == "DataType" || item.Name == "FrameState" || item.Name == "SysFrameNo")
                {
                    item.Visible = false;
                }
            }
            bindBkList();
            //ulist.Clear();
        }

        private void bindBkList()
        {
            if (checkBox1.Checked)
            {
                this.Invoke(new EventHandler(delegate
                {
                    dataGridView2.DataSource = ulist.Where(p => p.DataType == "BK").ToList();
                }));
                foreach (DataGridViewColumn item in dataGridView2.Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;
                    if (item.Name == "DataType" || item.Name == "FrameState" || item.Name == "SysFrameNo")
                    {
                        item.Visible = false;
                    }
                }
            }
        }

        public string[] GetCurrentComDevices()
        {
            string[] ports = SerialPort.GetPortNames();
            return ports;
        }

        private PointF p = new PointF(0, 0);
        private Boolean isStart = false;
        private Boolean isReveiveData = false;
        private void btnStart_Click(object sender, EventArgs e)
        {
            //TestOpenProtGetData();
            string selectPort = comboBoxPortSelect.Text.ToString();
            if (string.IsNullOrEmpty(comboBoxPortSelect.Text.ToString()))
            {
                MessageBox.Show("请先选择需要打开的串口！");
                return;
            }
            SetSerialPortName(selectPort);
            Button btn = sender as Button;
            if (!isStart)
            {
                
                isStart = true;
                btn.Text = "Stop";
                OpenOrSerialPort();
                serialPort.DataReceived += new SerialDataReceivedEventHandler(Comm_DataReceived);
                isUpdatePictureBox = true;
                //selectPort.Write() 
                //WriteDataToSerial(sendStartCmd,0);
            }
            else
            {
                isStart = false;
                isUpdatePictureBox = false;
                btn.Text = "Start";
                //WriteDataToSerial(sendStopCmd,1);
                OpenOrSerialPort();
                //commonHelper.SendData("stptst");
                //commonHelper.SetIsReceiveData(false);
                //commonHelper.CloseSerial();
                //StopDeviceData();
            }
        }

        /// <summary>
        /// 写数据到串口
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type">0:开启发送数据 1：关闭发送数据</param>
        private void WriteDataToSerial(string str ,int type)
        {
            if(serialPort != null && serialPort.IsOpen)
            {
               
                //string strHex = StringUtil.StringToHexString(str);
                //byte[] strBytes = StringUtil.strToHexByte(strHex);
                //serialPort.Write(strBytes,0,strBytes.Length);
                //char[] c = str.ToCharArray();
                //serialPort.Write(c, 0, c.Count());
                //serialPort.Write("run ts t - 1");
                serialPort.Write(str);
                //serialPort.WriteLine("runn");
                if (type == 0)
                {
                    isReveiveData = true;
                }
                else
                {
                    isReveiveData = false;
                }
            }
        }

        //存储每次绘制的矩形图形
        private List<PointF> pointFList = new List<PointF>();//pictureBox2
        private List<PointF> pointSList = new List<PointF>();//pictureBox1
        void loadPoint()
        {
            p.X = xCenterPoint;
            p.Y = yCenterPoint;
            if (isUpdatePictureBox)
            {
               
                for (int i = 0; i < ulist.Count; i++)
                {
                    double r = ulist[i].R;
                    double a = ulist[i].A;
                    double v = ulist[i].V;
                    if (r > distanceValue) continue;
                    if (ulist[i].DataType.Equals("AK"))
                    {
                        PointF pointFEx = getNewPointEx(a, r);
                        PointF pointF = getNewPoint(a, r);
                        PointF pointS = getNewSpeedPoint(r, v);
                        pointFList.Add(pointF);
                        pointSList.Add(pointS);

                        Brush brush = null;

                        if (v < 0)
                        {
                            brush = Brushes.Green;
                        }
                        else if (v == 0)
                        {
                            brush = Brushes.Yellow;
                        }
                        else if (v > 0)
                        {
                            brush = Brushes.Red;
                        }
                        drawRectangle(pictureBox2, pointF, brush, GetRectSize(distanceValue));
                        drawRectangle(pictureBox1, pointS, brush, GetRectSize(distanceValue));
                        drawCoordinatePoints(pictureBox2, pointF, r, pointFEx.X, 0);
                        drawCoordinatePoints(pictureBox1, pointS, r, v, 1);
                    }
                }


                if (pointFList.Count > 0)
                {
                    cleaDrawRectangle(pictureBox2);
                }

                if (pointSList.Count > 0)
                {
                    cleaDrawRectangle(pictureBox1);
                }
                pointFList.Clear();
                pointSList.Clear();
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// 根据距离的远近，设置方块的大小
        /// 1、距离越远,方块越大
        /// 2、距离越近,方块越小
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Size GetRectSize(double distance)
        {
            Size size = new Size();
            int num = 0;
            switch ((int)distance)
            {
                case 15: num = 30; break;//30、30
                case 30: num = 20; break;//20、20
                case 70: num = 15; break;//10、10
                case 100: num = 10; break;//7、7
                case 150: num = 8; break;//5、5
                case 200: num = 5; break;//3、3
                case 250: num = 3; break;//1、1
            }
            size.Width = num;
            size.Height = num;
            return size;
        }

        /// <summary>
        /// 距离转换
        /// </summary>
        /// <returns></returns>
        private double changeDistance(int d)
        {
            //15m:29倍  30m:11倍  60m:5.7倍   100m:3.6倍   150m:2.3倍
            double db = 0.0;
            switch (d)
            {
                case 0:
                    db = 29;
                    break;
                case 1:
                    db = 11;
                    break;
                case 2:
                    db = 5.7;
                    break;
                case 3:
                    db = 3.6;
                    break;
                case 4:
                    db = 2.3;
                    break;
            }
            return db;
        }

        private double changeDistanceNew(double d, int length)
        {
            //15m:29倍  30m:11倍  60m:5.7倍   100m:3.6倍   150m:2.3倍
            double db = 0.0;
            switch ((int)d)
            {
                case 15:
                    db = length / 2;
                    break;
                case 30:
                    db = length / 5;
                    break;
                case 70:
                    db = length / 10;
                    break;
                case 100:
                    db = length / 15;
                    break;
                case 150:
                    db = length / 20;
                    break;
                case 200:
                    db = length / 25;
                    break;
                case 250:
                    db = length / 33;
                    break;
            }
            return db;
        }

        /// <summary>
        /// 新坐标点
        /// </summary>
        /// <param name="pointB">起点坐标</param>
        /// <param name="angle">角度</param>
        /// <param name="bevel">距离</param>
        /// <returns></returns>
        private PointF getNewPoint(double angle, double bevel)
        {
            PointF p = getNewPointEx(angle,bevel);
            p.X = p.X * (float)changeDistanceNew(distanceValue,xPerLength);
            p.Y = p.Y * (float)changeDistanceNew(distanceValue, yPerLength);
            return new PointF(xCenterPoint + p.X, yCenterPoint - p.Y);
        }

        /// <summary>
        /// 计算x,y需要显示的坐标点
        /// </summary>
        /// <param name="angle">角度</param>
        /// <param name="bevel">等级</param>
        /// <returns></returns>
        private PointF getNewPointEx(double angle, double bevel)
        {
            //在Flash中顺时针角度为正，逆时针角度为负
            //换算过程中先将角度转为弧度
            var radian = angle * Math.PI / 180;
            var yMargin = float.Parse((Math.Cos(radian) * bevel).ToString());
            var xMargin = -float.Parse((Math.Sin(radian) * bevel ).ToString());//备注
            return new PointF((float)xMargin,(float)yMargin);
        }

        private PointF getNewSpeedPoint(double r, double v)
        {
            var yMargin = (float)r * (float)changeDistanceNew(distanceValue, yPerLength);
            var xMargin = (float)v * (float)changeDistanceNew(200, xPerLength);
            PointF p = new PointF();
            p.X = xCenterPoint + xMargin;  
            p.Y = yCenterPoint - yMargin;
            return p;
        }

      
        private Boolean isUpdatePictureBox = false;
        

        /// <summary>
        /// 根据坐标点画矩形
        /// </summary>
        /// <param name="control"></param>
        /// <param name="pointF"></param>
        /// <param name="brush"></param>
        private void drawRectangle(Control control, PointF pointF, Brush brush, Size size)
        {   
            Graphics g = control.CreateGraphics();
            //pointF.Y -= size.Height;
            //pointF.X -= size.Width;
            g.FillRectangle(brush, new RectangleF(pointF, size));//new Size(10, 20)
            g.Dispose();
        }

        private void drawCoordinatePoints(Control control, PointF pointF,double r, double v,int type)
        {
            Graphics g = control.CreateGraphics();
            pointF.Y -= GetRectSize(distanceValue).Height;
            pointF.X += GetRectSize(distanceValue).Width;
            double x = Math.Round(v, 2);
            double y = Math.Round(r, 2);
            g.DrawString("(" + Math.Round(v, 2) + "," + Math.Round(r, 2) + ")", new Font("宋体", 8), Brushes.Blue, pointF);
            g.Dispose();
        }

        /// <summary>
        /// 清除绘制的矩形图案
        /// </summary>
        /// <param name="control"></param>
        private void cleaDrawRectangle(Control control)
        {
            if (isUpdatePictureBox)
            {
                control.Invalidate();
            }
        }
        private double distanceValue = 0;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            distance = comboBox1.SelectedIndex;

            double db = double.Parse(comboBox1.SelectedItem.ToString().Substring(0, comboBox1.SelectedItem.ToString().Length - 1));
            distanceValue = db;
            pictureBox1.Image = drawXY(db, pictureBox1);
            pictureBox2.Image = drawXY(db, pictureBox2);
            maxM = db;

            //雷达位置
            //int ypaddings = 70;
            //int startPoint = 20;
            //int y = this.panel2.Height - startPoint;
            //p = new PointF((ypaddings * 2) + 16, y - startPoint);

            //drawRectangle(panel2, p, Brushes.Blue);
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            txtFilePath.Text = path.SelectedPath + "\\";

            StringUtil.Writue("DataFile", "path", path.SelectedPath + "\\");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                bindList();
            }
            else
            {
                dataGridView2.DataSource = null;
            }
        }

        private void btnCamera_Click(object sender, EventArgs e)
        {
            CameraFrm cameraFrm = new CameraFrm();
            cameraFrm.Show();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                if (string.IsNullOrEmpty(txtFilePath.Text))
                {
                    MessageBox.Show("请选择或输入文件存储目录！");
                }
                ////检测是否输入文件路径
                //if (!string.IsNullOrEmpty(txtFilePath.Text))
                //{
                //    Regex regex = new Regex(@"^([a-zA-Z]:\\)?[^\/\:\*\?\""\<\>\|\,]*$");
                //    Match m = regex.Match(txtFilePath.Text);
                //    if (!m.Success)
                //    {
                //        MessageBox.Show("非法的文件保存路径，请重新选择或输入！");
                //        return;
                //    }
                //    regex = new Regex(@"^[^\/\:\*\?\""\<\>\|\,]+$");
                //    m = regex.Match(txtFilePath.Text);
                //    if (!m.Success)
                //    {
                //        MessageBox.Show("请勿在文件名中包含\\ / : * ？ \" < > |等字符，请重新输入有效文件名！");
                //        return;
                //    }
                //}
                //else
                //{
                //    MessageBox.Show("请选择或输入文件存储目录！");
                //}
            }  
        }
    }
}
