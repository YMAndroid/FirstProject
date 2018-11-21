using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using Factorys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnmannedMonitor
{
    public partial class CameraFrm : Form
    {
        public CameraFrm()
        {
            InitializeComponent();
        }

        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        public int selectedDeviceIndex = 0;

        
        private void CameraFrm_Load(object sender, EventArgs e)
        {
            //InitUI();
            InItComBoxDeviceList();
            InitVideoSettings();
        }

        /// <summary>
        /// 枚举所有视频输入设备
        /// </summary>
        /// <returns></returns>
        public FilterInfoCollection GetDevices()
        {
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count != 0)
                {
                    Console.WriteLine("找到的视频设备数量为: {0}个", videoDevices.Count);
                    return videoDevices;
                }
                else
                {
                    Console.WriteLine("未找到可用的视频设备!");
                    return null;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("未找到可用的视频设备! 具体原因是: {0}" + ex.Message);
                return null;
            }
        }

        private void InitUI()
        {
            //连接//开启摄像头
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);//连接摄像头。
            videoSource.VideoResolution = videoSource.VideoCapabilities[0];
            videoSource.NewFrame += VideoSource_NewFrame;
        }

        private void InitVideoSettings()
        {  
            videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);//连接摄像头。
            videoSource.VideoResolution = videoSource.VideoCapabilities[0];
            videoSource.NewFrame += VideoSource_NewFrame;
        }

        private void InItComBoxDeviceList()
        {
            GetDevices();
            for(int i = 0; i < videoDevices.Count; i++)
            {
                comboBox1.Items.Add(videoDevices[i].Name);
            }
            comboBox1.SelectedIndex = 0;
         }

        /// <summary>
        /// 连接视频摄像头
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="resolutionIndex"></param>
        /// <returns></returns>
        public VideoCaptureDevice VideoConnect(int deviceIndex = 0, int resolutionIndex = 0)
        {
            if(videoDevices.Count <=0)
            {
                return null;
            }
            selectedDeviceIndex = deviceIndex;
            videoSource = new VideoCaptureDevice(videoDevices[deviceIndex].MonikerString);
            return null;
        }

        /// <summary>
        /// 照片处理
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public Bitmap ImageProcess(Image img)
        {
            Bitmap bm2 = new Bitmap(img.Width, img.Height);
            Graphics g = Graphics.FromImage(bm2);
            g.Clear(Color.White);
            g.DrawImage(img, 0, 0);
            g.Dispose();
            //img.Dispose();
            return bm2;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = (Bitmap)eventArgs.Frame.Clone();            

            //录像
            Graphics g = Graphics.FromImage(bmp);
            SolidBrush drawBrush = new SolidBrush(Color.Yellow);

            Font drawFont = new Font("Arial", 6, FontStyle.Bold, GraphicsUnit.Millimeter);
            int xPos = bmp.Width - (bmp.Width - 15);
            int yPos = 10;
            //写到屏幕上的时间
            drawDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            g.DrawString(drawDate, drawFont, drawBrush, xPos, yPos);
            if (!Directory.Exists(videoPath))
                Directory.CreateDirectory(videoPath);

            //创建文件路径
            //fileFullPath = path + fileName;

            if (stopREC)
            {
                stopREC = true;
                createNewFile = true;  //这里要设置为true表示要创建新文件
                if (videoWriter != null)
                    videoWriter.Close();
            }
            else
            {
                //开始录像
                if (createNewFile)
                {
                    videoFileName = DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss") + ".avi";
                    videoFileFullPath = videoPath + videoFileName;
                    createNewFile = false;
                    if (videoWriter != null)
                    {
                        videoWriter.Close();
                        videoWriter.Dispose();
                    }
                    videoWriter = new VideoFileWriter();
                    //这里必须是全路径，否则会默认保存到程序运行根据录下了
                    videoWriter.Open(videoFileFullPath, bmp.Width, bmp.Height, frameRate, VideoCodec.MPEG4);
                    videoWriter.WriteVideoFrame(bmp);
                }
                else
                {
                    videoWriter.WriteVideoFrame(bmp);
                }
            }
            pictureBox1.Image = bmp;
            GC.Collect();
        }

        private bool stopREC = true;
        private bool createNewFile = true;
        int frameRate = 20; //默认帧率
        private string drawDate = string.Empty;
        private void btnStartVideotape_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("请选择或输入视频存储路径！");
                return;
            }
            //开始录像
            if (btnStartVideotape.Text == "Start Camera")
            {
                videoSource.Start();
                stopREC = false;
                btnStartVideotape.Text = "Stop Camera";
            }
            else if (btnStartVideotape.Text == "Stop Camera")
            {
                videoSource.Stop();
                stopREC = true;
                btnStartVideotape.Text = "Start Camera";
            }
        }

        private string videoPath = ""; //视频文件路径
        private string videoFileName = string.Empty; //视频文件名
        private string videoFileFullPath = string.Empty; //视频文件全路径

        private VideoFileWriter videoWriter = null;

        private void CameraFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            videoSource.Stop();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(videoSource != null)
            {
                if (videoSource.IsRunning)
                {
                    MessageBox.Show("请先停止录制视频，在切换摄像头!");
                    return;
                }
                InitVideoSettings();
            }   
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            string selectPath = path.SelectedPath;
            if (string.IsNullOrEmpty(selectPath)) return;
            textBox1.Text = path.SelectedPath + "\\";
            videoPath = textBox1.Text;
            //StringUtil.Writue("DataFile", "path", path.SelectedPath + "\\");
        }
    }
}
