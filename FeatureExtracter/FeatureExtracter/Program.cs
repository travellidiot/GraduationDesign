using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;

namespace FeatureExtracter
{
    class Program
    {
        
        static void Main(string[] args)
        {
            string colorFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\ColorImage-05-39-15.png";
            string depthFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\DepthData-05-39-15.dp";
            string bodyIndexFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\BodyIndex-05-39-15.bi";
            string skeletonFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\SkeletonData-05-39-15.skt";

            //FeatureExtracter extracter = new FeatureExtracter();
            //extracter.LoadFiles(colorFile, depthFile, bodyIndexFile, skeletonFile);
            //extracter.test();
            KinectSensor sensor = KinectSensor.GetDefault();
            CoordinateMapper mapper = sensor.CoordinateMapper;
            MultiSourceFrameReader reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                FrameSourceTypes.Depth | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            reader.MultiSourceFrameArrived += reader_MultiSourceFrameArrived;

            sensor.Open();
            if (sensor.IsAvailable)
                Console.WriteLine("YYYYYYYY");

            CameraSpacePoint cp = new CameraSpacePoint();
            cp.X = -0.5f;
            cp.Y = -0.2f;
            cp.Z = 1.1f;
            DepthSpacePoint dp = mapper.MapCameraPointToDepthSpace(cp);
            Console.WriteLine("{0}, {1}", dp.X, dp.Y);
            sensor.Close();
        }

        static void reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            Console.WriteLine("arraived");
        }
    }
}
