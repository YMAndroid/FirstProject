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
            bindList();
        }

        /// <summary>
        /// 初始化串口选择Combox
        /// </summary>
        private void InitComBoxPortName()
        {
            string[] comDevices = commonHelper.GetCurrentComDevices();
            if(comDevices != null)
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
            if (checkBox2.Checked)
            {
                //存储txt内容
                StringUtil.WriteLog(data, txtFilePath.Text);
                //存储CSV数据,并处理
                List<UnmannedData> listNameData = StringUtil.MultipleDataSegmentation(data);
                foreach(UnmannedData u in listNameData)
                {
                    StringUtil.WriteCSV(u, txtFilePath.Text);
                    ulist.Add(u); 
                }
                bindList();
                ulist.Clear();
                ////存储csv内容
                //if (data.StartsWith("Num"))
                //{
                //    //第一条数据开始
                //    _data += data;
                //}
                //if (data.EndsWith("#"))
                //{
                //    //分析_data的数据并转换为对象显示和存储
                //    UnmannedData udata = new UnmannedData();

                //    ulist.Add(udata);

                //    StringUtil.WriteCSV(udata, txtFilePath.Text);
                //    _data = "";

                //    bindList();
                //}
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

        private delegate void SetDtCallback(List<UnmannedData> dt);

        private void SetDT(List<UnmannedData> dt)
        {
            // InvokeRequired需要比较调用线程ID和创建线程ID
            // 如果它们不相同则返回true
            if (this.dataGridView1.InvokeRequired)
            {
                SetDtCallback d = new SetDtCallback(SetDT);
                this.Invoke(d, new object[] { dt });
            }
            else
            {
                this.dataGridView1.DataSource = ulist.Where(d => d.DataType == "AK").ToList();
            }
        }

        /// <summary>
        /// 绑定列表
        /// </summary>
        private void bindList()
        {
            this.Invoke(new EventHandler(delegate
            {
                dataGridView1.DataSource = ulist.Where(d => d.DataType == "AK").ToList();
            }));
            //dataGridView1.DataSource = ulist.Where(d => d.DataType == "AK").ToList();

            foreach (DataGridViewColumn item in dataGridView1.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (item.Name == "DataType" || item.Name == "FrameState" || item.Name == "SysFrameNo")
                {
                    item.Visible = false;
                }
            }

            if (checkBox1.Checked)
            {
                dataGridView2.DataSource = ulist.Where(d => d.DataType == "BK").ToList();

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
            Button btn = sender as Button;
            if (!isStart)
            {
                isStart = true;
                btn.Text = "Stop";
                if (!commonHelper.OpenSerial(selectPort))
                {
                    MessageBox.Show("串口打开失败！");
                }
                else
                {
                    MessageBox.Show("串口打开成功，等待接收数据！");
                    commonHelper.SendData("runtst -1");
                    commonHelper.getResultEvent += CommonHelper_getResultEvent;
                }
            }
            else
            {
                isStart = false;
                btn.Text = "Start";
                commonHelper.SendData("stptst");
                commonHelper.CloseSerial();
            }
        }

        ///// <summary>
        ///// 用于测试的数据
        ///// </summary>
        //private SerialPort serialPort = new SerialPort();
        //public void TestOpenProtGetData()
        //{
        //    serialPort.StopBits = StopBits.One;
        //    serialPort.BaudRate = 115200;
        //    serialPort.Parity = Parity.None;
        //    serialPort.DataBits = 8;
            
        //    serialPort.PortName = comboBoxPortSelect.Text.ToString();
        //    serialPort.Open();
        //    serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(readDataEvent);
        //}

        //public void readDataEvent(object sender, EventArgs e)
        //{
        //    //Store the read data in a variable
        //    System.Threading.Thread.Sleep(100);
        //    if (serialPort.IsOpen)
        //    {
        //        byte[] buffer = new byte[serialPort.BytesToRead];
        //        serialPort.Read(buffer, 0, buffer.Length);
        //        StringBuilder sb = new StringBuilder();

        //        for (int i = 0; i < buffer.Length; i++)
        //        {
        //            sb.AppendFormat("{0:X2}", buffer[i]);
        //        }
        //        serialPort.DiscardInBuffer();
        //        string str = sb.ToString();
        //    }
        //}

        void loadPoint()
        {
            while (true)
            {
                //读取数据
                Random rd = new Random();
                double a = rd.Next(-90, 90);
                double b = rd.Next(0, int.Parse(maxM.ToString())) * changeDistance(distance);
                a = 90 - a;
                PointF pointF = getNewPoint(p, a, b);
                drawRectangle(pictureBox1, pointF, Brushes.Red);

                //Thread.Sleep(500);
            }
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
