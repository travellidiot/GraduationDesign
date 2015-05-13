//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//retry
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// 定义交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;
        
        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// BodyIndexFrame是识别的限制，kinect自定就可以识别最多六个人
        /// </summary>
        private static readonly uint[] BodyColor =
        {
            ///ARGB色彩(虽然创建时候写的是BGRA)，并非网上常见的ABGR格式，需要注意一下
            ///kinect最多可以同时识别六个人，也有且仅能识别六个人，所以无法应用在人流量密集的地方，人被识别出来的时候，
            ///所给的编号是随机的，所以并不是严格按照从0到5的顺序来分配序号
            ///绿色
            0x0000FF00,
            ///红色
            0x00FF0000,
            ///橙色
            0xFFFF4000,
            ///黄色
            0x40FFFF00,
            ///嫩绿
            0xFF40FF00,
            ///淡黄
            0xFF808000,
        };

        /// <summary>
        /// Active Kinect sensor
        /// 必要代码，启动kinectSensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// Reader section
        /// <summary>
        /// Reader for multisource frames
        /// </summary>
        //private BodyIndexFrameReader bodyIndexFrameReader = null;
        private MultiSourceFrameReader multiSourceFrameReader = null;

        /// <summary>
        /// Description of the data contained in the body index frame
        /// </summary>
        private FrameDescription bodyIndexFrameDescription = null;
        private FrameDescription depthFrameDescription = null;

        /// <summary>
        /// Bitmap to display the man founded
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;

        ///<summary>
        ///Bitmap to display the color picture the camera get
        ///</summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] bodyIndexPixels = null;

        /// <summary>
        /// 储存bodyIndex数据流，完成人的归并后，将数据流存入这个数组里面
        /// </summary>
        private byte[] bodyIndexBytes = null;

        /// <summary>
        /// 储存body信息（包含骨骼），以后要使用处理归并过之后的高质量的数据。现在暂时测试数据直接使用Kinect采集下来的数据，没有经过处理。
        /// </summary>
        private Body[] bodies = null;

        private ulong[] trackingIds = null;

        private int count = 0;

        private byte[] depthBytes = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText_body = null;
        private string statusText_color = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();
            
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                                                                                       FrameSourceTypes.BodyIndex |
                                                                                       FrameSourceTypes.Body);

            this.multiSourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            
            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display color
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being converted
            this.bodyIndexPixels = new uint[this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height];
            this.depthBytes = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height * this.depthFrameDescription.BytesPerPixel];
            // create the bitmap to display
            this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width, this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text of body sensor
            this.statusText_body = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // set the status text of color sensor
            this.statusText_color = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.NoSensorStatusText;


            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// 多格式数据流的处理事件，暂时顺序处理，以后为提高性能，可以将三个格式的数据流并发处理。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            

            // 处理颜色图像
            using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }

            // 处理Body数据，包含骨骼信息
            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    if (this.trackingIds == null)
                    {
                        this.trackingIds = new ulong[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                }
            }

            // 处理深度图像
            using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    System.Diagnostics.Debug.WriteLine("Null dpF!");
                }
                else
                {
                    unsafe
                    {
                        KinectBuffer buffer = depthFrame.LockImageBuffer();
                        ushort* begin = (ushort*)buffer.UnderlyingBuffer;
                        for (int i = 0; i < buffer.Size; i++)
                        {
                            //System.Diagnostics.Debug.WriteLine(begin[i]);
                            System.Diagnostics.Debug.WriteLine("Not null!");
                            System.Diagnostics.Debug.WriteLine(begin[i]);
                            //Console.WriteLine(begin[i]);
                        }
                    }
                    depthFrameDescription = depthFrame.FrameDescription;
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        Marshal.Copy(depthBuffer.UnderlyingBuffer, this.depthBytes, 0, (int)depthBuffer.Size);
                        unsafe
                        {
                            ushort* ptr = (ushort*)depthBuffer.UnderlyingBuffer;
                            for (int i = 0; i < depthBuffer.Size / 2; i++)
                            {
                                if (ptr[i] != 0)
                                {
                                    Debug.WriteLine("YYYYYY");
                                }
                            }
                        }
                    }
                }
            }

            bool bodyIndexFrameProcessed = false;

            // 处理BodyIndex数据流
            using (BodyIndexFrame bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height) == bodyIndexBuffer.Size) &&
                            (this.bodyIndexFrameDescription.Width == this.bodyIndexBitmap.PixelWidth) && (this.bodyIndexFrameDescription.Height == this.bodyIndexBitmap.PixelHeight))
                        {
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            this.bodyIndexBytes = new byte[bodyIndexBuffer.Size];
                            Marshal.Copy(bodyIndexBuffer.UnderlyingBuffer, this.bodyIndexBytes, 0, (int)bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }
                    }
                }
            }

            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
            }
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource1
        {
            get
            {
                return this.bodyIndexBitmap;
            }
        }

        public ImageSource ImageSource2
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText_body
        {
            get
            {
                return this.statusText_body;
            }

            set
            {
                if (this.statusText_body != value)
                {
                    this.statusText_body = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText_body"));
                    }
                }
            }
        }

        public string StatusText_color
        {
            get
            {
                return this.statusText_color;
            }

            set
            {
                if (this.statusText_color != value)
                {
                    this.statusText_color = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText_color"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.multiSourceFrameReader != null)
            {
                this.multiSourceFrameReader.MultiSourceFrameArrived -= this.Reader_MultiSourceFrameArrived;
                this.multiSourceFrameReader.Dispose();
                this.multiSourceFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// 保存截图
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.bodyIndexBitmap != null && this.bodyIndexBytes != null && this.bodies != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                //BitmapEncoder encoder = new PngBitmapEncoder();

                //// create frame from the writable bitmap and add to encoder
                //encoder.Frames.Add(BitmapFrame.Create(this.bodyIndexBitmap));

                string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-BodyIndex-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    //using (FileStream fs = new FileStream(path, FileMode.Create))
                    //{
                    //    encoder.Save(fs);
                    //}
                    BitmapFrame colorFrame = BitmapFrame.Create(this.colorBitmap);
                    SaveMultiSourceData(colorFrame, this.depthBytes, this.bodyIndexBytes, this.bodies);

                    this.StatusText_body = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText_body = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

       
        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// 重新计算bodyIndexFrame
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;
            int bodyPixelCount = 0;

            if(bodies == null)
            {
                //System.Diagnostics.Debug.WriteLine("Bodies is null!!");
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("Not null " + count++);
                for (int i = 0; i < bodies.Length; i++)
                {
                    trackingIds[i] = bodies[i].TrackingId;
                    //System.Diagnostics.Debug.WriteLine("Not null " + trackingIds[i]);
                }
            }
            

                // convert body index to a visual representation
                for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
                {
                    // the BodyColor array has been sized to match
                    // BodyFrameSource.BodyCount
                    if (frameData[i] < BodyColor.Length)
                    {
                        //System.Diagnostics.Debug.WriteLine(frameData[i]);
                        this.bodyIndexPixels[i] = BodyColor[frameData[i]];
                        bodyPixelCount++;
                    }
                    else
                    {
                        this.bodyIndexPixels[i] = 0x00000000;
                    }
                }

            //if (bodyPixelCount > 100)
            //{
            //    byte[] bodyIndexBytes = new byte[bodyIndexFrameDataSize];
            //    Marshal.Copy(bodyIndexFrameData, bodyIndexBytes, 0, (int)bodyIndexFrameDataSize);
            //    BitmapFrame colorFrame = BitmapFrame.Create(this.colorBitmap);
            //    TEST_SaveData(colorFrame, bodyIndexBytes);
            //}
        }


        /// <summary>
        /// 以后每一帧数据处理完成后，将彩色图像，BodyIndex数据流，和骨骼数据保存，用于离线处理
        /// </summary>
        /// <param name="colorFrame">颜色图像</param>
        /// <param name="bodyIndexBytes"></param>
        /// <param name="bodies"></param>
        private void SaveMultiSourceData(BitmapFrame colorFrame, byte[] depthBytes, byte[] bodyIndexBytes, Body[] bodies)
        {
            string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = @"V:\GitHub\kinect-picking\GraduationDesign\Data";
            string bodyPath = Path.Combine(myPhotos, "BodyIndex-" + time + ".bi");
            string skeletonPath = Path.Combine(myPhotos, "SkeletonData-" + time + ".skt");
            string colorPath = Path.Combine(myPhotos, "ColorImage-" + time + ".png");
            string depthPath = Path.Combine(myPhotos, "DepthData-" + time + ".dp");

            // 保存彩色图像
            SaveColorData(colorFrame, colorPath);

            // 保存人编号数据流
            SaveBodyIndexData(bodyIndexBytes, bodyPath);

            // 保存人体骨骼数据
            SaveSkeletonData(bodies, skeletonPath);

            // 保存深度数据
            int width = this.depthFrameDescription.Width;
            int height = this.depthFrameDescription.Height;
            SaveDepthData(depthBytes, width, height, depthPath);
        }
        
        
        private void SaveColorData(BitmapFrame colorFrame, string colorPath)
        {
            using (FileStream fs = new FileStream(colorPath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(colorFrame);
                encoder.Save(fs);
            }
        }

        private void SaveDepthData(byte[] depthBytes, int width, int height, string depthPath)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(depthPath, FileMode.Create)))
            {
                
                bw.Write(width);
                bw.Write(height);
                bw.Write(depthBytes);
            }
        }

        private void SaveBodyIndexData(byte[] bodyIndexBytes, string bodyPath)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(bodyPath, FileMode.Create)))
            {
                bw.Write(bodyIndexBytes.Length);
                bw.Write(bodyIndexBytes);
            }
        }
        
        private void SaveSkeletonData(Body[] bodies, string skeletonPath)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(skeletonPath, FileMode.Create)))
            {
                foreach (Body body in bodies)
                {
                    ulong idx = body.TrackingId;
                    bw.Write(idx);



                    IReadOnlyDictionary<JointType, Joint> Joints = body.Joints;
                    foreach (var joint in Joints.Values)
                    {
                        byte[] jointBytes = SBConvertor.Instance.StructToBytes(joint);
                        bw.Write(jointBytes);
                    }
                }
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// 修改bodyIndexBitmap的内容
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            //Console.WriteLine(this.bodyIndexBitmap.PixelWidth);
            //System.Diagnostics.Debug.WriteLine(this.bodyIndexBitmap.PixelWidth);
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText_body = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
