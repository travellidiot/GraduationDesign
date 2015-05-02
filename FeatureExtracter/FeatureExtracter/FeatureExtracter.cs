using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;

namespace FeatureExtracter
{
    public class FeatureExtracter
    {
        private CoordinateMapper coordinateMapper = null;
        private ColorSpacePoint[] colorMappedToDepthPoints = null;
        private byte[] depthBytes = null;
        private byte[] bodyIndexBytes = null;
        private BodyData[] bodies = null;
        private Bitmap colorBitmap = null;

        private int depthWidth = 0;
        private int depthHeight = 0;

        /// <summary>
        /// 载入提取特征需要的文件（彩色图像，深度数据，BodyIndex数据，骨骼数据）
        /// </summary>
        /// <param name="colorFileName">彩色图像</param>
        /// <param name="depthFileName">深度图像</param>
        /// <param name="depthIndexFileName">Body Index 图像</param>
        /// <param name="skeletonFileName">骨骼数据</param>
        public void LoadFiles(string colorFileName, string depthFileName,
                            string depthIndexFileName, string skeletonFileName)
        {
            colorBitmap = new Bitmap(colorFileName);

            using (BinaryReader br = new BinaryReader(File.Open(depthFileName, FileMode.Open)))
            {
                this.depthWidth = br.ReadInt32();
                this.depthHeight = br.ReadInt32();
                this.depthBytes = br.ReadBytes(this.depthWidth * this.depthHeight);
            }

            using (BinaryReader br = new BinaryReader(File.Open(depthIndexFileName, FileMode.Open)))
            {
                int size = br.ReadInt32();
                this.bodyIndexBytes = br.ReadBytes(size);
            }

            using (BodyReader br = new BodyReader(File.Open(skeletonFileName, FileMode.Open)))
            {
                this.bodies = br.ReadAllBodies();
            }
        }

        public void test()
        {
            unsafe
            {
                fixed (byte* p = (&this.depthBytes[0]))
                {
                    
                }
            }
        }
    }
}
