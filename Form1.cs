using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace FormsForPro
{
    public partial class Form1 : Form
    {
        public static string ma="", v="", hz="", w="", kwh="";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Threadtcpserver instance = new Threadtcpserver();
            //  richTextBox1.AppendText("***************************\n");
            (new System.Threading.Tasks.Task(abc)).Start();
            button1.Enabled = false;
        }
        private void abc()
        {
            for (int i = 0; i <= 20; i++)
            {
                //         this.Invoke(new Action(() => { this.richTextBox1.Text = i.ToString(); }));
                this.Invoke(new Action(() => { richTextBox1.AppendText("IP&Port:" + "balabalabala" + "\n" + "Time:" + DateTime.Now + "\n" + "mA:" + ma + "\n" + "V:" + v + "\n" + "Hz:" + hz + "\n" + "W:" + w + "\n" + "kWH:" + kwh + "\n" + "********************\n"); }));
                
                //按1秒循环
                System.Threading.Thread.Sleep(3000);
            }
        }

        class Math           //转换方法类
        {
            public static byte[] HexStrTobyte(string hexString)  //16进制字符串转字节数组
            {
                hexString = hexString.Replace(" ", "");
                if ((hexString.Length % 2) != 0)
                    hexString += " ";
                byte[] returnBytes = new byte[hexString.Length / 2];
                for (int i = 0; i < returnBytes.Length; i++)
                    returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
                return returnBytes;
            }

            public static string byteToHexStr(byte[] bytes)  //字节数组转16进制字符串
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

            public static int HextoInt(char m)  //16进制char转10进制int
            {
                int a = Convert.ToInt16(m);
                if ((a >= 48) && (a <= 57))
                    return a - 48;
                else if ((a >= 65) && (a <= 70))
                    return a - 55;
                else
                    return 0;
            }

        }

        class MyConvert            //各种换算类
        {
            public string mA(char[] raw)  //电流换算
            {
                int[] s = new int[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    s[i] = Math.HextoInt(raw[i]);
                int a = s[0] * 16 + s[1];
                int b = s[2] * 16 + s[3];
                int c = s[4] * 16 + s[5];
                double f = (a * 65536 + b * 256 + c) * 1234.0 / 18976 / 15;
                return f.ToString("f2");
            }

            public string V(char[] raw)   //电压换算
            {
                int[] s = new int[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    s[i] = Math.HextoInt(raw[i]);

                int a = s[0] * 16 + s[1];
                int b = s[2] * 16 + s[3];
                int c = s[4] * 16 + s[5];
                double f = (a * 65536 + b * 256 + c) * 214.8 / 1805143 * 222 / 229;
                return f.ToString("f2");
            }

            public string Hz(char[] raw)  //频率换算
            {
                int[] s = new int[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    s[i] = Math.HextoInt(raw[i]);

                int a = s[0] * 16 + s[1];
                int b = s[2] * 16 + s[3];
                double f = 3597545.0 / 8 / (a * 256 + b);
                return f.ToString("f2");
            }

            public string W(char[] raw)   //功率换算
            {
                int[] s = new int[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    s[i] = 15 - Math.HextoInt(raw[i]);

                int a = s[0] * 16 + s[1];
                int b = s[2] * 16 + s[3];
                int c = s[4] * 16 + s[5];
                int d = s[6] * 16 + s[7];
                double f = (a * 16777216 + b * 65536 + c * 256 + d) * 2.5258 / 10000 * 0.065;
                return f.ToString("f2");
            }

            public string kWh(char[] raw) //用电量换算
            {
                int[] s = new int[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    s[i] = Math.HextoInt(raw[i]);

                int a = s[0] * 16 + s[1];
                int b = s[2] * 16 + s[3];
                int c = s[4] * 16 + s[5];
                double f = (a * 65536 + b * 256 + c) / 3200.0 / 15;
                return f.ToString("f4");
            }

        }


        class SqlConnection      //连接数据库类
        {
            private MySqlConnection getmysqlcon()
            {
                string M_str_sqlcon = "server=localhost;user id=root;password=lsl532;database=data"; //根据自己的设置
                MySqlConnection myCon = new MySqlConnection(M_str_sqlcon);
                return myCon;
            }

            /// 执行MySqlCommand
            public void getmysqlcom(string M_str_sqlstr)
            {
                MySqlConnection mysqlcon = this.getmysqlcon();
                mysqlcon.Open();
                MySqlCommand mysqlcom = new MySqlCommand(M_str_sqlstr, mysqlcon);
                mysqlcom.ExecuteNonQuery();
                mysqlcom.Dispose();
                mysqlcon.Close();
                mysqlcon.Dispose();
            }
        }


        public class Threadtcpserver : Form1     //多线程类
        {
            public Socket server;
            public string IP_Port;
            public Threadtcpserver()               //创建线程
            {
                //初始化IP地址  
                IPAddress local = IPAddress.Parse("192.168.1.129");
                IPEndPoint iep = new IPEndPoint(local, 8899);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
                //将套接字与本地终结点绑定  
                server.Bind(iep);
                //在本地8899端口号上进行监听  
                server.Listen(20);
                Console.WriteLine("等待客户机进行连接......");
//                while (true)
//                {
                    //得到包含客户端信息的套接字  
                    Socket client = server.Accept();
                    //创建消息服务线程对象  
                    ClientThread newclient = new ClientThread(client);
                    //把ClientThread类的ClientService方法委托给线程  
                    Thread newthread = new Thread(new ThreadStart(newclient.ClientService));
                    //启动消息服务线程  
                    newthread.Start();
 //               }
            }

            public class ClientThread               //最主要程序       
            {
                //connections变量表示连接数  
                public static int connections = 0;
                public Socket service;

                public string IP_Port { get; private set; }
                public int ID { get; private set; }


                //构造函数  
                public ClientThread(Socket clientsocket)
                {
                    //service对象接管对消息的控制  
                    this.service = clientsocket;
                }

                public void SendMessage(string mag)                //发送消息
                {
                    byte[] ms = Math.HexStrTobyte(mag);
                    service.Send(ms, ms.Length, 0);

                }

                public char[] ReceiveMessage()                     //接收消息
                {
                    string recvStr = "";
                    byte[] recvBytes = new byte[10];
                    int bytes;
                    bytes = service.Receive(recvBytes, recvBytes.Length, 0); //从客户端接受消息
                    recvStr = Math.byteToHexStr(recvBytes);
                    char[] raw = recvStr.ToCharArray();
                    return raw;
                }

                public void Init()
                {
                    byte[] bytes = new byte[4];
                    //如果Socket不是空，则连接数加1  
                    if (service != null)
                    {
                        connections++;
                    }
                    Console.WriteLine("新客户连接建立：{0}个连接数", connections);
                    IP_Port = service.RemoteEndPoint.ToString();   //获取IP地址和端口号
                    string pattern = @".+\.([0-9]+):";              //获取IP最后一个字段，构造ID
                    MatchCollection ipid = Regex.Matches(IP_Port, pattern);
                    int ppid = int.Parse(ipid[0].Groups[1].Value);
                    int dpid = 1;
                    ID = dpid * 1000 + ppid;

                    //发送初始化命令
                    SendMessage("ea5abb");
                    SendMessage("8000007f");
                    SendMessage("8170010d");
                    SendMessage("8203a0da");
                    SendMessage("83001f5d");
                    SendMessage("85024434");
                    SendMessage("8a16d08f");
                    SendMessage("8efe165d");
                    SendMessage("eadc39");
                    Thread.Sleep(5);

                }

                public void SendOrder()                             //发送命令
                {
                    MyConvert mc = new MyConvert();

                    SendMessage("22");                                     //电流命令  
                    ma = mc.mA(ReceiveMessage());

                    SendMessage("24");                                     //电压命令
                    v = mc.V(ReceiveMessage());

                    SendMessage("25");                                     //频率命令
                    hz = mc.Hz(ReceiveMessage());

                    SendMessage("26");                                     //功率命令 
                    w = mc.W(ReceiveMessage());

                    SendMessage("29");                                     //用电量命令   
                    kwh = mc.kWh(ReceiveMessage());

                    //        Console.WriteLine("IP&Port: {0}\nTime: {1}\nmA: {2}\nV: {3}\nHz: {4}\nW: {5}\nkWH: {6}", IP_Port, DateTime.Now, ma, v, hz, w, kwh);
                    //         Console.WriteLine("***************************");

                    SqlConnection mdd = new SqlConnection();               //向数据库写数据
                    string sql = "INSERT INTO Msg (id,Time,IP_Port,mA,V,Hz,W,kWh) VALUES" +
                        "('" + ID + "','" + DateTime.Now + "','" + IP_Port + "','" + ma + "','" + v + "','" + hz + "','" + w + "','" + kwh + "')";
                    mdd.getmysqlcom(sql);

                }
                public void ClientService()
                {

                    Init();
                    while (true)
                    {
                        SendOrder();
                        //    service.Close();

                        Thread.Sleep(3000);
                    }

                    //关闭套接字  
                    //         service.Close();
                    //         connections--;
                    //         Console.WriteLine("客户关闭连接：{0}个连接数", connections);
                }
            }
        }
    }
}
