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

        /// <summary>
        /// 画坐标轴
        /// </summary>
        private Bitmap drawXY(double distance, Control control)
        {
            int xpaddings = 60;
            int ypaddings = 70;
            int startPoint = 30;
            int x = control.Width - startPoint;
            int y = control.Height - startPoint;


            Bitmap bitmap = new Bitmap(control.Width, control.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            Point px1 = new Point(startPoint, y);
            Point px2 = new Point(x + 20, y);
            g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);//绘制X轴
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

            for (int i = 1; i < 8; i++)
            {
                double temp = i * num;
                double temp1 = y - (i * xpaddings);
                //double 
                //画数值
                g.DrawString((i * num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(0, y - (i * xpaddings) - 6));
                //画刻度
                g.DrawLine(new Pen(Brushes.Black, 2), new Point(26, y - (i * xpaddings)), new Point(startPoint, y - (i * xpaddings)));
                //画表格线
                g.DrawLine(new Pen(Brushes.Gainsboro, 1), new Point(startPoint, y - (i * xpaddings)), new Point(x, y - (i * xpaddings)));
            }

            Point py1 = new Point(startPoint, 0);
            Point py2 = new Point(startPoint, y);
            g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);

            //测试，坐标系
            Point py3 = new Point(0, 0);
            Point py4 = new Point(control.Width, 0);
            g.DrawLine(new Pen(Brushes.Black, 2), py3, py4);

            //速度和角度
            if (control.Name == "pictureBox1")
            {
                //画数值
                g.DrawString("-50", new Font("宋体", 10), Brushes.Black, new PointF(10, y + 7));
                g.DrawString("-25", new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 1) + 15, y + 7));
                g.DrawString("0", new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 2) + 26, y + 7));
                g.DrawString("25", new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 3 + 22, y + 7));
                g.DrawString("50", new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 4 + 22, y + 7));

                //画单位
                g.DrawString("V", new Font("宋体", 10), Brushes.Red, new PointF(x + 5, y));
                g.DrawString("m/s", new Font("宋体", 8), Brushes.Red, new PointF(x + 10, y + 7));

                g.DrawString("R", new Font("宋体", 10), Brushes.Red, new PointF(3, 0));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(8, 7));
            }
            else
            {
                //距离和角度
                //画数值
                g.DrawString((-num * 2).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(15, y + 7));
                g.DrawString((-num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 1) + 15, y + 7));
                g.DrawString("0", new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 2) + 26, y + 7));
                g.DrawString((num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 3 + 22, y + 7));
                g.DrawString((num * 2).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 4 + 22, y + 7));

                //画辅助线
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(0, 30), new Point((ypaddings * 2) + startPoint, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(100, 0), new Point((ypaddings * 2) + startPoint, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(240, 0), new Point((ypaddings * 2) + startPoint, y));
                g.DrawLine(new Pen(Brushes.Yellow, 1), new Point(x, 30), new Point((ypaddings * 2) + startPoint, y));

                //画虚线
                Pen pen = new Pen(Color.Gray, 2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                pen.DashPattern = new float[] { 6, 6 };
                int temp = 0;
                for (int i = 1; i < 7;i++)
                {
                    g.DrawLine(pen, new Point(105 + temp, 0), new Point(105 + temp, y));
                    temp = i * 26;
                }


                //画单位
                g.DrawString("X", new Font("宋体", 10), Brushes.Red, new PointF(x + 15, y));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(x + 20, y + 7));

                g.DrawString("Y", new Font("宋体", 10), Brushes.Red, new PointF(3, 0));
                g.DrawString("m", new Font("宋体", 8), Brushes.Red, new PointF(8, 7));
            }
            for (int i = 1; i < 5; i++)
            {
                //画表格线
                g.DrawLine(new Pen(Brushes.Gainsboro, 1), new Point((i * ypaddings) + startPoint, 0), new Point(startPoint + (i * ypaddings), y));
                //画刻度
                g.DrawLine(new Pen(Brushes.Black, 2), new Point(startPoint + 1 + (i * ypaddings), y), new Point(startPoint + 1 + (i * ypaddings), y + 7));
            }

            g.Save();

            return bitmap;
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
            var screenLocation = pictureBox1.Location;
            var screenLocation1 = pictureBox2.Location;
            var p1w = pictureBox1.Width;
            var p2w = pictureBox2.Width;
            var p1H = pictureBox1.Height;
            var p2H = pictureBox2.Height;
            p.X = (p1w - 20) / 2;
            p.Y = p1H - 20; 
            if (isUpdatePictureBox)
            {
               
                for (int i = 0; i < ulist.Count; i++)
                {
                    double r = ulist[i].R;// * changeDistance(distance); //距离 --remove
                    double a = ulist[i].A;//角度 --angle
                    double rNew = r * changeDistance(distance);
                    double v = ulist[i].V;
                    double vNew = ulist[i].V * changeDistance(distance);
                    if (r > distanceValue) continue;
                    int picture1BoxHeight = pictureBox1.Height;
                    int picture1BoxWigth = pictureBox1.Width;
                    int picture2BoxHeight = pictureBox2.Height;
                    int picture2BoxWigth = pictureBox2.Width;
                    if (ulist[i].DataType.Equals("AK"))
                    {
                        PointF pointFEx = getNewPointEx(a, rNew);
                        PointF pointF = getNewPoint(p, a, rNew);
                        PointF pointS = getNewSpeedPoint(p, rNew, v);
                        pointFList.Add(pointF);
                        pointSList.Add(pointS);
                        

                        if (v < 0)
                        {
                            drawRectangle(pictureBox2, pointF, Brushes.Green, GetRectSize(distanceValue));
                            drawRectangle(pictureBox1, pointS, Brushes.Green, GetRectSize(distanceValue));
                            drawCoordinatePoints(pictureBox2,pointF, r,pointFEx.X, 0);
                            drawCoordinatePoints(pictureBox1, pointS,r,v,1);
                            //速度为负值 用绿色 --表示靠近目标
                            //StartThreadToUpdatePictureBox(gPictureBox2, pointF, Brushes.Green);
                            //StartThreadToUpdatePictureBox(gPictureBox1, pointS, Brushes.Green);

                        }
                        else if (v == 0)
                        {
                            drawRectangle(pictureBox2, pointF, Brushes.Yellow, GetRectSize(distanceValue));
                            drawRectangle(pictureBox1, pointS, Brushes.Yellow, GetRectSize(distanceValue));
                            drawCoordinatePoints(pictureBox2, pointF, r, pointFEx.X, 0);
                            drawCoordinatePoints(pictureBox1, pointS, r, v, 1);
                            //速度为0 用黄色 ---表示目标处于静止状态
                            //StartThreadToUpdatePictureBox(gPictureBox2, pointF, Brushes.Yellow);
                            //StartThreadToUpdatePictureBox(gPictureBox1, pointS, Brushes.Yellow);
                        }
                        else if (v > 0)
                        {
                            drawRectangle(pictureBox2, pointF, Brushes.Red, GetRectSize(distanceValue));
                            drawRectangle(pictureBox1, pointS, Brushes.Red, GetRectSize(distanceValue));
                            drawCoordinatePoints(pictureBox2, pointF, r, pointFEx.X, 0);
                            drawCoordinatePoints(pictureBox1, pointS, r, v, 1);
                            //速度为正值 用红色 ---表示远离目标
                            //StartThreadToUpdatePictureBox(gPictureBox2, pointF, Brushes.Red);
                            //StartThreadToUpdatePictureBox(gPictureBox1, pointS, Brushes.Red);
                        }
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

        /// <summary>
        /// 新坐标点
        /// </summary>
        /// <param name="pointB">起点坐标</param>
        /// <param name="angle">角度</param>
        /// <param name="bevel">距离</param>
        /// <returns></returns>
        private PointF getNewPoint(PointF pointB, double angle, double bevel)
        {
            PointF p = getNewPointEx(angle,bevel);
            //return new PointF(pointB.X + xMargin + (pictureBox2.Width / 2), pictureBox2.Height - (pointB.Y + yMargin) - 20);
            //return new PointF(((pictureBox2.Width - 20) / 2) + pointB.X + xMargin, pictureBox2.Height - (pointB.Y + yMargin) - 30);
            return new PointF(pointB.X - p.X, pointB.Y - p.Y - 10);
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
            var xMargin = -float.Parse((Math.Sin(radian)).ToString());//备注
            return new PointF((float)xMargin,(float)yMargin);
        }

        private PointF getNewSpeedPoint(PointF pointB, double r, double v)
        {
           //显示Y轴的速度
           //X 轴 始终为 0
            var yMargin = (float)r;
            var xMargin = (float)v;
            PointF p = new PointF();
            p.X = pointB.X + xMargin;
            p.Y = pointB.Y - yMargin - 10;
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
            pointF.Y -= size.Height;
            g.FillRectangle(brush, new RectangleF(pointF, size));//new Size(10, 20)
            //g.DrawString((i * num).ToString(), new Font("宋体", 10), Brushes.Black, new PointF(0, y - (i * xpaddings) - 6));
            
            //brush.Dispose();
            g.Dispose();
        }

        private void drawCoordinatePoints(Control control, PointF pointF,double r, double v,int type)
        {
            Graphics g = control.CreateGraphics();
            pointF.Y -= 25;
            pointF.X += 15;
            //pointF.Y -= ;
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
