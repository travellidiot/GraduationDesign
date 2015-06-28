using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
//using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;


using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Options;

using FeatureExtracter;

namespace QueryPro
{
    using DrawColor = System.Drawing.Color;
    using DataBinding = System.Windows.Data.Binding;

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool useColor = false;

        private string[] pictureVideosThumb = {
                                                  @"C:\Users\koala-papa\Documents\bishe\11924.png",
                                                  @"C:\Users\koala-papa\Documents\bishe\11947.png"
                                              };

        private string[] pictureVideos = {
                                             @"C:\Users\koala-papa\Videos\ColorVideo-11-09-24.avi",
                                             @"C:\Users\koala-papa\Videos\ColorVideo-11-09-47.avi"
                                         };

        private int[][] pictureAppears = null;

        private string[] colorVideosThumb = {
                                                @"C:\Users\koala-papa\Documents\bishe\ColorImage-09-33-41.png",
                                                @"C:\Users\koala-papa\Documents\bishe\ColorImage-11-43-41.png",
                                                @"C:\Users\koala-papa\Documents\bishe\tu1.png"
                                            };

        private string[] colorVideos = {
                                           @"C:\Users\koala-papa\Videos\test-01-26-59.avi"
                                       };


        public MainWindow()
        {
            InitializeComponent();

            pictureAppears = new int[2][];
            pictureAppears[0] = new int[] { 420 };
            pictureAppears[1] = new int[] { 0, 615 };
        }

        private void openPicture_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            if (ofd.ShowDialog().Value)
            {
                ImageSourceConverter source = new ImageSourceConverter();
                this.queryImage.Source = (ImageSource)source.ConvertFrom(ofd.FileName);
            }

            
        }

        private ListBoxItem createQueryImageItem(string path)
        {
            Image image = new Image();
            ImageSourceConverter source = new ImageSourceConverter();
            image.Source = (ImageSource)source.ConvertFrom(path);

            ListBoxItem item = new ListBoxItem();
            item.Width = 320;
            item.Content = image;
            item.Selected += queryImageSelected;

            return item;
        }
        private void addItemToQueryImageList(ListBoxItem item)
        {
            this.queryImageList.Items.Add(item);
        }
        private void queryButton_Click(object sender, RoutedEventArgs e)
        {
            string[] paths;
            if (useColor)
                paths = this.colorVideosThumb;
            else
                paths = this.pictureVideosThumb;

            foreach (string path in paths)
            {
                
                ListBoxItem item = createQueryImageItem(path);
                addItemToQueryImageList(item);
            }
        }

        private void queryImageSelected(object sender, RoutedEventArgs e)
        {
            int index = -1;
            ListBoxItem item = e.Source as ListBoxItem;
            for (int i = 0; i < this.queryImageList.Items.Count; i++)
            {
                if (this.queryImageList.Items[i] == item)
                {
                    index = i;
                    break;
                }
            }

            ShowVideo videodialog;
            if (!useColor)
                videodialog = new ShowVideo(this.pictureVideos[index], pictureAppears[index]);
            else
                videodialog = new ShowVideo(this.colorVideos[0], new int[] { 0 });

            videodialog.ShowDialog();
        }

        private void changeBrushColorByPalette(string brushKey)
        {
            ColorDialog cd = new ColorDialog();
            cd.ShowDialog();

            DrawColor cdColor = cd.Color;
            Color newColor = new Color();
            newColor.R = cdColor.R;
            newColor.G = cdColor.G;
            newColor.B = cdColor.B;
            newColor.A = cdColor.A;

            SolidColorBrush brush = new SolidColorBrush(newColor);

            this.Resources[brushKey] = brush;
        }

        private void upColorButton_Click(object sender, RoutedEventArgs e)
        {
            changeBrushColorByPalette("upBrush");
            useColor = true;
        }

        private void doColorButton_Click(object sender, RoutedEventArgs e)
        {
            changeBrushColorByPalette("doBrush");
            useColor = true;
        }

        private void shColorButton_Click(object sender, RoutedEventArgs e)
        {
            changeBrushColorByPalette("shBrush");
            useColor = true;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            this.queryImage.Source = null;
            this.queryImageList.Items.Clear();
        }
    }
}
