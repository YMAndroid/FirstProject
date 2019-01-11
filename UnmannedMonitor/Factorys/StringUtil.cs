using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Factorys
{
    public class StringUtil
    {
        /// <summary>
        /// 记录串口数据
        /// </summary>
        /// <param name="txt"></param>
        public static void WriteLog(string logtxt,string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                string logname = DateTime.Now.ToString("yyyyMMdd") + "RadarDatalog.txt";
                FileStream fs = new FileStream(path + "\\" + logname, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine(logtxt);
                sw.Close();
                fs.Close();
            }
            catch { }
        }

        //需要添加固定的头数据


        public static void WritePcanLog(string logtxt, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                string logname = DateTime.Now.ToString("yyyyMMdd") + "RadarPcanDatalog.trc";
                FileStream fs = new FileStream(path + "\\" + logname, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine(logtxt);
                sw.Close();
                fs.Close();
            }
            catch { }
        }



        /// <summary>
        /// 生成csv文件
        /// </summary>
        /// <param name="logtxt"></param>
        /// <param name="path"></param>
        public static void WriteCSV(UnmannedData logtxt, string path, Boolean isFirstWriteCsv)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string strPath = DateTime.Now.ToString("yyyyMMdd") + "RadarDatalog.csv";
                StreamWriter sw = new StreamWriter(new FileStream(path + "\\" + strPath, FileMode.Append), Encoding.GetEncoding("Windows-1252"));//Windows-1252 \GB2312
                if (isFirstWriteCsv)
                {
                    WriteHeader(sw);
                }
                string txt = logtxt.V + "," + logtxt.A + "," + logtxt.R + "," + logtxt.P + "," + logtxt.S + "," + logtxt.CH + "," + logtxt.FrameState + "," + logtxt.SysFrameNo;
                //sw.Write();
                sw.WriteLine(txt);
                sw.Close();
            }
            catch
            {

            }
        }

        private static void WriteHeader(StreamWriter sw)
        {
            string strHeader = "Speed,Angle,Range,Power,State,Channel,FrameState,SysFrameNo";
            sw.WriteLine(strHeader);
        }

        #region 读写Ini文件

        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);

        // 声明INI文件的写操作函数 WritePrivateProfileString()
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        private static string iniFilePath = System.IO.Directory.GetCurrentDirectory() + "\\Default.ini";

        /// <summary>
        /// 根据key获取配置值
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string ReadIniData(string Section, string Key)
        {
            if (File.Exists(iniFilePath))
            {
                StringBuilder temp = new StringBuilder(1024);
                GetPrivateProfileString(Section, Key, "", temp, 1024, iniFilePath);
                return temp.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// 写入配置信息
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Writue(string section, string key, string value)
        {
            // section=配置节，key=键名，value=键值，path=路径
            WritePrivateProfileString(section, key, value, iniFilePath);
        }

       
        /// <summary>
        /// 多条数据分割
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Dictionary<string, List<UnmannedData>> MultipleDataSegmentation(string data)
        {
            //key 存放每一帧的标识 
            Dictionary<string, List<UnmannedData>> dictionary = new Dictionary<string, List<UnmannedData>>();
            int count = data.Count(ch => ch == '#');
            //根据数据中的特殊字符'#' 进行数据分割
            string[] strArray = data.Split('#');
            
            List<UnmannedData> tempListAk = new List<UnmannedData>();
            List<UnmannedData> tempListBk = new List<UnmannedData>();
            for (int i=0; i < strArray.Length; i++)
            {
                if (strArray[i].Contains("Num"))
                {
                    List<UnmannedData> tempList = new List<UnmannedData>();
                    List<UnmannedData> listAkData= SingleAkDataProcess(strArray[i]);
                    if(listAkData != null && listAkData.Count > 0)
                    {
                       
                        for (int j = 0; j < listAkData.Count; j++)
                        {

                            //if ((j + 1 != listAkData.Count) && listAkData[j].SysFrameNo.Equals(listAkData[j + 1].SysFrameNo))
                            //    continue;
                            tempList.Add(listAkData[j]);
                        }
                    }

                    List<UnmannedData> listBkData = SingleBkDataProcess(strArray[i]);
                    
                    if(listBkData != null && listBkData.Count > 0)
                    {
                        //dictionary.Add(listBkData[0].SysFrameNo+"_BK", listBkData);
                        for (int j = 0; j < listBkData.Count; j++)
                        {
                            //if ((j + 1 != listBkData.Count) && listBkData[j].SysFrameNo.Equals(listBkData[j + 1].SysFrameNo))
                            //    continue;
                            tempList.Add(listBkData[j]);
                        }
                    }
                   
                    if(tempList != null && tempList.Count > 0)
                    {
                        dictionary.Add(tempList[0].SysFrameNo, tempList);
                        //tempList.Clear();
                    }
                } 
            }
            return dictionary;
        }

        /// <summary>
        /// 单条AK数据处理
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// Num 1 fft 15380 sig 36227 clu 55 kf 139 
        //--- F 4690 O 1/2/0! ---
        //BK
	        //00: S 55.62, R 2.05, V 0.00, A -4.00
        //AK

	        //00: S 49.54, R 5.46, V 0.02, A -12.05,S 1
	        //01: S 55.62, R 2.09, V -0.04, A -3.29,S 1
        public static List<UnmannedData> SingleAkDataProcess(string data)
        {
            //解析头 -- F 4690
            string framSystemNumber = DecodeHeadData(data);
            if (string.IsNullOrEmpty(framSystemNumber)) return null;
            //解析Ak --  
            string[] resultAkString = Regex.Split(data, "AK", RegexOptions.IgnoreCase);
            if(resultAkString.Length > 1)
            {
                string[] strAkTemp = Regex.Split(resultAkString[1], "\r\n\t", RegexOptions.IgnoreCase);
                List<UnmannedData> nameDataList = new List<UnmannedData>();
                for (int i = 0; i < strAkTemp.Length; i++)
                {
                    if (string.IsNullOrEmpty(strAkTemp[i])) continue;
                    UnmannedData nameData = new UnmannedData();
                    string[] strS = Regex.Split(strAkTemp[i], ",", RegexOptions.IgnoreCase);
                    if (strS.Length < 5) continue;//00: S 50.65, R 5.45, V 0.00, A -11.29,S 1
                    string[] strST = strS[0].Split('S');
                    string[] strSR = strS[1].Split('R');
                    string[] strSV = strS[2].Split('V');
                    string[] strSA = strS[3].Split('A');
                    string pass = @"[\t\r\n\s]";
                    string[] strSS = Regex.Replace(strS[4], pass, "").Split('S');
                    if (strST.Length < 2 || strSR.Length < 2 || strSV.Length < 2 
                        || strSA.Length < 2 || strSS.Length < 2) continue;

                    if (string.IsNullOrEmpty(strST[1]) || string.IsNullOrEmpty(strSR[1]) 
                        || string.IsNullOrEmpty(strSV[1]) || string.IsNullOrEmpty(strSA[1])
                        || string.IsNullOrEmpty(strSS[1])) continue;

                    if (!IsNumeric(strST[1]) || !IsNumeric(strSR[1]) || !IsNumeric(strSV[1])
                        || !IsNumeric(strSA[1]) || !IsNumeric(strSS[1])) continue;
                    nameData.P = double.Parse(strST[1].Replace(" ", ""));
                    nameData.R = double.Parse(strSR[1].Replace(" ", ""));
                    nameData.V = double.Parse(strSV[1].Replace(" ", ""));
                    nameData.A = double.Parse(strSA[1].Replace(" ", ""));

                    nameData.S = Convert.ToInt32(strSS[1].Replace(" ", ""));
                    nameData.CH = i;
                    nameData.FrameState = Convert.ToInt32(strSS[1]);
                    nameData.DataType = "AK";
                    nameData.SysFrameNo = framSystemNumber;
                    nameDataList.Add(nameData);
                }
                return nameDataList;
            }
            return null;      
        }

        public static bool IsNumeric(string value)
        {
            double d;
            if (double.TryParse(value, out d))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 单条BK数据处理
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// Num 1 fft 15380 sig 36227 clu 55 kf 139 
        //--- F 4690 O 1/2/0! ---
        //BK
        //00: S 55.62, R 2.05, V 0.00, A -4.00
        //AK

        //00: S 49.54, R 5.46, V 0.02, A -12.05,S 1
        //01: S 55.62, R 2.09, V -0.04, A -3.29,S 1
        public static List<UnmannedData> SingleBkDataProcess(string data)
        {
            //解析头
            string framSystemNumber = DecodeHeadData(data);
            if (string.IsNullOrEmpty(framSystemNumber)) return null;
            //解析BK --  
            string[] resultFirstString = Regex.Split(data, "BK", RegexOptions.IgnoreCase);
            if(resultFirstString.Length > 1)
            {
                string[] resultSecondString = Regex.Split(resultFirstString[1], "AK", RegexOptions.IgnoreCase);
                if (resultSecondString.Length < 2) return null;
                string[] strTemp = Regex.Split(resultSecondString[0], "\r\n\t", RegexOptions.IgnoreCase);
                List<UnmannedData> nameDataList = new List<UnmannedData>();
                for (int i = 0; i < strTemp.Length; i++)
                {
                    if (string.IsNullOrEmpty(strTemp[i])) continue;
                    UnmannedData nameData = new UnmannedData();
                    string[] strS = Regex.Split(strTemp[i], ",", RegexOptions.IgnoreCase);
                    if (strS.Length < 4) continue;//00: S 55.62, R 2.05, V 0.00, A -4.00
                    string[] strST = strS[0].Split('S');
                    string[] strSR = strS[1].Split('R');
                    string[] strSV = strS[2].Split('V');
                    string[] strSA = strS[3].Split('A');
                    if (strST.Length < 2 || strSR.Length < 2 
                        || strSV.Length < 2 || strSA.Length < 2 || strSA.Length < 2) continue;

                    if (string.IsNullOrEmpty(strST[1]) || string.IsNullOrEmpty(strSR[1])
                        || string.IsNullOrEmpty(strSV[1]) || string.IsNullOrEmpty(strSA[1])) continue;

                    if (!IsNumeric(strST[1]) || !IsNumeric(strSR[1]) || !IsNumeric(strSV[1])
                        || !IsNumeric(strSA[1])) continue;

                    nameData.P = double.Parse(strST[1].Replace(" ", ""));
                    nameData.R = double.Parse((strSR[1].Replace(" ","")));
                    nameData.V = double.Parse(strSV[1].Replace(" ", ""));
                    nameData.A = double.Parse(strSA[1].Replace(" ", ""));
                    nameData.DataType = "BK";
                    nameData.CH = i;
                    nameData.SysFrameNo = framSystemNumber;
                    nameDataList.Add(nameData);
                }
                return nameDataList;
            }
            else
            {
                return null;
            }
        }

        public static string DecodeHeadData(string data)
        {
            //解析头 -- F 4690
            string[] resultFHead = Regex.Split(data, "--- F ", RegexOptions.IgnoreCase);
            if (resultFHead.Length < 2) return null;
            string[] resultFHead1 = Regex.Split(resultFHead[1], "O", RegexOptions.IgnoreCase);
            return resultFHead1[0];
        }

        public static string ByteToHex(byte[] bytes)
        {
            StringBuilder ret = new StringBuilder();
            foreach (byte b in bytes)
            {
                //{0:X2} 大写
                ret.AppendFormat("{0:x2}", b);
            }
            return ret.ToString();
        }

        /// <summary>
        /// 转换为16进制字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string StringToHexString(string input)
        {
           
            char[] values = input.ToCharArray();
            string strResult = "";
            foreach (char letter in values)
            {
                // Get the integral value of the character.
                int value = Convert.ToInt32(letter);
                // Convert the decimal value to a hexadecimal value in string form.
                string hexOutput = String.Format("{0:X}", value);
                strResult += hexOutput;
                Console.WriteLine("Hexadecimal value of {0} is {1}", letter, hexOutput);
            }
            return strResult;
        }

        /// <summary>
        /// 16进制字符串转换为字符串
        /// </summary>
        /// <param name="hs"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string HexStringToString(string hs, Encoding encode)
        {
            //以%分割字符串，并去掉空字符
            string[] chars = hs.Split(new char[] { '%' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] b = new byte[chars.Length];
            //逐个字符变为16进制字节数据
            for (int i = 0; i < chars.Length; i++)
            {
                b[i] = Convert.ToByte(chars[i], 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }

        /// <summary>
        /// 字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] strToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        #endregion

        #region  解析PCAN 数据
        public static int flag = 1;//用来表示当前的显示是标量模式还是向量模式
        public static String FrameEnd = "0600";//帧开始
        public static String FrameStart = "0500";//帧结尾

        /// <summary>
        /// 解析PCAN数据
        /// </summary>
        /// <param name="strData"></param>
        /// <returns></returns>
        public static List<UnmannedData> AnalyticalPcanData(List<String> strData)
        {
            if (strData.Count <= 0) return null;
            List<UnmannedData> listUd = new List<UnmannedData>();
            uint[] DataBuf = new uint[9];
            int j = 0;
            foreach(String temp in strData)
            {
                j++;
                string[] strArray = temp.Split(' ');
                for(int i=0;i < strArray.Length -1 ;i++)
                {
                    DataBuf[i] = Convert.ToUInt32(strArray[i],16);
                }
              
                if (((DataBuf[0] << 8 | DataBuf[1]) & 0xFFFF) == 0xFFFF)
                {
                    flag = 1;//当前报文是标量报文
                }
                else
                {
                    flag = 0;//当前报文为矢量报文
                }
                UnmannedData ud = new UnmannedData();
                //当前报文是标量报文时按照标量报文格式进行解析
                if (flag == 1)
                {
                    float R = (float)(((DataBuf[2] << 8) | DataBuf[3]) / 10.0);
                    float V = (float)((((DataBuf[4] << 8) | DataBuf[5]) / 100.0) - 81.9);
                    float angle1 = (float)(((DataBuf[6] << 8) | DataBuf[7]) / 10.0 - 180.0);
                    ud.A = angle1;
                    ud.R = R;
                    ud.V = V;
                    ud.DataType = "AK";
                    ud.CH = j;
                    ud.FrameState = 1;
                }
                else if (flag == 0)
                {

                    //当前的报文是矢量报文时按照矢量报文的格式解析
                    float Rx = (float)((((DataBuf[0] & 0x7f) << 8) | DataBuf[1]) / 100.0);//不算最高位的符号位
                    if ((DataBuf[0] & 0x80) > 0)//Rx是负数的情况
                    {
                        Rx = -Rx;
                    }
                    float Ry = (float)(((DataBuf[2] & 0x7f) << 8 | DataBuf[3]) / 100.0);//不算最高位的符号位
                    if ((DataBuf[2] & 0x80) > 0)
                    {
                        Ry = -Ry;
                    }
                    float Vx = (float)(((DataBuf[4] & 0x7f) << 2 | (DataBuf[5] & 0xc0) >> 6) / 10.0);//不算最高位的符号位
                    if ((DataBuf[4] & 0x80) > 0)
                    {
                        Vx = -Vx;
                    }
                    float Vy = (float)((((DataBuf[5] & 0x1f) << 4) | (DataBuf[6] & 0xf0) >> 4) / 10.0);
                    if ((DataBuf[5] & 0x20) > 0)
                    {
                        Vy = -Vy;
                    }
                    float angle = (float)((((DataBuf[6] & 0x07) << 8) | DataBuf[7]) / 10.0);
                    if ((DataBuf[6] & 0x8) > 0)//angle是负数
                    {
                        angle = -angle;
                    }

                    //暂时不做处理
                    //return null;
                    ud.A = Math.Round(angle, 2);
                    //ud.A = Convert.ToSingle(Convert.ToDecimal(angle).ToString("f2"));
                    //ud.R = Math.Sqrt(Rx * Rx + Ry * Ry);
                    ud.R = Math.Round(Rx, 2);
                    ud.V = Math.Round(Ry, 2);
                    ud.CH = j;
                    ud.DataType = "AK";
                    ud.FrameState = 1;
                    Console.WriteLine("ud.R = " + ud.R + " ;ud.V = " + ud.V + " ;ud.A = " + ud.A);
                }
                listUd.Add(ud);
            }
            return listUd;
        }
        #endregion
    }

    public class UnmannedData : IComparable
    {
        public int CH { get; set; }
        public double R { get; set; }
        public double V { get; set; }
        public double A { get; set; }
        public double P { get; set; }
        public int S { get; set; }
        public string SysFrameNo { get; set; }
        public int FrameState { get; set; }
        /// <summary>
        /// BK   AK
        /// </summary>
        public string DataType { get; set; }

        public int CompareTo(object obj)
        {

            throw new NotImplementedException();
        }
    }

}
