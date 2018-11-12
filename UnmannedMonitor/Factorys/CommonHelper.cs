using Factorys;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factorys
{
    /// <summary>
    /// 串口传输
    /// </summary>
    public class CommonHelper
    {
        public delegate void getResultHandel(string data);
        public event getResultHandel getResultEvent;

        private SerialPort serialPort = new SerialPort();
        public CommonHelper()
        {
            serialPort.StopBits = StopBits.One;
            serialPort.BaudRate = 1000000;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
        }

        public void SetPortName(string portName)
        {
            serialPort.PortName = portName;
        }

        public string[] GetCurrentComDevices()
        {
            string[] ports = SerialPort.GetPortNames();
            return ports;
        }

        private bool bo = false;
        /// <summary>
        /// 打开串口
        /// </summary>
        /// <returns></returns>
        public bool OpenSerial(string portName)
        {
            if (!serialPort.IsOpen)
            {
                serialPort.PortName = portName;
            }
            try
            {
                if (!bo)
                {
                    serialPort.Open();
                    bo = true;
                }
            }
            catch
            {
                bo = false;
            }
            return bo;
        }

        public void CloseSerial()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="str">命令字符串</param>
        public void SendData(string data)
        {
            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.Write(data);
        }

        public void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            
            System.Threading.Thread.Sleep(100);
            if (serialPort.IsOpen)
            {
                byte[] buffer = new byte[serialPort.BytesToRead];
                serialPort.Read(buffer, 0, buffer.Length);
                string tempResult = StringUtil.ByteToHex(buffer);
                string result = Encoding.Default.GetString(buffer);
                //StringUtil.MultipleDataSegmentation(result);
                getResultEvent?.Invoke(result);
            }
        }
    }
}
