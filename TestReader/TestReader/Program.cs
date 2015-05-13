using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;

namespace TestReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string myPhotos = @"V:\GitHub\kinect-picking\GraduationDesign\Data";
            string bodyPath = Path.Combine(myPhotos, "BodyIndex.bi");
            string skeletonPath = Path.Combine(myPhotos, "SkeletonData.skt");
            string colorPath = Path.Combine(myPhotos, "ColorImage.png");
            string depthPath = Path.Combine(myPhotos, "DepthData.dp");

            try
            {
                // 测试彩色数据
                //using (FileStream fs = new FileStream(colorPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                //{
                //    Bitmap img = new Bitmap(fs);
                //    if (!img.RawFormat.Equals(ImageFormat.Png))
                //    {
                //        throw new Exception("Wrong Image Format!!!");
                //    }

                //    Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
                //    var data = img.LockBits(rect, ImageLockMode.ReadWrite, img.PixelFormat);
                //    IntPtr ptr = data.Scan0;
                //    int totalPixels = Math.Abs(data.Stride) * img.Height;
                //    byte[] pixels = new byte[totalPixels];

                //    Marshal.Copy(ptr, pixels, 0, totalPixels);

                //    int b = 0, g = 1, r = 2, a = 3;
                //    for (int i = 0; i < totalPixels; i += 4)
                //    {
                //        byte B = pixels[i + b];
                //        byte G = pixels[i + g];
                //        byte R = pixels[i + r];
                //        byte A = pixels[i + a];
                //        Console.WriteLine("line {0}:\t({1}, {2}, {3}, {4})", i, B, G, R, A);
                //    }
                //}
                // 测试bodyIndex数据读取
                //using (BinaryReader br = new BinaryReader(File.Open(bodyPath, FileMode.Open)))
                //{
                //    int size = br.ReadInt32();
                //    Console.WriteLine("Tht size: {0}", size);
                //    for (int i = 0; i < size; i++)
                //    {
                //        byte bt = br.ReadByte();
                //        Console.Write("{0} ", bt);
                //    }
                //    Console.Write("\n\n\n");
                //}

                // 测试深度数据
                using (BinaryReader br = new BinaryReader(File.Open(depthPath, FileMode.Open)))
                {
                    int width, height;
                    width = br.ReadInt32();
                    height = br.ReadInt32();

                    for (int i = 0; i < width * height; i++)
                    {
                        ushort pixel = br.ReadUInt16();
                        if (pixel != 0)
                            Console.Write("{0} ", pixel);
                    }
                    Console.Write("\n\n\n");
                }

                // 测试骨骼数据读取
                //using (BodyReader br = new BodyReader(File.Open(skeletonPath, FileMode.Open)))
                //{
                //    BodyData[] bodies = br.ReadAllBodies();
                //    for (int i = 0; i < bodies.Length; i++)
                //    {
                //        Console.WriteLine("BodyIndex:  {0}", bodies[i].TrackingId);
                //        foreach (Joint joint in bodies[i].Joints.Values)
                //        {
                //            Console.WriteLine("{0}:\t({1}, {2}, {3})", joint.JointType, joint.Position.X, joint.Position.Y, joint.Position.Z);
                //        }
                //    }
                //}
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.Message);
            }
        }
    }
}
