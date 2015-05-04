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
            string colorFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\ColorImage-05-39-11.png";
            string depthFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\DepthData-05-39-11.dp";
            string bodyIndexFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\BodyIndex-05-39-11.bi";
            string skeletonFile = @"V:\GitHub\kinect-picking\GraduationDesign\Data\SkeletonData-05-39-11.skt";

            //FeatureExtracter extracter = new FeatureExtracter();
            //extracter.LoadFiles(colorFile, depthFile, bodyIndexFile, skeletonFile);
            //extracter.test();
            KinectSensor sensor = KinectSensor.GetDefault();
            CoordinateMapper mapper = sensor.CoordinateMapper;
            CameraSpacePoint cp = new CameraSpacePoint();
            cp.X = 32f;
            cp.Y = 33f;
            cp.Z = 332f;
            DepthSpacePoint dp = mapper.MapCameraPointToDepthSpace(cp);
            Console.WriteLine("{0}, {1}", dp.X, dp.Y);
        }
    }
}
