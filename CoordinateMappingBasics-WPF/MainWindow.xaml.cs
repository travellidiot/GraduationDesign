//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.CoordinateMappingBasics
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Runtime.InteropServices;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading.Tasks;
    using Microsoft.Kinect;
    using Emgu.CV;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;
    using Emgu.Util;
    using MongoDB.Driver;
    using MongoDB.Bson;
    
    using FeatureExtracter;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

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
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader multiFrameSourceReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;
        ///<summary>
        ///Bitmap to display the color picture the camera get
        ///</summary>
        private WriteableBitmap colorBitmap = null;
        /// <summary>
        /// Bitmap to display the man founded
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;

        /// <summary>
        /// The size in bytes of the bitmap back buffer
        /// </summary>
        //private uint bitmapBackBufferSize = 0;

        /// <summary>
        /// Intermediate storage for the color to depth mapping
        /// </summary>
        //private DepthSpacePoint[] colorMappedToDepthPoints = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private FrameDescription depthFrameDescription = null;
        private FrameDescription bodyIndexFrameDescription = null;
        VideoWriter videoWriter = null;

        private Body[] bodies = null;
        private byte[] depthBytes = null;
        private byte[] bodyIndexBytes = null;
        private uint[] bodyIndexPixels = null;
        private ulong[] exTrackingIds = null;
        private ulong[] nowTrackingIds = null;

        Dictionary<ulong, List<Tuple<int, float, HueHisto, WriteableBitmap>>> qualities = null;
        /// <summary>
        /// 
        /// </summary>
        //HueHisto hHist = new HueHisto();
        //Tuple<int, float, HueHisto> t = Tuple.Create<int, float, HueHisto>(100, 10.0f, hHist);
        //qualities[1292921] = t;

        int counter = 0;
        int maxCount = 15 * 30;

        private float qualityCalc(Body outbody, int index)
        {
            float score = 0;
            Dictionary<JointType, Joint> doj = outbody.Joints as Dictionary<JointType, Joint>;
            //Joint joints;
            foreach(Joint joint in doj.Values)
            {
                score += (int)joint.TrackingState;
            }
            return score;
        }

        private float trackingScore(Body outbody, int index)
        {
            float score = 0;
            Dictionary<JointType, Joint> doj = outbody.Joints as Dictionary<JointType, Joint>;
            //Joint joints;
            foreach (Joint joint in doj.Values)
            {
                score += (int)joint.TrackingState;
            }
            return score;
        }

        private float trackingScoreWeighted(Body outbody, int index)
        {
            float score = 0;
            Dictionary<JointType, Joint> doj = outbody.Joints as Dictionary<JointType, Joint>;
            //Joint joints;
            foreach (Joint joint in doj.Values)
            {
                score += (int)joint.TrackingState;
            }
            return score;
        }

        private float pixelCountScore()
        {
            float score = 0;
            return score;
        }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

            this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = this.depthFrameDescription.Width;
            int depthHeight = this.depthFrameDescription.Height;
            this.bodyIndexPixels = new uint[depthWidth * depthHeight];
            this.depthBytes = new byte[depthWidth * depthHeight * this.depthFrameDescription.BytesPerPixel];
            this.bodyIndexBytes = new byte[depthWidth * depthHeight];
            
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            //this.colorMappedToDepthPoints = new DepthSpacePoint[colorWidth * colorHeight];

            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.colorBitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            //this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width, this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Calculate the WriteableBitmap back buffer size
            //this.bitmapBackBufferSize = (uint)((this.bitmap.BackBufferStride * (this.bitmap.PixelHeight - 1)) + (this.bitmap.PixelWidth * this.bytesPerPixel));
                                   
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.kinectSensor.Open();

            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            this.DataContext = this;

            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        public ImageSource ImageSource1
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.multiFrameSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                this.multiFrameSourceReader.Dispose();
                this.multiFrameSourceReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }

            if (this.videoWriter != null)
            {
                this.videoWriter.Dispose();
                this.videoWriter = null;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e) 
        {
            string myPhotos = @"V:\GitHub\kinect-picking\GraduationDesign\Data";
            string bodyPath = Path.Combine(myPhotos, "BodyIndex-09-33-41.bi");
            string skeletonPath = Path.Combine(myPhotos, "SkeletonData-09-33-41.skt");
            string colorPath = Path.Combine(myPhotos, "ColorImage-09-33-41.png");
            string depthPath = Path.Combine(myPhotos, "DepthData-09-33-41.dp");

            FeatureExtracter extracter = new FeatureExtracter(this.kinectSensor);

            extracter.LoadColorFile(colorPath);
            int width = this.depthFrameDescription.Width;
            int height = this.depthFrameDescription.Height;
            extracter.LoadDepthFile(depthPath, width, height);
            extracter.LoadDepthIndexFile(bodyPath, width, height);
            extracter.LoadSkeletonFile(skeletonPath);

            extracter.Init();

            extracter.test();

            // Create a render target to which we'll render our composite image
            //RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)CompositeImage.ActualWidth, (int)CompositeImage.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            //DrawingVisual dv = new DrawingVisual();
            //using (DrawingContext dc = dv.RenderOpen())
            //{
            //    VisualBrush brush = new VisualBrush(CompositeImage);
            //    dc.DrawRectangle(brush, null, new Rect(new Point(), new Size(CompositeImage.ActualWidth, CompositeImage.ActualHeight)));
            //}

            //renderBitmap.Render(dv);

            //BitmapEncoder encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            //string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            //string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            //string path = Path.Combine(myPhotos, "KinectScreenshot-CoordinateMapping-" + time + ".png");

            //// Write the new file to disk
            //try
            //{
            //    using (FileStream fs = new FileStream(path, FileMode.Create))
            //    {
            //        encoder.Save(fs);
            //    }

            //    this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            //}
            //catch (IOException)
            //{
            //    this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            //}
        }

        /// <summary>
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private async void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            int depthWidth = 0;
            int depthHeight = 0;
                    
            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            BodyFrame bodyFrame = null;
            bool isBitmapLocked = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();           

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the bitmap back buffer.
            try
            {                
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();
                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                Task[] tasks = new Task[4];
                int videoIndex = this.counter;

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                // Lock the bitmap for writing
                this.bitmap.Lock();
                isBitmapLocked = true;
                uint size = (uint)((this.bitmap.BackBufferStride * (this.bitmap.PixelHeight - 1)) + (this.bitmap.PixelWidth * this.bytesPerPixel));
                colorFrame.CopyConvertedFrameDataToIntPtr(this.bitmap.BackBuffer, size, ColorImageFormat.Bgra);
                this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));


                #region Process Depth
                tasks[0] = Task.Factory.StartNew(() =>
                {
                    // Access the depth frame data directly via LockImageBuffer to avoid making a copy
                    using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                    {
                        //this.coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                        //    depthFrameData.UnderlyingBuffer,
                        //    depthFrameData.Size,
                        //    this.colorMappedToDepthPoints);
                        Marshal.Copy(depthFrameData.UnderlyingBuffer, this.depthBytes, 0, (int)depthFrameData.Size);

                    }
                });
                #endregion

                #region Process Color
                tasks[1] = Task.Factory.StartNew(() =>
                {
                    int videoWidth = colorFrame.FrameDescription.Width;
                    int videoHeight = colorFrame.FrameDescription.Height;

                    if (videoWriter != null)
                    {
                        Bitmap image = new Bitmap(videoWidth, videoHeight);
                        BitmapData imageData = image.LockBits(
                            new Rectangle(0, 0, videoWidth, videoHeight),
                            ImageLockMode.WriteOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                        colorFrame.CopyConvertedFrameDataToIntPtr(imageData.Scan0, size, ColorImageFormat.Rgba);
                        image.UnlockBits(imageData);

                        Image<Rgb, byte> im = new Image<Rgb, byte>(image);
                        videoWriter.WriteFrame(im);
                        counter++;

                        if (counter >= maxCount)
                        {
                            videoWriter.Dispose();
                            videoWriter = null;
                            counter = 0;
                        }
                        image.Dispose();
                    }
                    else
                    {
                        string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                        string myPhotos = @"V:\GitHub\kinect-picking\GraduationDesign\Data";
                        string videoPath = Path.Combine(myPhotos, "ColorVideo-" + time + ".avi");

                        videoWriter = new VideoWriter(videoPath, 30, videoWidth, videoHeight, true);
                    }
                });
                #endregion

                #region Process BodyIndex
                tasks[2] = Task.Factory.StartNew(() =>
                {
                    // We'll access the body index data directly to avoid a copy
                    using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                    {
                        Marshal.Copy(bodyIndexData.UnderlyingBuffer, this.bodyIndexBytes, 0, (int)bodyIndexData.Size);
                    }
                });
                #endregion

                #region Process Body
                tasks[3] = Task.Factory.StartNew(() =>
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    for (int i = 0; i < bodies.Length; i++)
                    {
                        float qualityScore = qualityCalc(bodies[i], i);
                        //Dictionary<ulong, Tuple<int, float, HueHisto>> qualities = null;
                        //Dictionary<ulong, Tuple<int, float, HueHisto>> qualities = null;
                        if (qualityScore != 0)
                        {
                            //如果包含该TrackingId,即人物出现过并仍在持续出现中
                            if (qualities.ContainsKey(bodies[i].TrackingId))
                            {
                                List<Tuple<int, float, HueHisto, WriteableBitmap>> list = qualities[bodies[i].TrackingId];
                                if (list.Count < 6)
                                {
                                    FeatureExtracter extracter = new FeatureExtracter(this.kinectSensor);
                                    Dictionary<JointType, Joint> joints = bodies[i].Joints as Dictionary<JointType, Joint>;
                                    CameraSpacePoint[] bodyJoints = new CameraSpacePoint[joints.Count];
                                    foreach (Joint j in joints.Values)
                                    {
                                        bodyJoints[(int)j.JointType] = j.Position;
                                    }
                                    DepthSpacePoint[] bodyDepthPoints = new DepthSpacePoint[joints.Count];
                                    coordinateMapper.MapCameraPointsToDepthSpace(bodyJoints, bodyDepthPoints);
                                    BoxRect rect = extracter.GetBox(bodyDepthPoints);
                                    HueHisto hue = extracter.BodyPartHueHisto(i, rect);

                                    Tuple<int, float, HueHisto, WriteableBitmap> newTuple = Tuple.Create<int, float, HueHisto, WriteableBitmap>(videoIndex, qualityScore, hue, bitmap);
                                    list.Add(newTuple);
                                }
                                else
                                {
                                    float min = list[0].Item2;
                                    int pos = 0;
                                    for (int j = 0; j < list.Count; j++)
                                    {
                                        if (list[j].Item2 < min)
                                        {
                                            min = list[j].Item2;
                                            pos = j;
                                        }
                                    }
                                    if (min < qualityScore)
                                    {
                                        FeatureExtracter extracter = new FeatureExtracter(this.kinectSensor);
                                        Dictionary<JointType, Joint> joints = bodies[i].Joints as Dictionary<JointType, Joint>;
                                        CameraSpacePoint[] bodyJoints = new CameraSpacePoint[joints.Count];
                                        foreach (Joint j in joints.Values)
                                        {
                                            bodyJoints[(int)j.JointType] = j.Position;
                                        }
                                        DepthSpacePoint[] bodyDepthPoints = new DepthSpacePoint[joints.Count];
                                        coordinateMapper.MapCameraPointsToDepthSpace(bodyJoints, bodyDepthPoints);
                                        BoxRect rect = extracter.GetBox(bodyDepthPoints);
                                        HueHisto hue = extracter.BodyPartHueHisto(i, rect);

                                        Tuple<int, float, HueHisto, WriteableBitmap> newTuple = Tuple.Create<int, float, HueHisto, WriteableBitmap>(videoIndex, qualityScore, hue, bitmap);
                                        list[pos] = newTuple;
                                    }
                                }
                            }
                            else
                            {
                                List<Tuple<int, float, HueHisto, WriteableBitmap>> list = qualities[bodies[i].TrackingId];
                                //阈值还没测试只是随便写的需要试验调整
                                float threshold_val = 10;
                                FeatureExtracter extracter = new FeatureExtracter(this.kinectSensor);
                                Dictionary<JointType, Joint> joints = bodies[i].Joints as Dictionary<JointType, Joint>;
                                CameraSpacePoint[] bodyJoints = new CameraSpacePoint[joints.Count];
                                foreach (Joint j in joints.Values)
                                {
                                    bodyJoints[(int)j.JointType] = j.Position;
                                }
                                DepthSpacePoint[] bodyDepthPoints = new DepthSpacePoint[joints.Count];
                                coordinateMapper.MapCameraPointsToDepthSpace(bodyJoints, bodyDepthPoints);
                                BoxRect rect = extracter.GetBox(bodyDepthPoints);
                                HueHisto hue = extracter.BodyPartHueHisto(i, rect);

                                Tuple<int, float, HueHisto, WriteableBitmap> newTuple = Tuple.Create<int, float, HueHisto, WriteableBitmap>(videoIndex, qualityScore, hue, bitmap);
                                int pos_flag = -1;
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (hue.DistanceTo(list[j].Item3) < threshold_val)
                                    {
                                        pos_flag = j;
                                        break;
                                    }
                                }
                                if (pos_flag >= 0)
                                {
                                    if (list[pos_flag].Item2 < qualityScore)
                                    {
                                        list[pos_flag] = newTuple;
                                    }
                                }
                                else
                                {
                                    list.Add(newTuple);
                                }
                            }
                            System.Diagnostics.Debug.WriteLine(bodies[i].TrackingId);
                            System.Diagnostics.Debug.Write(qualityScore);
                            System.Diagnostics.Debug.Write(" ");
                            System.Diagnostics.Debug.WriteLine(i);
                        }
                        //HueHisto hHist = new HueHisto();

                    }
                });
                #endregion

                //foreach(Body body in bodies)
                //{
                //    if(body.IsTracked)
                //    {
                //        System.Diagnostics.Debug.WriteLine(body.TrackingId);
                //    }
                //    else
                //    {
                //        System.Diagnostics.Debug.WriteLine("No body is tracked!");
                //    }
                //}
                // We're done with the DepthFrame 

                await Task.WhenAll(tasks);

                depthFrame.Dispose();
                depthFrame = null;

                colorFrame.Dispose();
                colorFrame = null;

                bodyFrame.Dispose();  
                bodyFrame = null;
            }
            finally
            {
                if (isBitmapLocked)
                {
                    this.bitmap.Unlock();
                }

                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                }

                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }
            }
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

        private void SaveDepthData(byte[] depthBytes, string depthPath)
        {
             using (BinaryWriter bw = new BinaryWriter(File.Open(depthPath, FileMode.Create)))
             {
                 bw.Write(depthBytes);
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
                    System.Diagnostics.Debug.Write(body);
                    IReadOnlyDictionary<JointType, Joint> Joints = body.Joints;
                    foreach (var joint in Joints.Values)
                    {
                        byte[] jointBytes = SBConvertor.Instance.StructToBytes(joint);
                        bw.Write(jointBytes);
                    }
                }
            }
        }

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
            SaveDepthData(bodyIndexBytes, bodyPath);

            // 保存深度数据
            SaveDepthData(depthBytes, depthPath);

            // 保存人体骨骼数据
            SaveSkeletonData(bodies, skeletonPath);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
