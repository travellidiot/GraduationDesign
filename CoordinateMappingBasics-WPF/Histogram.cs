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

    public interface IHistogram<T>
    {
        /// <summary>
        /// 指定bin的值
        /// </summary>
        /// <param name="bin">要指定的bin</param>
        /// <returns></returns>
        float this[T bin] { get; set; }
        /// <summary>
        /// 都直方图归一化
        /// </summary>
        void Norm();
        /// <summary>
        /// 判断是否已经归一化
        /// </summary>
        bool Normalized { get; set; }
        /// <summary>
        /// 计算与另一个直方图的距离
        /// </summary>
        /// <param name="histo">另一个直方图</param>
        /// <returns></returns>
        double DistanceTo(IHistogram<T> histo);
        /// <summary>
        /// 合并两个直方图，两个直方图必须都未归一化
        /// </summary>
        /// <param name="histo">另一个直方图</param>
        IHistogram<T> Merge(IHistogram<T> histo);
    }

    public class HueHisto : IHistogram<byte>
    {
        class HistogramException : Exception
        {
            protected HistogramException() : base() { }
            public HistogramException(string message) : base(message) { }
        }
        
        /// <summary>
        /// Hue直方图的维度，即bin的数量
        /// </summary>
        public static readonly int Dimension = 180;

        private float[] hhisto = null;
        
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
            float sum = hhisto.Sum();
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
            if (!Normalized || !other.Normalized)
            {
                throw new HistogramException("The histogram should be normalized before calculating distance");
            }
                
            float sum = 0;
            for (int i = 0; i < Dimension; i++)
            {
                float otherBin = other[(byte)i];
                sum += (hhisto[i] - otherBin) * (hhisto[i] - otherBin);
            }

            return Math.Sqrt(sum);
        }

        public IHistogram<byte> Merge(IHistogram<byte> other)
        {
            if (Normalized || other.Normalized)
            {
                throw new HistogramException("Normalized histogram can not be merged");
            }

            HueHisto h = new HueHisto();
            for (byte i = 0; i < Dimension; i++)
            {
                h[i] = hhisto[i] + other[i];
            }

            return h;
        }

        public HueHisto GetCumHueHisto()
        {
            if (!Normalized)
                this.Norm();

            HueHisto h = new HueHisto();
            h[0] = hhisto[0];
            for (byte i = 1; i < Dimension; i++)
            {
                h[i] = h[(byte)(i-1)] + hhisto[i];
            }

            return h;
        }
    }

}
