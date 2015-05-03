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
            string colorFile = @"C:\Users\koala\Documents\GitHub\GraduationDesign\Data\ColorImage.png";
            string depthFile = @"C:\Users\koala\Documents\GitHub\GraduationDesign\Data\DepthData.dp";
            string bodyIndexFile = @"C:\Users\koala\Documents\GitHub\GraduationDesign\Data\BodyIndex.bi";
            string skeletonFile = @"C:\Users\koala\Documents\GitHub\GraduationDesign\Data\SkeletonData.skt";

            FeatureExtracter extracter = new FeatureExtracter();
            extracter.LoadFiles(colorFile, depthFile, bodyIndexFile, skeletonFile);
            extracter.test();
        }
    }
}
