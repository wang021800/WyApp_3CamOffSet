﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HslCommunication;
using WY_App.Utility;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Drawing.Imaging;
using HalconDotNet;
using TcpClient = WY_App.Utility.TcpClient;
using Sunny.UI.Win32;
using Newtonsoft.Json.Linq;
using Sunny.UI;
using static WY_App.Utility.Parameters;
using OpenCvSharp.Dnn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using OpenCvSharp.Flann;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WY_App
{
    public partial class MainForm : Form
    {
        HslCommunication hslCommunication;
        public static string Alarm = "";
        public static List<string> AlarmList = new List<string>();
        Thread myThread;
        Thread[] MainThread= new Thread[3];
        //Thread ImageThread;
        //public static MVLineScanCam HivCam = new MVLineScanCam(); 
        Halcon halcon = new Halcon();
        public static Parameters.Rect1[] specifications;
        HWindow[] hWindows0;
        HWindow[] hWindows1;
        HWindow[] hWindows2;
        public static List<HObject> ho_Image = new List<HObject>();
        public static List<HObject> ho_DefectImage = new List<HObject>();
        public static List<HObject> ho_OrigalImage = new List<HObject>();
        public static HObject[] hImage = new HObject[3];
        public static HTuple[] hv_AcqHandle = new HTuple[4];
        double[] defectionValues = new double[4];

        private static Queue<Func<int>> m_List = new Queue<Func<int>>();
        private static object m_obj = new object();
        private bool isExit = false;
        static HObject[] hObjectIn = new HObject[3];
        static HObject[] hObjectOut = new HObject[3];
        public static int CamNum = 0;
        public static int baseNum = 0;
        public static HObject[] hoRegions = new HObject[3];
        bool m_Pause = false;
        bool DetectionResult = true;
        static string productSN = "0000";

        public MainForm()
        {
            InitializeComponent();
            HOperatorSet.ReadImage(out hImage[0], Application.StartupPath + "/image/N1.bmp");
            HOperatorSet.GetImageSize(MainForm.hImage[0], out Halcon.hv_Width[0], out Halcon.hv_Height[0]);//获取图片大小规格
            HOperatorSet.ReadImage(out hImage[1], Application.StartupPath + "/image/N1.bmp");
            HOperatorSet.GetImageSize(MainForm.hImage[1], out Halcon.hv_Width[1], out Halcon.hv_Height[1]);//获取图片大小规格
            HOperatorSet.ReadImage(out hImage[2], Application.StartupPath + "/image/N1.bmp");
            HOperatorSet.GetImageSize(MainForm.hImage[2], out Halcon.hv_Width[2], out Halcon.hv_Height[2]);//获取图片大小规格
            hWindows0 = new HWindow[3] { hWindowControl1.HalconWindow, hWindowControl4.HalconWindow, hWindowControl5.HalconWindow };
            HOperatorSet.SetPart(hWindows0[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows0[1], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.SetPart(hWindows0[2], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.DispObj(hImage[0], hWindows0[0]);
            hWindows1 = new HWindow[3] { hWindowControl2.HalconWindow, hWindowControl6.HalconWindow, hWindowControl7.HalconWindow };
            HOperatorSet.SetPart(hWindows1[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows1[1], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.SetPart(hWindows1[2], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.DispObj(hImage[1], hWindows1[0]);

            hWindows2 = new HWindow[3] { hWindowControl3.HalconWindow, hWindowControl8.HalconWindow, hWindowControl9.HalconWindow };
            HOperatorSet.SetPart(hWindows2[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows2[1], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.SetPart(hWindows2[2], 0, 0, 1000, 1000);//设置窗体的规格
            HOperatorSet.DispObj(hImage[2], hWindows2[0]);

           
           
            pictureBox1.Load(Application.StartupPath + "/image/logo.png");
            #region 读取配置文件
            try
            {
                Parameters.commministion = XMLHelper.BackSerialize<Parameters.Commministion>("Parameter/Commministion.xml");
            }
            catch
            {
                Parameters.commministion = new Parameters.Commministion();
                XMLHelper.serialize<Parameters.Commministion>(Parameters.commministion, "Parameter/Commministion.xml");
            }
            try
            {
                Parameters.counts = XMLHelper.BackSerialize<Parameters.Counts>(Parameters.commministion.productName + "/CountsParams.xml");
            }
            catch
            {
                Parameters.counts = new Parameters.Counts();
                XMLHelper.serialize<Parameters.Counts>(Parameters.counts, Parameters.commministion.productName + "/CountsParams.xml");
            }
            try
            {
                Parameters.counts = XMLHelper.BackSerialize<Parameters.Counts>(Parameters.commministion.productName + "/CountsParams.xml");
            }
            catch
            {
                Parameters.cameraParam = new Parameters.CameraParam();
                XMLHelper.serialize<Parameters.CameraParam>(Parameters.cameraParam, Parameters.commministion.productName + "/CameraParam.xml");
            }
            try
            {
                Parameters.specifications = XMLHelper.BackSerialize<Parameters.Specifications>(Parameters.commministion.productName + "/Specifications.xml");
            }
            catch
            {
                Parameters.specifications = new Parameters.Specifications();
                XMLHelper.serialize<Parameters.Specifications>(Parameters.specifications, Parameters.commministion.productName + "/Specifications.xml");
            }
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Parameters.detectionSpec[i] = XMLHelper.BackSerialize<Parameters.DetectionSpec>(Parameters.commministion.productName + "/DetectionSpec" + i + ".xml");
                }
                catch
                {
                    Parameters.detectionSpec[i] = new Parameters.DetectionSpec();
                    XMLHelper.serialize<Parameters.DetectionSpec>(Parameters.detectionSpec[i], Parameters.commministion.productName + "/DetectionSpec" + i + ".xml");
                }
                HOperatorSet.ReadRegion(out hoRegions[i], Parameters.commministion.productName + "/halcon/hoRegion" + i + ".tiff");
            }
            #endregion
            myThread = new Thread(initAll);
            myThread.IsBackground = true;
            myThread.Start();
        }

        /// <summary>
        /// skinPictureBox1滚动缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {


            }
            catch (Exception x)
            {
                LogHelper.Log.WriteError("缩放异常：" + x.Message);
            }
        }

        //鼠标移动
        int xPos = 0;
        int yPos = 0;
        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            xPos = e.X;//当前x坐标.
            yPos = e.Y;//当前y坐标.
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox1_MouseMove1(object sender, MouseEventArgs e)
        {

        }


        private void initAll()
        {
            while (true)
            {
                Thread.Sleep(1);
                Task task = new Task(() =>
                {
                    MethodInvoker start = new MethodInvoker(() =>
                    {
                        lab_Time.Text = System.DateTime.Now.ToString();
                        if (Parameters.commministion.PlcEnable)
                        {
                            if (HslCommunication.plc_connect_result)
                            {
                                lab_PLCStatus.Text = "在线";
                                lab_PLCStatus.BackColor = Color.Green;
                            }
                            else
                            {
                                lab_PLCStatus.Text = "离线";
                                lab_PLCStatus.BackColor = Color.Red;
                            }
                        }
                        else
                        {
                            lab_PLCStatus.Text = "禁用";
                            lab_PLCStatus.BackColor = Color.Gray;
                        }
                        if (Parameters.commministion.TcpClientEnable)
                        {
                            if (TcpClient.TcpClientConnectResult)
                            {
                                lab_Client.Text = "在线";
                                lab_Client.BackColor = Color.Green;
                            }
                            else
                            {
                                lab_Client.Text = "等待";
                                lab_Client.BackColor = Color.Red;
                            }
                        }
                        else
                        {
                            lab_Client.Text = "禁用";
                            lab_Client.BackColor = Color.Gray;
                        }
                        if (Parameters.commministion.TcpServerEnable)
                        {
                            if (TcpServer.TcpServerConnectResult)
                            {
                                lab_Server.Text = "在线";
                                lab_Server.BackColor = Color.Green;
                            }
                            else
                            {
                                lab_Server.Text = "等待";
                                lab_Server.BackColor = Color.Red;
                            }
                        }
                        else
                        {
                            lab_Server.Text = "禁用";
                            lab_Server.BackColor = Color.Gray;
                        }
                        if (AlarmList.Count > 0)
                        {
                            lab_log.Text = AlarmList[0];
                            AlarmList.RemoveAt(0);
                        }
                        if (Halcon.CamConnect[0])
                        {
                            lab_Cam1.Text = "在线";
                            lab_Cam1.BackColor = Color.Green;
                        }
                        else
                        {
                            lab_Cam1.Text = "等待";
                            lab_Cam1.BackColor = Color.Red;
                        }
                        if (Halcon.CamConnect[1])
                        {
                            lab_Cam2.Text = "在线";
                            lab_Cam2.BackColor = Color.Green;
                        }
                        else
                        {
                            lab_Cam2.Text = "等待";
                            lab_Cam2.BackColor = Color.Red;
                        }
                        if (Halcon.CamConnect[2])
                        {
                            lab_Cam3.Text = "在线";
                            lab_Cam3.BackColor = Color.Green;
                        }
                        else
                        {
                            lab_Cam3.Text = "等待";
                            lab_Cam3.BackColor = Color.Red;
                        }
                        //lab_Product.Text = Parameters.commministion.productName;
                    });
                    this.BeginInvoke(start);
                });
                task.Start();
            }

        }
       
        private void MainRun0()
        {
            while (true)
            {
                if (m_Pause)
                {
                    System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start(); //  开始监视代码运行时间

                    if (Halcon.CamConnect[0])
                    {
                        hImage[0].Dispose();
                        HOperatorSet.GrabImage(out hImage[0],Halcon.hv_AcqHandle[0]);
                        //Halcon.GrabImage(0,Halcon.hv_AcqHandle[0], out hImage[0]);
                    }
                    DetectionResult = true;
                    HOperatorSet.GetImageSize(hImage[0], out Halcon.hv_Height[0], out Halcon.hv_Width[0]);
                    HOperatorSet.DispObj(hImage[0], hWindows0[0]);
                    HOperatorSet.SetPart(hWindows0[0], 0, 0, -1, -1);
                    if (Parameters.specifications.SaveOrigalImage)
                    {
                        HOperatorSet.CopyImage(hImage[0], out hObjectIn[0]);
                    }
                    相机检测设置.Detection(0, hWindows0, MainForm.hImage[0], messageShow0, messageShow1);
                    if (Parameters.specifications.SaveDefeatImage)
                    {
                        HOperatorSet.DumpWindowImage(out hObjectOut[0], hWindows0[0]);
                    }
                    DateTime dtNow = System.DateTime.Now;  // 获取系统当前时间
                    strDateTime = dtNow.ToString("yyyyMMddHHmmss");
                    strDateTimeDay = dtNow.ToString("yyyy-MM-dd");
                    Push();
                    Thread th = new Thread(new ThreadStart(ThreadWork));
                    th.Start();
                    if (DetectionResult)
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 0);
                    }
                    else
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 1);
                    }
                    stopwatch.Stop(); //  停止监视
                    TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                    double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数           
                    AlarmList.Add(System.DateTime.Now.ToString() + "检测时间:" + milliseconds.ToString());
                }
            }
        }
        private void MainRun1()
        {
            while (true)
            {
                if (m_Pause)
                {
                    System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start(); //  开始监视代码运行时间

                    if (Halcon.CamConnect[1])
                    {
                        hImage[1].Dispose();
                        HOperatorSet.GrabImage(out hImage[1], Halcon.hv_AcqHandle[1]);
                        //Halcon.GrabImage(Halcon.hv_AcqHandle[1], out hImage[1]);
                    }
                    DetectionResult = true;
                    HOperatorSet.GetImageSize(hImage[1], out Halcon.hv_Height[1], out Halcon.hv_Width[1]);
                    HOperatorSet.DispObj(hImage[1], hWindows1[0]);
                    HOperatorSet.SetPart(hWindows1[0], 0, 0, -1, -1);
                    if (Parameters.specifications.SaveOrigalImage)
                    {
                        HOperatorSet.CopyImage(hImage[1], out hObjectIn[1]);
                    }
                    //相机检测设置.Detection(1, hWindows1, MainForm.hImage[1], messageShow2, messageShow3);
                    if (Parameters.specifications.SaveOrigalImage)
                    {
                        HOperatorSet.DumpWindowImage(out hObjectOut[1], hWindows1[0]);
                    }
                    DateTime dtNow = System.DateTime.Now;  // 获取系统当前时间
                    strDateTime = dtNow.ToString("yyyyMMddHHmmss");
                    strDateTimeDay = dtNow.ToString("yyyy-MM-dd");
                    Push();
                    Thread th = new Thread(new ThreadStart(ThreadWork));
                    th.Start();
                    if (DetectionResult)
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 0);
                    }
                    else
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 1);
                    }
                    stopwatch.Stop(); //  停止监视
                    TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                    double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数           
                    AlarmList.Add(System.DateTime.Now.ToString() + "检测时间:" + milliseconds.ToString());
                }
            }
        }

        private void MainRun2()
        {
            while (true)
            {
                if (m_Pause)
                {
                    System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start(); //  开始监视代码运行时间

                    if (Halcon.CamConnect[2])
                    {
                        hImage[2].Dispose();
                        HOperatorSet.GrabImage(out hImage[2], Halcon.hv_AcqHandle[2]);
                        //Halcon.GrabImage(Halcon.hv_AcqHandle[2], out hImage[2]);
                    }
                    DetectionResult = true;
                    HOperatorSet.GetImageSize(hImage[2], out Halcon.hv_Height[2], out Halcon.hv_Width[2]);
                    HOperatorSet.DispObj(hImage[2], hWindows2[0]);
                    HOperatorSet.SetPart(hWindows2[0], 0, 0, -1, -1);
                    if (Parameters.specifications.SaveOrigalImage)
                    {
                        HOperatorSet.CopyImage(hImage[2], out hObjectIn[2]);
                    }
                    //相机检测设置.Detection(2, hWindows2, MainForm.hImage[2], messageShow4, messageShow5);
                    if (Parameters.specifications.SaveDefeatImage)
                    {
                        HOperatorSet.DumpWindowImage(out hObjectOut[2], hWindows1[0]);
                    }
                    DateTime dtNow = System.DateTime.Now;  // 获取系统当前时间
                    strDateTime = dtNow.ToString("yyyyMMddHHmmss");
                    strDateTimeDay = dtNow.ToString("yyyy-MM-dd");
                    Push();
                    Thread th = new Thread(new ThreadStart(ThreadWork));
                    th.Start();
                    if (DetectionResult)
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 0);
                    }
                    else
                    {
                        HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Completion, 1);
                    }
                    stopwatch.Stop(); //  停止监视
                    TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                    double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数           
                    AlarmList.Add(System.DateTime.Now.ToString() + "检测时间:" + milliseconds.ToString());
                }
            }
        }

        private void btn_Close_System_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定关闭程序吗？", "软件关闭提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {

                XMLHelper.serialize<Parameters.Counts>(Parameters.counts, "Parameter/CountsParams.xml");
                //Parameter.specifications.右短端.value = 10;
                //XMLHelper.serialize<Parameter.Specifications>(Parameter.specifications, "Specifications.xml");
                myThread.Abort();


                LogHelper.Log.WriteInfo(System.DateTime.Now.ToString() + "软件关闭。");
                this.Close();
            }
        }

        private void btn_Minimizid_System_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            LogHelper.Log.WriteInfo(System.DateTime.Now.ToString() + "窗体最小化。");
            MainForm.AlarmList.Add(System.DateTime.Now.ToString() + "窗体最小化。");
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();//隐藏主窗体  
                LogHelper.Log.WriteInfo(System.DateTime.Now.ToString() + "主窗体隐藏。");
                MainForm.AlarmList.Add(System.DateTime.Now.ToString() + "主窗体隐藏。");
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)//当鼠标点击为左键时  
            {
                this.Show();//显示主窗体  
                LogHelper.Log.WriteInfo(System.DateTime.Now.ToString() + "主窗体恢复。");
                MainForm.AlarmList.Add(System.DateTime.Now.ToString() + "主窗体恢复。");
                this.WindowState = FormWindowState.Normal;//主窗体的大小为默认  
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Parameters.commministion.PlcEnable)
            {
                hslCommunication = new HslCommunication();
                Thread.Sleep(1000);
                if (HslCommunication.plc_connect_result)
                {
                    lab_PLCStatus.Text = "在线";
                    lab_PLCStatus.BackColor = Color.Green;
                }
                else
                {
                    lab_PLCStatus.Text = "离线";
                    lab_PLCStatus.BackColor = Color.Red;
                }
            }
            else
            {
                lab_PLCStatus.Text = "禁用";
                lab_PLCStatus.BackColor = Color.Gray;
            }

            if (Parameters.commministion.TcpClientEnable)
            {
                TcpClient tcpClientr = new TcpClient();
                Thread.Sleep(1000);
                if (TcpClient.TcpClientConnectResult)
                {
                    lab_Client.Text = "在线";
                    lab_Client.BackColor = Color.Green;
                }
                else
                {
                    lab_Client.Text = "等待";
                    lab_Client.BackColor = Color.Red;
                }
            }
            else
            {
                lab_Client.Text = "禁用";
                lab_Client.BackColor = Color.Gray;
            }

            if (Parameters.commministion.TcpServerEnable)
            {
                TcpServer tcpServer = new TcpServer();
                Thread.Sleep(1000);
                if (TcpServer.TcpServerConnectResult)
                {
                    lab_Server.Text = "在线";
                    lab_Server.BackColor = Color.Green;
                }
                else
                {
                    lab_Server.Text = "等待";
                    lab_Server.BackColor = Color.Red;
                }
            }
            else
            {
                lab_Server.Text = "禁用";
                lab_Server.BackColor = Color.Gray;
            }
            lab_Product.Text = Parameters.commministion.productName;
            UpdataUI();
            HOperatorSet.SetPart(hWindows0[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows0[1], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows0[2], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.DispObj(hImage[0], hWindows0[0]);
            //HOperatorSet.DispObj(hImage[0], hWindows0[1]);
            //HOperatorSet.DispObj(hImage[0], hWindows0[2]);

            HOperatorSet.SetPart(hWindows1[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows1[1], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows1[2], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.DispObj(hImage[1], hWindows1[0]);
            //HOperatorSet.DispObj(hImage[1], hWindows1[1]);
            //HOperatorSet.DispObj(hImage[1], hWindows1[2]);

            HOperatorSet.SetPart(hWindows2[0], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows2[1], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.SetPart(hWindows2[2], 0, 0, -1, -1);//设置窗体的规格
            HOperatorSet.DispObj(hImage[2], hWindows2[0]);
            //HOperatorSet.DispObj(hImage[2], hWindows2[1]);
            //HOperatorSet.DispObj(hImage[2], hWindows2[2]);

            
            LogHelper.Log.WriteInfo(System.DateTime.Now.ToString() + "初始化完成");
            MainForm.AlarmList.Add(System.DateTime.Now.ToString() + "初始化完成");
        }

        #region 点击panel控件移动窗口
        System.Drawing.Point downPoint;
        private void panel4_MouseDown(object sender, MouseEventArgs e)
        {
            downPoint = new System.Drawing.Point(e.X, e.Y);
        }
        private void panel4_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new System.Drawing.Point(this.Location.X + e.X - downPoint.X,
                    this.Location.Y + e.Y - downPoint.Y);
            }
        }
        #endregion
        private void btn_SettingMean_Click(object sender, EventArgs e)
        {
            通讯设置 flg = new 通讯设置();
            flg.ShowDialog();
        }

        //    Directory.CreateDirectory(string path);//在指定路径中创建所有目录和子目录，除非已经存在
        //    Directory.Delete(string path);//从指定路径删除空目录
        //    Directory.Delete(string path, bool recursive);//布尔参数为true可删除非空目录
        //    Directory.Exists(string path);//确定路径是否存在
        //    Directory.GetCreationTime(string path);//获取目录创建日期和时间
        //    Directory.GetCurrentDirectory();//获取应用程序当前的工作目录
        //    Directory.GetDirectories(string path);//返回指定目录所有子目录名称，包括路径
        //    Directory.GetFiles(string path);//获取指定目录中所有文件的名称，包括路径
        //    Directory.GetFileSystemEntries(string path);//获取指定路径中所有的文件和子目录名称
        //    Directory.GetLastAccessTime(string path);//获取上次访问指定文件或目录的时间和日期
        //    Directory.GetLastWriteTime(string path);//返回上次写入指定文件或目录的时间和日期
        //    Directory.GetParent(string path);//检索指定路径的父目录，包括相对路径和绝对路径
        //    Directory.Move(string soureDirName, string destName);//将文件或目录及其内容移到新的位置
        //    Directory.SetCreationTime(string path);//为指定的目录或文件设置创建时间和日期
        //    Directory.SetCurrentDirectory(string path);//将应用程序工作的当前路径设为指定路径
        //    Directory.SetLastAccessTime(string path);//为指定的目录或文件设置上次访问时间和日期
        //    Directory.SetLastWriteTime(string path);//为指定的目录和文件设置上次访问时间和日期


        private void btn_Start_Click(object sender, EventArgs e)
        {
            if (HslCommunication.plc_connect_result)
            {
                HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.StartAdd, 1);              
                HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.Trigger_Detection, 0);
            }
            else
            {
                MessageBox.Show("PLC链接异常，请检查!");
                return;
            }
                     
            if (Halcon.CamConnect[0])
            {
                MainThread[0] = new Thread(MainRun0);
                MainThread[0].IsBackground = true;
                MainThread[0].Start();
            }
            else
            {
                MessageBox.Show("相机" + 0 + "链接异常，请检查!");
                return;
            }
            if (Halcon.CamConnect[1])
            {
                MainThread[1] = new Thread(MainRun1);
                MainThread[1].IsBackground = true;
                MainThread[1].Start();
            }
            else
            {
                MessageBox.Show("相机" + 1 + "链接异常，请检查!");
                return;
            }
            if (Halcon.CamConnect[2])
            {
                MainThread[2] = new Thread(MainRun2);
                MainThread[2].IsBackground = true;
                MainThread[2].Start();
            }
            else
            {
                MessageBox.Show("相机" + 2 + "链接异常，请检查!");
                return;
            }
            m_Pause = true;
            Permission = "访客";
            UpdataUI();
            btn_Start.Enabled = false;
            btn_Stop.Enabled = true;
            btn_Connutius.Enabled = true;
            btn_Connect.Enabled = false;
            btn_SpecicationSetting.Enabled = false;
            btn_Login.Enabled = false;
            btn_Cam1.Enabled = false;
            btn_Cam2.Enabled = false;
            btn_Cam3.Enabled = false;
            btn_Close_System.Enabled = false;
        }


        private void btn_Stop_Click(object sender, EventArgs e)
        {
            if (HslCommunication.plc_connect_result)
            {
                HslCommunication._NetworkTcpDevice.Write(Parameters.plcParams.StartAdd, 0);
                btn_Start.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Connutius.Enabled = false;
                btn_Connect.Enabled = false;
                btn_SpecicationSetting.Enabled = false;
                btn_Login.Enabled = true;
                btn_Cam1.Enabled = false;
                btn_Cam2.Enabled = false;
                btn_Cam3.Enabled = false;
                btn_Close_System.Enabled = false;
            }
            else
            {
                MessageBox.Show("链接异常，请检查!");
            }
            for (int i = 0; i < 3; i++)
            {
                if (MainThread[ i] != null)
                {
                    MainThread[i].Abort();
                }
            }           
            //HivCam.StopGrab(0);
        }

        private void btn_Connutius_Click(object sender, EventArgs e)
        {
            if (btn_Connutius.Text == "暂停")
            {
                m_Pause = false;
                btn_Connutius.Text = "继续";
            }
            else
            {
                m_Pause = true;
                btn_Connutius.Text = "暂停";
            }

        }

     
        #region 任务队列

        public Func<int> Pop()
        {
            Monitor.Enter(m_obj);
            //Action ac = null;
            Func<int> ac = null;
            try
            {
                if (m_List.Count > 0)
                {
                    ac = m_List.Dequeue();
                }
                else
                {
                    //Monitor.Wait(m_obj);
                    //ac = m_List.Dequeue();
                }
            }
            finally
            {
                Monitor.Exit(m_obj);
            }
            return ac;
        }

        public void Push()
        {
            Monitor.Enter(m_obj);

            //Func<int> fuc = new Func<int>(ImgSaveIn);
            Func<int> fuc1 = new Func<int>(ImgSaveOut);
            //m_List.Enqueue(fuc);
            m_List.Enqueue(fuc1);
            Monitor.Pulse(m_obj);
            Monitor.Exit(m_obj);
        }

        public void ThreadWork()
        {
            while (!isExit)
            {
                Func<int> work = Pop();

                try
                {
                    if (work != null)
                    {
                        work();
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("error..");
                    return;
                }

            }
            Console.WriteLine("exit threadWork");
        }
        #endregion
        string strDateTime;
        string strDateTimeDay;
        private int ImgSaveIn()
        {
            #region 保存图片            
            // 文件命名规则

            return 0;
            #endregion
        }

        private int ImgSaveOut()
        {
            #region 保存图片
            // 文件命名规则
            for (int i = 0; i < 3; i++)            
            {
                if (Parameters.specifications.SaveDefeatImage)
                {
                    string stfFileNameOut = "Out_" + productSN +"_"+ strDateTime;  // 默认的图像保存名称
                    string pathOut = Parameters.commministion.ImageSavePath + "/" + strDateTimeDay + "/" + productSN;
                    if (!System.IO.Directory.Exists(pathOut))
                    {
                        System.IO.Directory.CreateDirectory(pathOut);//不存在就创建文件夹
                    }
                    HOperatorSet.WriteImage(hObjectOut[i], "jpg", 0, pathOut + stfFileNameOut + ".jpg");
                }
                if (Parameters.specifications.SaveOrigalImage)
                {
                    string stfFileNameIn = "/In_" + productSN + "_" + strDateTime;  // 默认的图像保存名称
                    string pathIn = Parameters.commministion.ImageSavePath + "/" + strDateTimeDay + "/" + productSN;
                    if (!System.IO.Directory.Exists(pathIn))
                    {
                        System.IO.Directory.CreateDirectory(pathIn);//不存在就创建文件夹
                    }
                    HOperatorSet.WriteImage(hObjectIn[i], "jpg", 0, pathIn + stfFileNameIn + ".jpg");
                }
            }
            

            return 0;
            #endregion
        }

        private void btn_检测设置1_Click(object sender, EventArgs e)
        {
            CamNum = 0;
            相机检测设置 flg = new 相机检测设置();
            flg.ShowDialog();
        }

        private void btn_检测设置2_Click(object sender, EventArgs e)
        {
            CamNum = 1;
            相机检测设置 flg = new 相机检测设置();
            flg.ShowDialog();
        }

        private void btn_检测设置3_Click(object sender, EventArgs e)
        {
            CamNum = 2;
            相机检测设置 flg = new 相机检测设置();
            flg.ShowDialog();
        }


        public static string Permission = "访客";
        void User_TransfEvent(string value)
        {
            Permission = value;
        }
        private void UpdataUI()
        {
            if (Permission == "开发员")
            {
                btn_Start.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Connutius.Enabled = true;
                btn_Login.Enabled = true;
                btn_Connect.Enabled = true;
                btn_SpecicationSetting.Enabled = true;
                btn_Cam1.Enabled = true;
                btn_Cam2.Enabled = true;
                btn_Cam3.Enabled = true;
                btn_Close_System.Enabled = true;
                btn_Minimizid_System.Enabled = true;
                lab_User.Text = Permission;
                btn_changeProduct.Enabled = true;
            }
            else if (Permission == "管理员")
            {
                btn_Start.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Connutius.Enabled = true;
                btn_Login.Enabled = true;
                btn_Connect.Enabled = true;
                btn_SpecicationSetting.Enabled = true;
                btn_Cam1.Enabled = true;
                btn_Cam2.Enabled = true;
                btn_Cam3.Enabled = true;
                btn_Close_System.Enabled = true;
                btn_Minimizid_System.Enabled = true;
                lab_User.Text = Permission;
                 btn_changeProduct.Enabled = true;
            }
            else if (Permission == "操作员")
            {
                btn_Start.Enabled = false;
                btn_Stop.Enabled = false;
                btn_Connutius.Enabled = false;
                btn_Login.Enabled = true;
                btn_Connect.Enabled = false;
                btn_SpecicationSetting.Enabled = false;
                btn_Cam1.Enabled = false;
                btn_Cam2.Enabled = false;
                btn_Cam3.Enabled = false;
                btn_Close_System.Enabled = false;
                btn_Minimizid_System.Enabled = false;
                lab_User.Text = Permission;
                btn_changeProduct.Enabled = false;
            }
            else
            {
                btn_Start.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Connutius.Enabled = true;
                btn_Connect.Enabled = false;
                btn_SpecicationSetting.Enabled = false;
                btn_Login.Enabled = true;
                btn_Cam1.Enabled = false;
                btn_Cam2.Enabled = false;
                btn_Cam3.Enabled = false;
                btn_Close_System.Enabled = false;
                btn_Minimizid_System.Enabled = false;
                lab_User.Text = Permission;
                btn_changeProduct.Enabled = false;
            }
        }
        private void btn_Login_Click(object sender, EventArgs e)
        {
            登陆界面 flg = new 登陆界面();
            flg.TransfEvent += User_TransfEvent;
            flg.ShowDialog();
            UpdataUI();
        }
        public static string Product = "55";
        void Product_TransfEvent(string value)
        {
            Product = value;
        }
        private void btn_changeProduct_Click(object sender, EventArgs e)
        {
            切换产品 flg = new 切换产品();
            flg.TransfEvent += Product_TransfEvent;
            flg.ShowDialog();
            lab_Product.Text = Product;
        }

        private void btn_SpecicationSetting_Click(object sender, EventArgs e)
        {

        }
    }
}