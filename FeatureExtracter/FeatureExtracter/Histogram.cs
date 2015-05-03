using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FeatureExtracter
{
    /// <summary>
    /// 用于颜色空间的转换
    /// </summary>
    class ColorConvertor
    {
        private static readonly ColorConvertor instance = new ColorConvertor();
        private ColorConvertor() { }

        public static ColorConvertor Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// 通过RGB值获得HSV/HSL空间的Hue，即色相的值
        /// </summary>
        /// <param name="r">RGB空间中，R通道值</param>
        /// <param name="g">RGB空间中，G通道值</param>
        /// <param name="b">RGB空间中，B通道值</param>
        /// <returns>HSV/HSL颜色空间中的Hue通道值，即色相值</returns>
        public float GetHue(byte r, byte g, byte b)
        {
            float R = 1.0f * r / 255;
            float G = 1.0f * g / 255;
            float B = 1.0f * b / 255;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            float numerator = 0.0f;
            float tail = 0.0f;

            if (max == min)
            {
                return 0;
            }
            else if (max == r)
            {
                numerator = G - B;
                tail = numerator >= 0 ? 0 : 360;
            }
            else if (max == g)
            {
                numerator = B - R;
                tail = 120;
            }
            else
            {
                numerator = R - G;
                tail = 240;
            }

            return 60 * numerator / (max - min) + tail;
        }
    }

    interface IHistogram<T>
    {
        float this[T bin] { get; set; }
        void Norm();
        bool  Normalized{ get; set; }
        double DistanceTo(IHistogram<T> histo);
    }

    class HueHisto : IHistogram<byte>
    {
        /// <summary>
        /// Hue直方图的维度，即bin的数量
        /// </summary>
        public static readonly int Dimension = 180;

        private float[] hhisto = null;
        private int sum = 0;
        
        public HueHisto()
        {
            hhisto = new float[Dimension];
            for (int i = 0; i < Dimension; i++)
            {
                hhisto[i] = 0;
            }
        }

        /// <summary>
        /// 指定bin的值
        /// </summary>
        /// <param name="bin"></param>
        /// <returns></returns>
        public float this[byte bin]
        {
            get { return hhisto[bin]; }
            set { hhisto[bin] = value; }
        }

        /// <summary>
        /// 对直方图进行归一化
        /// </summary>
        public void Norm()
        {
            if (!Normalized)
            {
                for (int i = 0; i < Dimension; i++)
                {
                    hhisto[i] /= sum;
                }
                Normalized = true;
            }
        }

        /// <summary>
        /// 判断直方图是否归一化
        /// </summary>
        public bool Normalized { get; set; }

        /// <summary>
        /// 计算与另一个同类型直方图的距离
        /// </summary>
        /// <param name="other">另一个Hue直方图</param>
        /// <returns>两个直方图的距离</returns>
        public double DistanceTo(IHistogram<byte> other)
        {
            float sum = 0;
            for (int i = 0; i < Dimension; i++)
            {
                float otherBin = other[(byte)i];
                sum += (hhisto[i] - otherBin) * (hhisto[i] - otherBin);
            }

            return Math.Sqrt(sum);
        }
    }
}
