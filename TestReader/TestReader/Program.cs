using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Kinect;

namespace TestReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string bodyPath = Path.Combine(myPhotos, "BodyIndex.bi");
            string skeletonPath = Path.Combine(myPhotos, "SkeletonData.skt");
            string colorPath = Path.Combine(myPhotos, "ColorImage.png");

            try
            {
                // 测试bodyIndex数据读取
                using (BinaryReader br = new BinaryReader(File.Open(bodyPath, FileMode.Open)))
                {
                    int size = br.ReadInt32();
                    Console.WriteLine("Tht size: {0}", size);
                    for (int i = 0; i < size; i++)
                    {
                        byte bt = br.ReadByte();
                        Console.Write("{0} ", bt);
                    }
                    Console.Write("\n\n\n");
                }

                // 测试骨骼数据读取
                using (BinaryReader br = new BinaryReader(File.Open(skeletonPath, FileMode.Open)))
                {
                    while (true)
                    {
                        ulong idx = br.ReadUInt64();
                        Console.WriteLine("Body Index: {0}", idx);

                        int size = Body.JointCount;
                        for (int i = 0; i < size; i++)
                        {
                            int jointSize = Marshal.SizeOf(typeof(Joint));
                            byte[] jointBytes = br.ReadBytes(jointSize);
                            Object o = SBConvertor.Instance.BytesToStruct(jointBytes, typeof(Joint));

                            Joint joint;
                            if (o != null)
                            {
                                joint = (Joint)o;
                                CameraSpacePoint point = joint.Position;
                                Console.WriteLine("type:{0}, state:{1}, position:({2},{3},{4})", joint.JointType, joint.TrackingState, point.X, point.Y, point.Z);                            
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                Console.Write("\n");
                Console.WriteLine("Reach the end of file!");
            }
            catch (IOException)
            {
                Console.WriteLine("Something wrong with IO");
            }
        }
    }
}
