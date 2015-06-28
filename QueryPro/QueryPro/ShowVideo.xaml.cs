using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QueryPro
{
    /// <summary>
    /// ShowVideo.xaml 的交互逻辑
    /// </summary>
    public partial class ShowVideo : Window
    {
        private DispatcherTimer timer;
        public ShowVideo(string path, int[] pos)
        {
            InitializeComponent();

            Uri vp = new Uri(path, UriKind.Relative);
            myMediaElement.Source = vp;

            int prev = 0;
            foreach (int i in pos)
            {
                Button nb = new Button();
                nb.Style = this.Resources["ColorButtonStyle"] as Style;
                nb.Background = this.Resources["brush"] as Brush;
                nb.Margin = new Thickness(i-prev, 0, 0, 10);
                
                this.appearDock.Children.Add(nb);
                prev = i;
            }
            
        }

        private void myMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineSlider.Maximum = myMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            timelineSlider.AddHandler(MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(SeekToMediaPosition),
                true);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += timer_Tick;
            timer.Start(); 
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timelineSlider.Value = myMediaElement.Position.TotalMilliseconds;
        }

        private void myMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            myMediaElement.Stop();
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (playButton.Content.ToString() == "Play")
            {
                myMediaElement.Play();
                playButton.Content = "Pause";
            }
            else if (playButton.Content.ToString() == "Pause")
            {
                myMediaElement.Pause();
                playButton.Content = "Play";
            }
        }

        private void stopBotton_Click(object sender, RoutedEventArgs e)
        {
            myMediaElement.Stop();
            timelineSlider.Value = 0;
            playButton.Content = "Play";
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            myMediaElement.Position -= TimeSpan.FromMilliseconds(1000);
        }

        private void foreBotton_Click(object sender, RoutedEventArgs e)
        {
            myMediaElement.Position += TimeSpan.FromMilliseconds(1000);
        }

        private void SeekToMediaPosition(object sender, MouseButtonEventArgs args)
        {
            Point p = args.GetPosition(timelineSlider);
            System.Diagnostics.Debug.WriteLine(p.X);

            double pos = (p.X / timelineSlider.Width) * timelineSlider.Maximum;
            TimeSpan ts = TimeSpan.FromMilliseconds(pos);
            myMediaElement.Position = ts;
        }

        private void appearBotton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void myMediaElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoInfo infoDialog = new VideoInfo();
            infoDialog.Show();
        }
    }
}
