//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.CoordinateMappingBasics
{
    using System;
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
    using Microsoft.Kinect;
    using Emgu.CV;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;
    using Emgu.Util;
    
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
        VideoWriter videoWriter = null;

        private Body[] bodies = null;
        private byte[] depthBytes = null;
        private byte[] bodyIndexBytes = null;
        private uint[] bodyIndexPixels = null;

        int counter = 0;
        int maxCount = 15 * 30;

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
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
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

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                // Process Depth
                // Access the depth frame data directly via LockImageBuffer to avoid making a copy
                using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                {
                    //this.coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                    //    depthFrameData.UnderlyingBuffer,
                    //    depthFrameData.Size,
                    //    this.colorMappedToDepthPoints);
                    Marshal.Copy(depthFrameData.UnderlyingBuffer, this.depthBytes, 0, (int)depthFrameData.Size);
                    
                }
                // We're done with the DepthFrame 
                depthFrame.Dispose();
                depthFrame = null;


                // Process Color
                this.bitmap.Lock();
                // Lock the bitmap for writing
                isBitmapLocked = true;
                uint size = (uint)((this.bitmap.BackBufferStride * (this.bitmap.PixelHeight - 1)) + (this.bitmap.PixelWidth * this.bytesPerPixel));
                colorFrame.CopyConvertedFrameDataToIntPtr(this.bitmap.BackBuffer, size, ColorImageFormat.Bgra);
                int videoWidth = colorFrame.FrameDescription.Width;
                int videoHeight = colorFrame.FrameDescription.Height;

                if (videoWriter != null)
                {
                    Bitmap image = new Bitmap(videoWidth, videoHeight);
                    BitmapData imageData = image.LockBits(new Rectangle(0, 0, videoWidth, videoHeight),
                        ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
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
                    string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                    string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string path = Path.Combine(myPhotos, "test-" + time + ".avi");
                    videoWriter = new VideoWriter(path, 30, videoWidth, videoHeight, true);
                }
                // We're done with the ColorFrame 
                colorFrame.Dispose();
                colorFrame = null;

                // We'll access the body index data directly to avoid a copy
                using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                {
                    Marshal.Copy(bodyIndexData.UnderlyingBuffer, this.bodyIndexBytes, 0, (int)bodyIndexData.Size);
                }


                // Process Body
                if (this.bodies == null)
                {
                    this.bodies = new Body[bodyFrame.BodyCount];
                }
                bodyFrame.GetAndRefreshBodyData(this.bodies);
                // Done with the BodyFrame
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
