using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Kinect;

namespace FeatureExtracter
{
    /// <summary>
    /// 用来管理非托管内存
    /// </summary>
    unsafe class UnManagedMemory : IDisposable
    {
        public Int32 Count { get; private set; }
        public byte* Handle;
        private bool _disposed = false;

        public UnManagedMemory(int size)
        {
            Handle = (byte*)Marshal.AllocHGlobal(size);
            Count = size;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
                return;
            if (isDisposing)
            {
                if (Handle != null)
                {
                    Marshal.FreeHGlobal((IntPtr)Handle);
                }
            }
            _disposed = true;
        }

        ~UnManagedMemory()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// 结构体和字节流之间的互相转换，主要用于离线骨骼数据
    /// </summary>
    class SBConvertor
    {
        private static readonly SBConvertor instance = new SBConvertor();
        private SBConvertor() { }

        /// <summary>
        /// 单例
        /// </summary>
        public static SBConvertor Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// 结构体转为字节流
        /// </summary>
        /// <param name="structure">需要转换的结构体对象</param>
        /// <returns></returns>
        public byte[] StructToBytes(Object structure)
        {
            int size = Marshal.SizeOf(structure);
            byte[] bytes = new byte[size];

            using (UnManagedMemory mem = new UnManagedMemory(size))
            {
                unsafe
                {
                    IntPtr buffer = (IntPtr)mem.Handle;
                    Marshal.StructureToPtr(structure, buffer, false);
                    Marshal.Copy(buffer, bytes, 0, size);
                }
                
                return bytes;
            }
            
        }

        /// <summary>
        /// 字节流转换为结构体
        /// </summary>
        /// <param name="bytes">需要转换的字节流</param>
        /// <param name="structType">转换结果的结构体类型</param>
        /// <returns></returns>
        public Object BytesToStruct(byte[] bytes, Type structType)
        {
            int size = Marshal.SizeOf(structType);

            using (UnManagedMemory mem = new UnManagedMemory(size))
            {
                unsafe
                {
                    IntPtr buffer = (IntPtr)mem.Handle;
                    Marshal.Copy(bytes, 0, buffer, size);

                    return Marshal.PtrToStructure(buffer, structType);
                }
            }
        }
    }

    public class BodyData
    {
        private IDictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();

        public ulong TrackingId { get; private set; }
        public IDictionary<JointType, Joint> Joints
        {
            get { return joints; }
        }

        public BodyData(ulong id)
        {
            this.TrackingId = id;
        }
    }

    public class BodyReader : IDisposable
    {
        private FileStream fs;
        readonly uint BodyNum = 6;
        public BodyReader(FileStream fs)
        {
            this.fs = fs;
        }

        public BodyData[] ReadBodies(uint count)
        {
            BodyData[] bodies = new BodyData[count];
            using (BinaryReader br = new BinaryReader(this.fs))
            {
                for (int i = 0; i < count; i++)
                {
                    BodyData data = new BodyData(br.ReadUInt64());

                    int jointNum = Body.JointCount;
                    for (int j = 0; j < jointNum; j++)
                    {
                        int jointSize = Marshal.SizeOf(typeof(Joint));
                        byte[] jointBytes = br.ReadBytes(jointSize);
                        Object o = SBConvertor.Instance.BytesToStruct(jointBytes, typeof(Joint));
                        Joint joint;
                        if (o != null)
                        {
                            joint = (Joint)o;
                            data.Joints[joint.JointType] = joint;
                        }
                    }

                    bodies[i] = data;
                }
            }
            return bodies;
        }

        public BodyData[] ReadAllBodies()
        {
            return ReadBodies(BodyNum);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.fs = null;
            }
        }

        ~BodyReader()
        {
            Dispose(false);
        }
    }
}
