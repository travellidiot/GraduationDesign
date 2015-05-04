using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using Microsoft.Kinect;

namespace FeatureExtracter
{
    public class FeatureExtracter
    {
        private KinectSensor kinectSensor = null;
        private CoordinateMapper coordinateMapper = null;
        private ColorSpacePoint[] depthMappedToColorPoints = null;
        private CameraSpacePoint[][] jointsInCameraSpace = null;
        private byte[] depthBytes = null;
        private ushort[] depthFrameBytes = null;
        private byte[] bodyIndexBytes = null;
        private BodyData[] bodies = null;
        private Bitmap colorBitmap = null;

        private int depthWidth = 0;
        private int depthHeight = 0;

        public FeatureExtracter()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = kinectSensor.CoordinateMapper;
        }


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

            int bytesPerPixel = Marshal.SizeOf(typeof(ushort)) / Marshal.SizeOf(typeof(byte));
            using (BinaryReader br = new BinaryReader(File.Open(depthFileName, FileMode.Open)))
            {
                this.depthWidth = br.ReadInt32();
                this.depthHeight = br.ReadInt32();
                this.depthBytes = br.ReadBytes(this.depthWidth * this.depthHeight * bytesPerPixel);
                this.depthFrameBytes = new ushort[this.depthBytes.Length / 2];
                Buffer.BlockCopy(this.depthBytes, 0, this.depthFrameBytes, 0, this.depthBytes.Length);
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

            jointsInCameraSpace = new CameraSpacePoint[bodies.Length][];
            for (int i = 0; i < this.bodies.Length; i++)
            {
                BodyData body = this.bodies[i];
                if (body.TrackingId == 0)
                    continue;

                jointsInCameraSpace[i] = new CameraSpacePoint[body.Joints.Count];
                foreach (Joint joint in body.Joints.Values)
                {
                    jointsInCameraSpace[i][(int)joint.JointType] = joint.Position;
                    Debug.WriteLine("{0}, {1}, {2}", joint.Position.X, joint.Position.Y, joint.Position.Z);
                }
            }
            
            depthMappedToColorPoints = new ColorSpacePoint[this.depthFrameBytes.Length];
            coordinateMapper.MapDepthFrameToColorSpace(depthFrameBytes, depthMappedToColorPoints);
        }

        public void test()
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i].TrackingId == 0)
                {
                    continue;
                }

                Debug.WriteLine("{0}, {1}", i, bodies[i].TrackingId);
                HueHisto hhisto = UpBodyHueHisto(i);
                using (BinaryWriter fs = new BinaryWriter(File.Open(@"V:\GitHub\kinect-picking\GraduationDesign\Test\hist.txt", FileMode.Append)))
                {
                    string line = "Boddy " + i.ToString() + ":\r\n";
                    fs.Write(line);
                    for (byte bin = 0; bin < HueHisto.Dimension; bin++)
                    {
                        line = "bin " + bin.ToString() + ":\t" + hhisto[bin].ToString() + "\r\n";
                        fs.Write(line);
                    }
                }
            }
        }

        private DepthSpacePoint[] getJointsPosInColorSpace(int bodyIndex)
        {
            DepthSpacePoint[] jointsInDepthSpacePoints =new DepthSpacePoint[bodies[bodyIndex].Joints.Count];    
            coordinateMapper.MapCameraPointsToDepthSpace(jointsInCameraSpace[bodyIndex], jointsInDepthSpacePoints);

            return jointsInDepthSpacePoints;
        }

        /// <summary>
        /// 返回包围盒，Tuple值类型为<左边界，上边界，右边界，下边界>
        /// </summary>
        /// <param name="joints">需要做包围盒的节点数组</param>
        /// <returns></returns>
        private Tuple<float, float, float, float> getBox(DepthSpacePoint[] joints)
        {
            IEnumerable<float> coorX = Enumerable.Select<DepthSpacePoint, float>(joints, (j) => j.X);
            IEnumerable<float> coorY = Enumerable.Select<DepthSpacePoint, float>(joints, (j) => j.Y);

            var rect = Tuple.Create<float, float, float, float>(coorX.Min(), coorY.Max(), coorX.Max(), coorY.Min());

            return rect;
        }

        /// <summary>
        /// 取上半身包围盒，先这么着吧，结构都搭好了再说
        /// </summary>
        /// <param name="joints">人体所有节点</param>
        /// <returns></returns>
        private Tuple<float, float, float, float> getUpBodyBox(DepthSpacePoint[] joints)
        {
            DepthSpacePoint[] upBodyJoints = new DepthSpacePoint[]
            {
                joints[(int)JointType.Neck], 
                joints[(int)JointType.HandLeft], 
                joints[(int)JointType.HandRight],
                joints[(int)JointType.ShoulderLeft],
                joints[(int)JointType.ShoulderRight],
                joints[(int)JointType.SpineBase]
            };

            return getBox(upBodyJoints);
        }

        public HueHisto UpBodyHueHisto(int bodyIndex)
        {
            DepthSpacePoint[] jointsInDepthSpacePoints = getJointsPosInColorSpace(bodyIndex);
            Tuple<float, float, float, float> rect = getUpBodyBox(jointsInDepthSpacePoints);
            BitmapData bitmapData = this.colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height),
                ImageLockMode.ReadOnly, colorBitmap.PixelFormat);
            
            HueHisto hhisto = new HueHisto();

            Debug.WriteLine(rect);
            for (float i = rect.Item4; i < rect.Item2+1; i++)
            {
                for (float j = rect.Item1; j < rect.Item3+1; j++)
                {
                    int depthX = (int)(j + 0.5);
                    int depthY = (int)(i + 0.5);
                    if ((depthX >= 0) && (depthX <= depthWidth) && (depthY >= 0) && (depthY <= depthHeight))
                    {
                        int depthIndex = depthY * depthWidth + depthX;
                        if (bodyIndexBytes[depthIndex] == bodyIndex)
                        {
                            int colorX = (int)(depthMappedToColorPoints[depthIndex].X + 0.5);
                            int colorY = (int)(depthMappedToColorPoints[depthIndex].Y + 0.5);

                            unsafe
                            {
                                byte* p = (byte*)bitmapData.Scan0;
                                int index = colorY * bitmapData.Stride + colorX * 3;
                                int r = index, g = index + 1, b = index + 2;
                                float hue = ColorConvertor.Instance.GetHue(*(p + r), *(p + g), *(p + b));
                                byte bin = (byte)(hue / (360 / HueHisto.Dimension));
                                hhisto[bin]++;
                            }
                        }
                    }
                }
            }

            this.colorBitmap.UnlockBits(bitmapData);

            return hhisto;
        }
    }
}
