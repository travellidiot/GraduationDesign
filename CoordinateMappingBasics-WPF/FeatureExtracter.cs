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

    using BoxRect = Tuple<float, float, float, float>;
    using HistoTuple = Tuple<IHistogram<byte>, IHistogram<byte>, IHistogram<byte>>;

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

        public FeatureExtracter(KinectSensor sensor)
        {
            this.kinectSensor = sensor;
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
        }

        public void LoadColorBitmap(Bitmap color)
        {
            this.colorBitmap = color;
        }
        public void LoadColorFile(string colorFileName)
        {
            this.colorBitmap = new Bitmap(colorFileName);
        }


        public void LoadDepthBytes(byte[] depth)
        {
            this.depthBytes = depth;
        }
        public void LoadDepthFile(string depthFileName, int width, int height)
        {
            int bytesPerPixel = Marshal.SizeOf(typeof(ushort)) / Marshal.SizeOf(typeof(byte));
            this.depthWidth = width;
            this.depthHeight = height;
            using (BinaryReader br = new BinaryReader(File.Open(depthFileName, FileMode.Open)))
            {
                this.depthBytes = br.ReadBytes(width * height * bytesPerPixel);
                this.depthFrameBytes = new ushort[width * height];
                Buffer.BlockCopy(this.depthBytes, 0, this.depthFrameBytes, 0, this.depthBytes.Length);
            }
        }

        public void LoadDepthIndexBytes(byte[] depthIndex)
        {
            this.bodyIndexBytes = depthIndex;
        }
        public void LoadDepthIndexFile(string depthIndexFileName, int width, int height)
        {
            using (BinaryReader br = new BinaryReader(File.Open(depthIndexFileName, FileMode.Open)))
            {
                this.bodyIndexBytes = br.ReadBytes(width * height);
            }
        }

        public void LoadBodies(Body[] bodies)
        {

        }
        public void LoadSkeletonFile(string skeletonFileName)
        {
            using (BodyReader br = new BodyReader(File.Open(skeletonFileName, FileMode.Open)))
            {
                this.bodies = br.ReadAllBodies();
            }
        }
        

        public void Init()
        {
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
                    //Debug.WriteLine("{0}, {1}, {2}", joint.Position.X, joint.Position.Y, joint.Position.Z);
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

                //Debug.WriteLine("{0}, {1}", i, bodies[i].TrackingId);
                HistoTuple tuple = ExtractHistograms(i);
                IHistogram<byte> hhisto = tuple.Item1;
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
        private BoxRect getBox(DepthSpacePoint[] joints)
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
        private BoxRect getUpBodyBox(DepthSpacePoint[] joints)
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

        private BoxRect getDownBodyBox(DepthSpacePoint[] joints)
        {
            DepthSpacePoint[] downBodyJoints = new DepthSpacePoint[]
            {
                joints[(int)JointType.HipLeft],
                joints[(int)JointType.KneeLeft],
                joints[(int)JointType.AnkleLeft],
                joints[(int)JointType.HipRight],
                joints[(int)JointType.KneeRight],
                joints[(int)JointType.AnkleRight]
            };

            return getBox(downBodyJoints);
        }

        private BoxRect getLeftFootBox(DepthSpacePoint[] joints)
        {
            DepthSpacePoint ankleLeft = joints[(int)JointType.AnkleLeft];
            DepthSpacePoint footLeft = joints[(int)JointType.FootLeft];
            DepthSpacePoint footTipLeft;
            footTipLeft.X = 2 * footLeft.X - ankleLeft.X;
            footTipLeft.Y = 2 * footLeft.Y - ankleLeft.Y;

            DepthSpacePoint[] leftFootJoints = new DepthSpacePoint[]
            {
                ankleLeft, footLeft, footTipLeft
            };

            return getBox(leftFootJoints);
        }

        private BoxRect getRightFootBox(DepthSpacePoint[] joints)
        {
            DepthSpacePoint ankleRight = joints[(int)JointType.AnkleRight];
            DepthSpacePoint footRight = joints[(int)JointType.FootRight];
            DepthSpacePoint footTipRight;
            footTipRight.X = 2 * footRight.X - ankleRight.X;
            footTipRight.Y = 2 * footRight.Y - ankleRight.Y;

            DepthSpacePoint[] leftFootJoints = new DepthSpacePoint[]
            {
                ankleRight, footRight, footTipRight
            };

            return getBox(leftFootJoints);
        }

        public HistoTuple ExtractHistograms(int bodyIndex)
        {
            DepthSpacePoint[] jointsInDepthSpacePoints = getJointsPosInColorSpace(bodyIndex);
            BoxRect upBodyRect = getUpBodyBox(jointsInDepthSpacePoints);
            //BoxRect downBodyRect = getDownBodyBox(jointsInDepthSpacePoints);
            //BoxRect leftFootRect = getLeftFootBox(jointsInDepthSpacePoints);
            //BoxRect rightFootRect = getRightFootBox(jointsInDepthSpacePoints);

            IHistogram<byte> upBodyHisto = BodyPartHueHisto(bodyIndex, upBodyRect);
            //IHistogram<byte> downBodyHisto = BodyPartHueHisto(bodyIndex, downBodyRect);

            //IHistogram<byte> leftFootHisto = BodyPartHueHisto(bodyIndex, leftFootRect);
            //IHistogram<byte> rightFootHisto = BodyPartHueHisto(bodyIndex, rightFootRect);
            //IHistogram<byte> footHisto = leftFootHisto.Merge(rightFootHisto);
            //leftFootHisto = null;
            //rightFootHisto = null;

            HueHisto downBodyHisto = null;
            HueHisto footHisto = null;

            upBodyHisto.Norm();
            //downBodyHisto.Norm();
            //footHisto.Norm();

            return Tuple.Create<IHistogram<byte>, IHistogram<byte>, IHistogram<byte>>(upBodyHisto, downBodyHisto, footHisto);
        }

        private HueHisto BodyPartHueHisto(int bodyIndex, BoxRect rect)
        {
            BitmapData bitmapData = this.colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height),
                ImageLockMode.ReadOnly, colorBitmap.PixelFormat);
            
            HueHisto hhisto = new HueHisto();

            //Debug.WriteLine(rect);

            unsafe
            {
                byte* p = (byte*)bitmapData.Scan0;

                for (float i = rect.Item4; i < rect.Item2; i++)
                {
                    for (float j = rect.Item1; j < rect.Item3; j++)
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
                                if (colorY >= bitmapData.Height)
                                    colorY = bitmapData.Height-1;
                                if (colorX >= bitmapData.Width)
                                    colorX = bitmapData.Width-1;
                                int index = colorY * bitmapData.Stride + colorX * 3;
                                int r = index, g = index + 1, b = index + 2;
                                Debug.WriteLine("{0}, {1}, {2}", colorX, colorY, index);
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
