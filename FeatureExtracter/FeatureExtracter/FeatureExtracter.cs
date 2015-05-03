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
        private ColorSpacePoint[] depthMappedToColorPoints = null;
        private CameraSpacePoint[][] jointsInCameraSpace = null;
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
                }
            }

            depthMappedToColorPoints = new ColorSpacePoint[depthBytes.Length];
            unsafe
            {
                fixed (byte* scan0 = &depthBytes[0])
                {
                    IntPtr ptr = (IntPtr)scan0;
                    coordinateMapper.MapDepthFrameToColorSpaceUsingIntPtr(ptr,
                        (uint)depthBytes.Length, depthMappedToColorPoints);
                }
            }
        }

        public void test()
        {
            
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

        public IHistogram<byte> UpBodyHueHisto(int bodyIndex)
        {
            DepthSpacePoint[] jointsInDepthSpacePoints = getJointsPosInColorSpace(bodyIndex);
            Tuple<float, float, float, float> rect = getUpBodyBox(jointsInDepthSpacePoints);
            
            
            HueHisto hhisto = new HueHisto();
            unsafe
            {
                fixed (byte* scan0 = &bodyIndexBytes[0])
                {
                    for (float i = rect.Item4; i < rect.Item2; i++)
                    {
                        for (float j = rect.Item1; j < rect.Item3; j++)
                        {
                            
                        }
                    }
                }
            }
        }
    }
}
