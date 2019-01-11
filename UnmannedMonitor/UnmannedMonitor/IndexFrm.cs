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
using Peak.Can.Basic;
using TPCANHandle = System.UInt16;

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
        private bool isInitPcan = false;
        public IndexFrm()
        {
            InitializeComponent();
            InitComBoxPortName();
        }

        private void IndexFrm_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            this.Resize += new System.EventHandler(this.Form_Resize);
            txtFilePath.Text = StringUtil.ReadIniData("DataFile", "path");
            InitSerial();
            bindList();
        }

        private void Form_Resize(object sender, System.EventArgs e)
        {
            // Write your code at here
            //Console.Write(e.ToString());
            //重新绘制坐标系
            pictureBox1.Image = drawXY(maxM, pictureBox1);
            pictureBox2.Image = drawXY(maxM, pictureBox2);
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
            //判断获取数据的方式
            if (rbSerialPort.Checked)
            {
                //串口获取数据逻辑
                if (!SerialGetData())
                {
                    return;
                }
            } else if (rbPcan.Checked)
            {
                //PCAN获取数据逻辑
                //1、初始化PCAN
                //2、读取数据
                //3、释放资源
                if (!isInitPcan)
                {
                    InitializeBasicComponents();
                    isInitPcan = true;
                    isUpdatePictureBox = true;
                    PCAN_Init_Click();
                }
            }

           
            Button btn = sender as Button;
            if (!isStart)
            {
                isStart = true;
                btn.Text = "Stop";     
                //selectPort.Write() 
                //WriteDataToSerial(sendStartCmd,0);
            }
            else
            {
                isStart = false;
                isUpdatePictureBox = false;
                btn.Text = "Start";
                if(rbSerialPort.Checked)
                {
                    //关闭串口
                    OpenOrSerialPort();
                }
                if(rbPcan.Checked )
                {
                    UnInitPcan();
                }
                //WriteDataToSerial(sendStopCmd,1);
                //commonHelper.SendData("stptst");
                //commonHelper.SetIsReceiveData(false);
                //commonHelper.CloseSerial();
                //StopDeviceData();
            }
        }

        /// <summary>
        /// 串口获取数据
        /// </summary>
        private bool SerialGetData()
        {
            //TestOpenProtGetData();
            string selectPort = comboBoxPortSelect.Text.ToString();
            if (string.IsNullOrEmpty(comboBoxPortSelect.Text.ToString()))
            {
                MessageBox.Show("请先选择需要打开的串口！");
                return false;
            }
            SetSerialPortName(selectPort);
            //打开串口
            OpenOrSerialPort();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(Comm_DataReceived);
            isUpdatePictureBox = true;
            return true;
        }

        /// <summary>
        /// PCAN 获取数据
        /// </summary>
        private void PcanGetData()
        {

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
                    if(ulist[i].FrameState == 0 || r > distanceValue)
                    {
                        continue;
                    }
                    //if (r > distanceValue) continue;
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
                        //pictureBox2.Image = drawRectangleNew(pictureBox2, pointF, brush, GetRectSize(distanceValue));
                        drawRectangle(pictureBox2, pointF, brush, GetRectSize(distanceValue));
                        drawRectangle(pictureBox1, pointS, brush, GetRectSize(distanceValue));
                        drawCoordinatePoints(pictureBox2, pointF, r, pointFEx.X);
                        drawCoordinatePoints(pictureBox1, pointS, r, v);
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
                Thread.Sleep(50);
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
                case 15: num = 40; break;
                case 30: num = 30; break;
                case 70: num = 20; break;
                case 100: num = 15; break;
                case 150: num = 12; break;
                case 200: num = 10; break;
                case 250: num = 5; break;
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
            pointF.Y -= size.Height / 2;
            pointF.X -= size.Width / 2;
            g.FillRectangle(brush, new RectangleF(pointF, size));
            g.Dispose();
        }

        private Bitmap drawRectangleNew(Control control, PointF pointF, Brush brush, Size size)
        {
            //定义Bitmap
            Bitmap bmp = new Bitmap(control.Width, control.Height);
            //创建位图
            Graphics gr1 = Graphics.FromImage(bmp);
            gr1.FillRectangle(brush, new RectangleF(pointF, size));
            gr1.Save();
            gr1.Dispose();
            return bmp;
        }

        /// <summary>
        /// 绘制坐标点
        /// </summary>
        /// <param name="control"></param>
        /// <param name="pointF"></param>
        /// <param name="r"></param>
        /// <param name="v"></param>
        private void drawCoordinatePoints(Control control, PointF pointF,double r, double v)
        {
            //Bitmap bitmap = new Bitmap(control.Width, control.Height);
            //Graphics g = Graphics.FromImage(bitmap);
            //g.Clear(Color.White);
            Graphics g = control.CreateGraphics();
            pointF.Y -= GetRectSize(distanceValue).Height;
            pointF.X += GetRectSize(distanceValue).Width - 5;
            g.DrawString("(" + Math.Round(v, 2) + "," + Math.Round(r, 2) + ")", new Font("宋体", 8), Brushes.Blue, pointF);
            //if (!isStart)
            //{
            //    g.Save();
            //}
            g.Dispose();
            //return bitmap; 
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

        /// <summary>
        /// Receive-Event
        /// </summary>
        private System.Threading.AutoResetEvent m_ReceiveEvent;
        /// <summary>
        /// Saves the baudrate register for a conenction
        /// </summary>
        private TPCANBaudrate m_Baudrate;
        /// <summary>
        /// Saves the type of a non-plug-and-play hardware
        /// </summary>
        private TPCANType m_HwType;

        /// <summary>
        /// Initialize of PCAN-Basic components
        /// </summary>
        private void InitializeBasicComponents()
        {

            // Creates the list for received messages
            //
            m_LastMsgsList = new System.Collections.ArrayList();
            // Creates the delegate used for message reading
            //
            m_ReadDelegate = new ReadDelegateHandler(ReadMessages);

            // Creates the event used for signalize incomming messages 
            //
            m_ReceiveEvent = new System.Threading.AutoResetEvent(false);

            m_Baudrate = TPCANBaudrate.PCAN_BAUD_500K;
            m_HwType = TPCANType.PCAN_TYPE_ISA;

            SetHandle();
        }

        private void PCAN_Init_Click()
        {
            TPCANStatus stsResult;

            // Connects a selected PCAN-Basic channel
            //
            stsResult = PCANBasic.Initialize(
                m_PcanHandle,
                m_Baudrate,
                m_HwType,
                Convert.ToUInt32("0100", 16),
                Convert.ToUInt16(3));

            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
            else
                // Prepares the PCAN-Basic's PCAN-Trace file
                //
                ConfigureTraceFile();

            // Sets the connection status of the main-form
            //
            SetConnectionStatus(stsResult == TPCANStatus.PCAN_ERROR_OK);
        }


        /// <summary>
        /// Activates/deaactivates the different controls of the main-form according
        /// with the current connection status
        /// </summary>
        /// <param name="bConnected">Current status. True if connected, false otherwise</param>
        private void SetConnectionStatus(bool bConnected)
        {
            if (bConnected)
            {
                tmrRead.Enabled = true;
            }
        }

        /// <summary>
        /// Configures the PCAN-Trace file for a PCAN-Basic Channel
        /// </summary>
        private void ConfigureTraceFile()
        {
            UInt32 iBuffer;
            TPCANStatus stsResult;

            // Configure the maximum size of a trace file to 5 megabytes
            //
            iBuffer = 5;
            stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_TRACE_SIZE, ref iBuffer, sizeof(UInt32));
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage(GetFormatedError(stsResult));

            // Configure the way how trace files are created: 
            // * Standard name is used
            // * Existing file is ovewritten, 
            // * Only one file is created.
            // * Recording stopts when the file size reaches 5 megabytes.
            //
            iBuffer = PCANBasic.TRACE_FILE_SINGLE | PCANBasic.TRACE_FILE_OVERWRITE;
            stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_TRACE_CONFIGURE, ref iBuffer, sizeof(UInt32));
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage(GetFormatedError(stsResult));
        }

        /// <summary>
        /// Saves the handle of a PCAN hardware
        /// </summary>
        private TPCANHandle m_PcanHandle;

        /// <summary>
        /// Stores the status of received messages for its display
        /// </summary>
        private System.Collections.ArrayList m_LastMsgsList;

        #region Delegates
        /// <summary>
        /// Read-Delegate Handler
        /// </summary>
        private delegate void ReadDelegateHandler();
        #endregion

        /// <summary>
        /// Read Delegate for calling the function "ReadMessages"
        /// </summary>
        private ReadDelegateHandler m_ReadDelegate;

        private void ReadMessages()
        {
            TPCANMsg CANMsg;
            TPCANTimestamp CANTimeStamp;
            TPCANStatus stsResult;
           
            do
            {
                // We execute the "Read" function of the PCANBasic
                //
                stsResult = PCANBasic.Read(m_PcanHandle, out CANMsg, out CANTimeStamp);
                
                if (stsResult != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    // We process the received message
                    //
                    ProcessMessage(CANMsg, CANTimeStamp);

                if (stsResult == TPCANStatus.PCAN_ERROR_ILLOPERATION)
                    break;
                //else
                //    // If an error occurred, an information message is included
                //    //
                //    PCANBasic.GetErrorText(stsResult, 0, strMsg);
                //    MessageBox.Show(strMsg.ToString());
                //    IncludeTextMessage(GetFormatedError(stsResult));
            }
            while ((stsResult & TPCANStatus.PCAN_ERROR_QRCVEMPTY) != TPCANStatus.PCAN_ERROR_QRCVEMPTY) ;           
        }

        private List<String> tempDataStr = new List<String>();
        private List<UnmannedData> tempUd = new List<UnmannedData>();
        /// <summary>
        /// Processes a received message, in order to show it in the Message-ListView
        /// </summary>
        /// <param name="theMsg">The received PCAN-Basic message</param>
        /// <returns>True if the message must be created, false if it must be modified</returns>
        private void ProcessMessage(TPCANMsg theMsg, TPCANTimestamp itsTimeStamp)
        {
            // We search if a message (Same ID and Type) is 
            // already received or if this is a new message
            //
            //tempDataStr.Clear();
            lock (m_LastMsgsList.SyncRoot)
            {
                foreach (MessageStatus msg in m_LastMsgsList)
                {
                    //Console.WriteLine("ID = " + msg.CANMsg.ID + "; msg.DatsTRING = " + msg.DataString + " ;msg.dataId = " +msg.IdString);

                    if (msg.IdString.Equals("500h"))
                    {
                        tempDataStr.Add(msg.DataString);
                        //=====
                    }
                    else
                    {
                        tempUd = StringUtil.AnalyticalPcanData(tempDataStr);
                        if (tempUd != null)
                        {
                            foreach (UnmannedData u in tempUd)
                            {
                                ulist.Add(u);
                            }
                            //更新UI界面--dataGrid
                            //&&&&&&&&&&&&&&&&&&&&&&&&&&&&
                            //绘制pictureBox
                            SetDgvDataSourceAk(ulist);
                            //bindList();
                            if (checkBox1.Checked)
                            {
                                SetDgvDataSourceBk(ulist);
                            }
                            loadPoint();
                            tempUd.Clear();
                            tempDataStr.Clear();
                            ulist.Clear();
                        }
                    }
                    if ((msg.CANMsg.ID == theMsg.ID) && (msg.CANMsg.MSGTYPE == theMsg.MSGTYPE))
                    {
                        // Modify the message and exit
                        //
                        msg.Update(theMsg, itsTimeStamp);
                        return;
                    }
                    //解析数据--把每一帧的数据放到一起去解析
                    
                }
                // Message not found. It will created
                //
                InsertMsgEntry(theMsg, itsTimeStamp);
            }
        }

        /// <summary>
        /// Includes a new line of text into the information Listview
        /// </summary>
        /// <param name="strMsg">Text to be included</param>
        private void IncludeTextMessage(string strMsg)
        {
            //lbxInfo.Items.Add(strMsg);
            //lbxInfo.SelectedIndex = lbxInfo.Items.Count - 1;
        }


        /// <summary>
        /// Inserts a new entry for a new message in the Message-ListView
        /// </summary>
        /// <param name="newMsg">The messasge to be inserted</param>
        /// <param name="timeStamp">The Timesamp of the new message</param>
        private void InsertMsgEntry(TPCANMsg newMsg, TPCANTimestamp timeStamp)
        {
            MessageStatus msgStsCurrentMsg;

            lock (m_LastMsgsList.SyncRoot)
            {
                // We add this status in the last message list
                //
                msgStsCurrentMsg = new MessageStatus(newMsg, timeStamp, 1);
                ////lstMessages.Items.Count);-modify                                                          
                ////解析数据                                                      
                ////解析数据--把每一帧的数据放到一起去解析
                //if (!msg.IdString.Equals("600h"))
                //{
                //    tempDataStr.Add(msg.DataString);
                //    //=====
                //}
                //else
                //{
                //    tempUd = StringUtil.AnalyticalPcanData(tempDataStr);
                //    foreach (UnmannedData u in tempUd)
                //    {
                //        ulist.Add(u);
                //    }
                //    //更新UI界面--dataGrid
                //    //&&&&&&&&&&&&&&&&&&&&&&&&&&&&
                //    //绘制pictureBox
                //    SetDgvDataSourceAk(ulist);
                //    //bindList();
                //    if (checkBox1.Checked)
                //    {
                //        SetDgvDataSourceBk(ulist);
                //    }
                //    loadPoint();
                //    tempDataStr.Clear();
                //}
                m_LastMsgsList.Add(msgStsCurrentMsg); 
            }
        }

        

        #region Structures
        /// <summary>
        /// Message Status structure used to show CAN Messages
        /// in a ListView
        /// </summary>
        private class MessageStatus
        {
            private TPCANMsg m_Msg;
            private TPCANTimestamp m_TimeStamp;
            private TPCANTimestamp m_oldTimeStamp;
            private int m_iIndex;
            private int m_Count;
            private bool m_bShowPeriod;
            private bool m_bWasChanged;

            public MessageStatus(TPCANMsg canMsg, TPCANTimestamp canTimestamp, int listIndex)
            {
                m_Msg = canMsg;
                m_TimeStamp = canTimestamp;
                m_oldTimeStamp = canTimestamp;
                m_iIndex = listIndex;
                m_Count = 1;
                m_bShowPeriod = true;
                m_bWasChanged = false;
            }

            public void Update(TPCANMsg canMsg, TPCANTimestamp canTimestamp)
            {
                m_Msg = canMsg;
                m_oldTimeStamp = m_TimeStamp;
                m_TimeStamp = canTimestamp;
                m_bWasChanged = true;
                m_Count += 1;
            }

            public TPCANMsg CANMsg
            {
                get { return m_Msg; }
            }

            public TPCANTimestamp Timestamp
            {
                get { return m_TimeStamp; }
            }

            public int Position
            {
                get { return m_iIndex; }
            }

            public string TypeString
            {
                get { return GetMsgTypeString(); }
            }

            public string IdString
            {
                get { return GetIdString(); }
            }

            public string DataString
            {
                get { return GetDataString(); }
            }

            public int Count
            {
                get { return m_Count; }
            }

            public bool ShowingPeriod
            {
                get { return m_bShowPeriod; }
                set
                {
                    if (m_bShowPeriod ^ value)
                    {
                        m_bShowPeriod = value;
                        m_bWasChanged = true;
                    }
                }
            }

            public bool MarkedAsUpdated
            {
                get { return m_bWasChanged; }
                set { m_bWasChanged = value; }
            }

            public string TimeString
            {
                get { return GetTimeString(); }
            }

            private string GetTimeString()
            {
                double fTime;

                fTime = m_TimeStamp.millis + (m_TimeStamp.micros / 1000.0);
                if (m_bShowPeriod)
                    fTime -= (m_oldTimeStamp.millis + (m_oldTimeStamp.micros / 1000.0));
                return fTime.ToString("F1");
            }

            private string GetDataString()
            {
                string strTemp;

                strTemp = "";

                if ((m_Msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_RTR) == TPCANMessageType.PCAN_MESSAGE_RTR)
                    return "Remote Request";
                else
                    for (int i = 0; i < m_Msg.LEN; i++)
                        strTemp += string.Format("{0:X2} ", m_Msg.DATA[i]);

                return strTemp;
            }

            private string GetIdString()
            {
                // We format the ID of the message and show it
                //
                if ((m_Msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED)
                    return string.Format("{0:X8}h", m_Msg.ID);
                else
                    return string.Format("{0:X3}h", m_Msg.ID);
            }

            private string GetMsgTypeString()
            {
                string strTemp;

                if ((m_Msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED)
                    strTemp = "EXTENDED";
                else
                    strTemp = "STANDARD";

                if ((m_Msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_RTR) == TPCANMessageType.PCAN_MESSAGE_RTR)
                    strTemp += "/RTR";

                return strTemp;
            }

        }
        #endregion

        /// <summary>
        /// Help Function used to get an error as text
        /// </summary>
        /// <param name="error">Error code to be translated</param>
        /// <returns>A text with the translated error</returns>
        private string GetFormatedError(TPCANStatus error)
        {
            StringBuilder strTemp;

            // Creates a buffer big enough for a error-text
            //
            strTemp = new StringBuilder(256);
            // Gets the text using the GetErrorText API function
            // If the function success, the translated error is returned. If it fails,
            // a text describing the current error is returned.
            //
            if (PCANBasic.GetErrorText(error, 0, strTemp) != TPCANStatus.PCAN_ERROR_OK)
                return string.Format("An error occurred. Error-code's text ({0:X}) couldn't be retrieved", error);
            else
                return strTemp.ToString();
        }


        private void SetHandle()
        {
            string strTemp;

            strTemp = "PCAN_USB 1(51h)";
            strTemp = strTemp.Substring(strTemp.IndexOf('(') + 1, 2);

            // Determines if the handle belong to a No Plug&Play hardware 
            //
            m_PcanHandle = Convert.ToByte(strTemp, 16);
        }

        #region PCAN 通信
        //PCAN通信分为3个阶段
        //1、PCAN信道初始化，调用函数(CAN_Initialize)； 
        //2、交互(调用函数CAN_Read、CAN_Write)；
        //3、完成，通信完成，调用函数CAN_Uninitialize 释放资源

        /// <summary>
        /// 释放PCAN通道
        /// </summary>
        private void UnInitPcan()
        {
            TPCANStatus result;
            StringBuilder strMsg;
            strMsg = new StringBuilder(256);
            result = PCANBasic.Uninitialize(PCANBasic.PCAN_NONEBUS);
            if(result != TPCANStatus.PCAN_ERROR_OK)
            {
                PCANBasic.GetErrorText(result, 0, strMsg);
                MessageBox.Show(strMsg.ToString());
            }
            else
            {
                Console.WriteLine("PCAN-PCI (ch-1) was released");
                isInitPcan = false;
                tmrRead.Enabled = false;
            }
        }

        private void GetCurrentChannelStatus()
        {
            TPCANStatus result;
            StringBuilder strMsg;
            strMsg = new StringBuilder(256);
            result = PCANBasic.GetStatus(PCANBasic.PCAN_PCIBUS1);
            switch (result)
            {
                case TPCANStatus.PCAN_ERROR_BUSLIGHT:
                    MessageBox.Show("PCAN-PCI (Ch-1): Handling a BUS-LIGHT status...");
                    break;
                case TPCANStatus.PCAN_ERROR_BUSHEAVY:
                    MessageBox.Show("PCAN-PCI (Ch-1): Handling a BUS-HEAVY status...");
                    break;
                case TPCANStatus.PCAN_ERROR_BUSOFF:
                    MessageBox.Show("PCAN-PCI (Ch-1): Handling a BUS-OFF status...");
                    break;
                case TPCANStatus.PCAN_ERROR_OK:
                    MessageBox.Show("PCAN-PCI (Ch-1): Status is OK");
                    break;
                default:
                    // An error occurred, get a text describing the error and show it
                    //
                    PCANBasic.GetErrorText(result, 0, strMsg);
                    MessageBox.Show(strMsg.ToString());
                    break;
            }
        }

        /// <summary>
        /// 从通道中读取消息存在两种肯能：
        /// 1、时间触发读取：通常应用程序启动一个计时器，每50或者100毫秒检查一次消息，循环中调用read方法，直到达到PCAN_ERROR_QRECEIVE的值或者其他错误条件
        /// 2、事件触发读取：当收到消息并插入到接收队列时，包括对PCAN驱动程序发送给已注册应用程序的通知作出反应。
        /// </summary>



        #endregion

        private void btnCamera_Click_1(object sender, EventArgs e)
        {
            CameraFrm cameraFrm = new CameraFrm();
            cameraFrm.Show();
        }

        /// <summary>
        /// 串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbSerialPort_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSerialPort.Checked)
            {
                //设置PCAN 为未选中状态
                rbPcan.Checked = false;
                comboBoxPortSelect.Enabled = true;
            } else
            {
                //设置PCAN为选中状态
                rbPcan.Checked = true;
                comboBoxPortSelect.Enabled = false;
            }
        }

        /// <summary>
        /// pcan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbPcan_CheckedChanged(object sender, EventArgs e)
        {
            if (rbPcan.Checked)
            {
                rbSerialPort.Checked = false;
                //设置select port 端口项为不可用状态
                comboBoxPortSelect.Enabled = false;
            }
            else
            {
                rbSerialPort.Checked = true;
                comboBoxPortSelect.Enabled = true;
            }
        }

        /// <summary>
        /// Form-Closing Function / Finish function
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Releases the used PCAN-Basic channel
            //
            PCANBasic.Uninitialize(m_PcanHandle);
        }

        private void tmrRead_Tick(object sender, EventArgs e)
        {
            // Checks if in the receive-queue are currently messages for read
            // 
            ReadMessages();
        }
    }
}
