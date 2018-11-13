using System;
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
                string logname = DateTime.Now.ToString("yyyyMMdd") + "RadarDatalog.txt";
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
        public static void WriteCSV(UnmannedData logtxt, string path)
        {
            string strPath = DateTime.Now.ToString("yyyyMMdd") + "RadarDatalog.csv";
            StreamWriter sw = new StreamWriter(new FileStream(path + "\\" + strPath, FileMode.Append), Encoding.GetEncoding("Windows-1252"));//Windows-1252 \GB2312
            WriteHeader(sw);
            string txt = logtxt.V + "," + logtxt.A + "," + logtxt.R + "," + logtxt.P + "," + logtxt.S + "," + logtxt.CH + "," + logtxt.FrameState + "," + logtxt.SysFrameNo;
            sw.Write(txt);
            sw.Close();
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
        public static List<UnmannedData> MultipleDataSegmentation(string data)
        {
            int count = data.Count(ch => ch == '#');
            //根据数据中的特殊字符'#' 进行数据分割
            string[] strArray = data.Split('#');
            List<UnmannedData> tempList = new List<UnmannedData>();
            List<UnmannedData> tempListAk = new List<UnmannedData>();
            List<UnmannedData> tempListBk = new List<UnmannedData>();
            for (int i=0; i < strArray.Length; i++)
            {
                if (strArray[i].Contains("Num"))
                {
                    List<UnmannedData> listAkData= SingleAkDataProcess(strArray[i]);
                    if(listAkData != null)
                    {
                        for (int j = 0; j < listAkData.Count; j++)
                        {
                            if ( (j + 1 != listAkData.Count) &&  listAkData[j].SysFrameNo.Equals(listAkData[j + 1].SysFrameNo))
                                continue;
                            tempListAk.Add(listAkData[j]);
                        }
                    }

                    List<UnmannedData> listBkData = SingleBkDataProcess(strArray[i]);
                    
                    if(listBkData != null)
                    {
                        for(int j = 0; j < listBkData.Count; j++)
                        {
                            if ((j + 1 != listBkData.Count) && listBkData[j].SysFrameNo.Equals(listBkData[j + 1].SysFrameNo))
                                continue;
                            tempListBk.Add(listBkData[j]);
                        }
                    }   
                } 
            }
            for(int i=0; i<tempListAk.Count;i++)
            {
                tempListAk[i].CH = i;
                tempList.Add(tempListAk[i]);
            }
            for(int i = 0; i < tempListBk.Count; i++)
            {
                tempListBk[i].CH = i;
                tempList.Add(tempListBk[i]);
            }
            
            return tempList;
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
                    if (strST.Length < 2 || strSR.Length < 2 || strSV.Length < 2 || strSA.Length < 2) continue;

                    nameData.P = double.Parse(strST[1]);
                    nameData.R = double.Parse(strSR[1]);
                    nameData.V = double.Parse(strSV[1]);
                    nameData.A = double.Parse(strSA[1]);
                    nameData.S = Convert.ToInt32(strSS[1]);
                    nameData.FrameState = Convert.ToInt32(strSS[1]);
                    nameData.DataType = "AK";
                    nameData.SysFrameNo = framSystemNumber;
                    nameDataList.Add(nameData);
                }
                return nameDataList;
            }
            return null;      
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
                    if (strST.Length < 2 || strSR.Length < 2 || strSV.Length < 2 || strSA.Length < 2) continue;
                    nameData.P = double.Parse(strST[1]);
                    nameData.R = double.Parse(strSR[1]);
                    nameData.V = double.Parse(strSV[1]);
                    nameData.A = double.Parse(strSA[1]);
                    nameData.DataType = "BK";
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
        #endregion
    }

    public class UnmannedData
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
    }
}
