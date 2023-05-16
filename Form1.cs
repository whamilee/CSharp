using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge;
using AForge.Controls;
using AForge.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

using PaddleOCRSharp;


/// <summary>
/// 20230410 by AmiLee 
/// 关于原理：
/// C#调用摄像头+存储图片+Zxing/Zbar图片识别.当开启摄像头的时候利用Timer对当前图片进行解析处理，识别条码；
/// 关于条码解析：
/// 这个DEMO含两个条码解析组件，分别是Zxing和Zbar，使用哪个可以自己切换；
/// 关于作者：李和密
/// </summary>

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 20230410 by AmiLee 
    /// </summary>
    public partial class Form1 : Form
    {
        #region 全局变量定义
        FilterInfoCollection videoDevices;
        VideoCaptureDevice videoSource;
        Bitmap img;//处理图片
        public int selectedDeviceIndex = 0;   //选择摄像头 0：笔记本摄像头 1：USB摄像头
        #endregion

        public Form1()
        {
            InitializeComponent();
            InitializeView();
        }

        #region 事件
        /// <summary>
        /// 启动扫描(Start) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStart_Click(object sender, EventArgs e)
        {
            PbxScanner.Image = null;//初始画面
       
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            //selectedDeviceIndex = 0;
            videoSource = new VideoCaptureDevice(videoDevices[selectedDeviceIndex].MonikerString);//连接摄像头

            //videoSource.NewFrame += new NewFrameEventHandler(VspContainerClone);//捕获画面事件

            //videoSource.VideoResolution = videoSource.VideoCapabilities[selectedDeviceIndex];
            VspContainer.VideoSource = videoSource;
            VspContainer.Start();

            StartVideoSource();//按钮设定
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStop_Click(object sender, EventArgs e)
        {
            CloseVideoSource();
        }

        /// <summary>
        /// 保存
        /// 20230410 by AmiLee 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnScanner_Click(object sender, EventArgs e)
        {
            if (videoSource == null)
                return;
            Bitmap bitmap = VspContainer.GetCurrentVideoFrame();
            string fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ff") + ".jpg";

            bitmap.Save(Application.StartupPath + "\\" + fileName, ImageFormat.Jpeg);
            bitmap.Dispose();
        }

        /// <summary>
        /// 同步事件
        /// 20230410 by AmiLee
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void VspContainerClone(object sender, NewFrameEventArgs eventArgs)
        {
            //PbxScanner.Image = (Bitmap)eventArgs.Frame.Clone();
             PbxScanner.Image = VspContainer.GetCurrentVideoFrame();
        }

        /// <summary>
        /// Timer定时器
        /// 20230410 by AmiLee
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TmScanner_Tick(object sender, EventArgs e)
        {
            if (PbxScanner.Image != null)
            {
                TmScanner.Enabled = false;
                Bitmap img = (Bitmap)PbxScanner.Image.Clone();
                //解析二维码ByZxing
                //if (DecodeByZxing(img))

                //解析二维码ByZbar
                //if (DecodeByZbar(img))
                
                //OCR识别视的图片
                if (Ocr(img))
                {
                    CloseVideoSource();
                }
                else
                {
                    TmScanner.Enabled = true;
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 初始化
        /// 20190515 by hanfre 
        /// </summary>
        private void InitializeView()
        {
            //初始化将扫描及停止按钮禁用
            BtnScanner.Enabled = false;
            BtnStop.Enabled = false;
        }

        /// <summary>
        /// 启动
        /// 20190515 by hanfre 
        /// </summary>
        private void StartVideoSource()
        {
            TmScanner.Enabled = true;
            BtnStart.Enabled = false;
            BtnStop.Enabled = true;
            BtnScanner.Enabled = true;
        }
        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseVideoSource()
        {
            if (!(videoSource == null))
            {
                if (videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource = null;
                }
            }

            VspContainer.SignalToStop();
            //videoSourcePlayer1.Stop();
            //videoSourcePlayer1.Dispose();

            TmScanner.Enabled = false;
            BtnScanner.Enabled = false;
            BtnStart.Enabled = true;
            BtnStop.Enabled = false;
        }
        #endregion

        #region 方法/Zxing&Zbar
        /// <summary>
        /// 解码
        /// 20190515 by hanfre 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool DecodeByZxing(Bitmap b)
        {
            try
            {
                BarcodeReader reader = new BarcodeReader();
                reader.AutoRotate = true;

                Result result = reader.Decode(b);

                TxtScannerCode.Text = result.Text;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                TxtScannerCode.Text = "";
                return false;
            }

            return true;
        }

        private bool DecodeByZbar(Bitmap b)
        {
            DateTime now = DateTime.Now;

            Bitmap pImg = ZbarMakeGrayscale3(b);
            using (ZBar.ImageScanner scanner = new ZBar.ImageScanner())
            {
                scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 0);
                scanner.SetConfiguration(ZBar.SymbolType.CODE39, ZBar.Config.Enable, 1);
                scanner.SetConfiguration(ZBar.SymbolType.CODE128, ZBar.Config.Enable, 1);
                scanner.SetConfiguration(ZBar.SymbolType.QRCODE, ZBar.Config.Enable, 1);

                List<ZBar.Symbol> symbols = new List<ZBar.Symbol>();
                symbols = scanner.Scan((System.Drawing.Image)pImg);

                if (symbols != null && symbols.Count > 0)
                {
                    string result = string.Empty;
                    symbols.ForEach(s => result += "条码内容:" + s.Data + " 条码质量:" + s.Quality + Environment.NewLine);
                    MessageBox.Show(result);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 处理图片灰度
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static Bitmap ZbarMakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
               new float[][]
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
        #endregion

        private bool Ocr(Bitmap img)
        {
            OCRModelConfig config = null;
            OCRParameter oCRParameter = new OCRParameter();

            oCRParameter.cpu_math_library_num_threads = 12;
            oCRParameter.cls = false; //是否执行文字方向分类
            oCRParameter.use_angle_cls = false;//是否开启方向检测
            oCRParameter.det_db_score_mode = true;//是否使用多段线，即文字区域是用多段线还是用矩形，


            OCRResult ocrResult = new OCRResult();

            PaddleOCREngine engine = new PaddleOCREngine(config, oCRParameter);
            {
                ocrResult = engine.DetectText(img);
            }

            //识别到结果及处理
            if (ocrResult != null && ocrResult.ToString().Length!=0)
            {
                //MessageBox.Show(ocrResult.Text, "识别结果");
                TxtScannerCode.Text = ocrResult.Text; //显示出扫描的结果
                //result.Text = ocrResult.Text;
                return true;
            }

            //不再用OCR时，请把PaddleOCREngine释放
            engine.Dispose();
            return false;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            img = VspContainer.GetCurrentVideoFrame();//拍摄
            PbxScanner.Image = img;
        }
    }
}
