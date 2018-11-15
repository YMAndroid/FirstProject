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
            //string[] comDevices = commonHelper.GetCurrentComDevices();
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
                        StringUtil.WriteCSV(u, txtFilePath.Text);       
                    }
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


        private void Comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!serialPort.IsOpen) return;
            byte[] buffer = new byte[serialPort.BytesToRead];
            serialPort.Read(buffer, 0, buffer.Length);
            string tempResult = StringUtil.ByteToHex(buffer);
            string data = Encoding.Default.GetString(buffer);

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
                        StringUtil.WriteCSV(u, txtFilePath.Text);
                    }
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
            Point px2 = new Point(x, y);
            g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);

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

            //速度和角度
            if (control.Name == "pictureBox1")
            {
                //画数值
                g.DrawString("-50", new Font("宋体", 10), Brushes.Black, new PointF(10, y + 7));
                g.DrawString("-25", new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 1) + 15, y + 7));
                g.DrawString("0", new Font("宋体", 10), Brushes.Black, new PointF((ypaddings * 2) + 26, y + 7));
                g.DrawString("25", new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 3 + 22, y + 7));
                g.DrawString("50", new Font("宋体", 10), Brushes.Black, new PointF(ypaddings * 4 + 22, y + 7));
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

        delegate void DelegateAk(List<UnmannedData> table);
        private void SetDgvDataSourceAk(List<UnmannedData> table)
        {
            dataGridView1.ClearSelection();
            if (dataGridView1.InvokeRequired)
            {
                BeginInvoke(new DelegateAk(SetDgvDataSourceAk), new object[] { table.Where(d=> d.DataType == "AK").ToList() });
            }
            else
            {
                dataGridView1.DataSource = table.Where(d => d.DataType == "AK").ToList();
            }
            foreach (DataGridViewColumn item in dataGridView2.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;
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
            }
            foreach (DataGridViewColumn item in dataGridView2.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;
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
                //if (commonHelper.OpenSerial(selectPort))
                //{
                //    commonHelper.SendData("runtst -1");
                //    commonHelper.SetIsReceiveData(true);
                //    commonHelper.getResultEvent += CommonHelper_getResultEvent;   
                //}
                //else
                //{
                //    MessageBox.Show("串口打开失败");
                //}  
            }
            else
            {
                isStart = false;
                btn.Text = "Start";
                OpenOrSerialPort();
                //commonHelper.SendData("stptst");
                //commonHelper.SetIsReceiveData(false);
                //commonHelper.CloseSerial();
                //StopDeviceData();
            }
        }

        /// <summary>
        /// 用于保存已经画过的矩形点
        /// </summary>
        Dictionary<Point, Point> dicPoints = new Dictionary<Point, Point>();

        //

        void loadPoint()
        {
            System.Drawing.Pen pen = null;

            while (true)
            {
                //读取数据

                //Random rd = new Random();
                //double a = rd.Next(-90, 90);
                //double b = rd.Next(0, int.Parse(maxM.ToString())) * changeDistance(distance);
                //a = 90 - a;
                //画 角度跟距离 pictureBox2
                for (int i=0;i<ulist.Count;i++)
                {
                    double r = ulist[i].R;// * changeDistance(distance); //距离 --remove
                    double a = ulist[i].A;//角度 --angle
                    double v = ulist[i].V;
                    int picture1BoxHeight  = pictureBox1.Height;
                    int picture1BoxWigth = pictureBox1.Width;
                    int picture2BoxHeight = pictureBox2.Height;
                    int picture2BoxWigth = pictureBox2.Width;
                    if (ulist[i].DataType.Equals("AK"))
                    {
                        PointF pointF = getNewPoint(p, a, r);
                        PointF pointS = getNewSpeedPoint(p, picture1BoxWigth/2, picture2BoxHeight - v -10);
                        if (v < 0)
                        {
                            pen = new Pen(Color.Green);
                            //速度为负值 用绿色 --表示靠近目标
                            drawRectangle(pictureBox2, pointF, Brushes.Green);//显示速度 根据V 跟A换算
                            drawRectangle(pictureBox1, pointS, Brushes.Green);//显示每个物体的距离 根据 R 跟 A 换算
                            
                        }
                        else if (v == 0)
                        {
                            pen = new Pen(Color.Yellow);
                            //速度为0 用黄色 ---表示目标处于静止状态
                            drawRectangle(pictureBox2, pointF, Brushes.Yellow);
                            drawRectangle(pictureBox1, pointS, Brushes.Yellow);
                        }
                        else if (v > 0)
                        {
                            pen = new Pen(Color.Red);
                            //速度为正值 用红色 ---表示远离目标
                            drawRectangle(pictureBox2, pointF, Brushes.Red);
                            drawRectangle(pictureBox1, pointS, Brushes.Red);
                        }
                    }             
                }
                cleaDrawRectangle(pictureBox2);
                cleaDrawRectangle(pictureBox1);
                //Thread.Sleep(500);
            }
        }

        private void drawRectangle()
        {

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
            //在Flash中顺时针角度为正，逆时针角度为负
            //换算过程中先将角度转为弧度
            var radian = angle * Math.PI / 180;
            var xMargin = float.Parse((Math.Cos(radian) * bevel).ToString());
            var yMargin = -float.Parse((Math.Sin(radian) * bevel).ToString());
            return new PointF(pointB.X + xMargin + (pictureBox2.Width / 2), pictureBox2.Height - (pointB.Y + yMargin) - 10);
        }

        private PointF getNewSpeedPoint(PointF pointB, double a, double v)
        {
           //显示Y轴的速度
           //X 轴 始终为 0
            var yMargin = (float)v;
            var xMargin = (float)a;
            return new PointF(pointB.X + xMargin, pointB.Y + yMargin);
        }

        /// <summary>
        /// 根据坐标点画矩形
        /// </summary>
        /// <param name="control"></param>
        /// <param name="pointF"></param>
        /// <param name="brush"></param>
        private void drawRectangle(Control control, PointF pointF, Brush brush)
        {
            Graphics g = control.CreateGraphics();
            g.FillRectangle(brush, new RectangleF(pointF, new Size(10, 20)));
        }

        private void cleaDrawRectangle(Control control)

        {
            Graphics g = control.CreateGraphics();
            g.Clear(Color.White);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            distance = comboBox1.SelectedIndex;

            double db = double.Parse(comboBox1.SelectedItem.ToString().Substring(0, comboBox1.SelectedItem.ToString().Length - 1));
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

        private void comboBoxPortSelect_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
