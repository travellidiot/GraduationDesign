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

    //using BoxRect = Tuple<float, float, float, float>;
    //using HistoTuple = Tuple<IHistogram<byte>, IHistogram<byte>, IHistogram<byte>>;

    public class BoxRect
    {
        private Tuple<float, float, float, float> box;
        
        public BoxRect(float left, float lower, float right, float upper)
        {
            this.box = new Tuple<float, float, float, float>(left, lower, right, upper);
        }

        public float Left { get { return this.box.Item1; } }
        public float Lower { get { return this.box.Item2; } }
        public float Right { get { return this.box.Item3; } }
        public float Upper { get { return this.box.Item4; } }
    }

    public class HistoTuple<THisto>
    {
        private Tuple<THisto, THisto, THisto> histoTuple;

        public HistoTuple(THisto upperPart, THisto lowerPart, THisto feetPart)
        {
            this.histoTuple = new Tuple<THisto, THisto, THisto>(upperPart, lowerPart, feetPart);
        }

        public THisto UpperBody { get { return this.histoTuple.Item1; } }
        public THisto LowerBody { get { return this.histoTuple.Item2; } }
        public THisto Feet { get { return this.histoTuple.Item3; } }
    }

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

        public void LoadBodies(Body[] kinectBodies)
        {
            if (this.bodies == null)
            {
                this.bodies = kinectBodies.Select(body => new BodyData(body)).ToArray();
            }
        }
        public void LoadSkeletonFile(string skeletonFileName)
        {
            using (BodyReader br = new BodyReader(File.Open(skeletonFileName, FileMode.Open)))
            {
                this.bodies = br.ReadAllBodies();
            }
        }
        
        public void LoadAllData(Bitmap kColorBimap, byte[] kDepthBytes, byte[] kDepthIndexBytes, Body[] kBodies)
        {
            LoadColorBitmap(kColorBimap);
            LoadDepthBytes(kDepthBytes);
            LoadDepthIndexBytes(kDepthIndexBytes);
            LoadBodies(kBodies);
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
                HistoTuple<IHistogram<byte>> tuple = ExtractHistograms(i);
                IHistogram<byte> hhisto = tuple.UpperBody;
                using (BinaryWriter fs = new BinaryWriter(File.Open(@"V:\GitHub\kinect-picking\GraduationDesign\Test\hist.txt", FileMode.Append)))
                {
                    string line = "Body " + i.ToString() + ":\r\n";
                    fs.Write(line);
                    for (byte bin = 0; bin < HueHisto.Dimension; bin++)
                    {
                        line = "bin " + bin.ToString() + ":\t" + hhisto[bin].ToString() + "\r\n";
                        fs.Write(line);
                    }
                }
            }
        }

        private DepthSpacePoint[] getJointsPosInDepthSpace(int bodyIndex)
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
        public BoxRect GetBox(DepthSpacePoint[] joints)
        {
            IEnumerable<float> coorX = Enumerable.Select<DepthSpacePoint, float>(joints, (j) => j.X);
            IEnumerable<float> coorY = Enumerable.Select<DepthSpacePoint, float>(joints, (j) => j.Y);

            var rect = new BoxRect(coorX.Min(), coorY.Max(), coorX.Max(), coorY.Min());

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

            return GetBox(upBodyJoints);
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

            return GetBox(downBodyJoints);
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

            return GetBox(leftFootJoints);
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

            return GetBox(leftFootJoints);
        }

        public HistoTuple<IHistogram<byte>> ExtractHistograms(int bodyIndex)
        {
            DepthSpacePoint[] jointsInDepthSpacePoints = getJointsPosInDepthSpace(bodyIndex);
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

            return new HistoTuple<IHistogram<byte>>(upBodyHisto, downBodyHisto, footHisto);
        }

        public HueHisto BodyPartHueHisto(int bodyIndex, BoxRect rect)
        {
            BitmapData bitmapData = this.colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height),
                ImageLockMode.ReadOnly, colorBitmap.PixelFormat);
            
            HueHisto hhisto = new HueHisto();

            //Debug.WriteLine(rect);

            unsafe
            {
                byte* p = (byte*)bitmapData.Scan0;

                int upper = (int)(rect.Upper + 0.5);
                int lower = (int)(rect.Lower + 0.5);
                int left = (int)(rect.Left + 0.5);
                int right = (int)(rect.Right + 0.5);

                Parallel.For(upper, lower, depthY =>
                {
                    for (int depthX = left; depthX < right; depthX++)
                    {
                        if ((depthX >= 0) && (depthX <= depthWidth) && (depthY >= 0) && (depthY <= depthHeight))
                        {
                            int depthIndex = depthY * depthWidth + depthX;
                            if (bodyIndexBytes[depthIndex] == bodyIndex)
                            {
                                int colorX = (int)(depthMappedToColorPoints[depthIndex].X + 0.5);
                                int colorY = (int)(depthMappedToColorPoints[depthIndex].Y + 0.5);
                                if (colorY >= bitmapData.Height)
                                    colorY = bitmapData.Height - 1;
                                if (colorX >= bitmapData.Width)
                                    colorX = bitmapData.Width - 1;
                                int index = colorY * bitmapData.Stride + colorX * 3;
                                int r = index, g = index + 1, b = index + 2;
                                // Debug.WriteLine("{0}, {1}, {2}", colorX, colorY, index);
                                float hue = ColorConvertor.Instance.GetHue(*(p + r), *(p + g), *(p + b));
                                byte bin = (byte)(hue / (360 / HueHisto.Dimension));
                                hhisto[bin]++;
                            }
                        }
                    }
                });
            }

            this.colorBitmap.UnlockBits(bitmapData);

            return hhisto;
        }
    }
}
