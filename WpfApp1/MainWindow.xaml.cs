using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Windows.Interop;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using static System.Net.WebRequestMethods;
using System.Windows.Threading;
using OpenCvSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;
using Rectangle = System.Windows.Shapes.Rectangle;
using Point = System.Windows.Point;
using System.Drawing.Imaging;
using System.Net;
using OpenCvSharp.Aruco;
using System.Collections;
using System.Windows.Forms.DataVisualization.Charting;
using LiveCharts;
using LiveCharts.Defaults;
using System.Collections.ObjectModel;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf;
using System.Threading;
using ThreadState = System.Threading.ThreadState;
using System.Runtime.InteropServices;
using System.Linq.Expressions;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
   
    class Video : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private BitmapSource video_source;

        public BitmapSource Video_source
        {
            get { return video_source; }
            set
            {
                video_source = value;
                OnPropertyChanged("Video_source");
            }
        }

        private ChartValues<int> countOfFrames = new ChartValues<int>();
        public ChartValues<int> CountOfFrames
        {
            get { return countOfFrames; }
            set
            {
                countOfFrames = value;
                OnPropertyChanged("CountOfFrames");
            }
        }
        private ChartValues<double> dist = new ChartValues<double>();
        public ChartValues<double> Dist
        {
            get { return dist; }
            set
            {
                dist = value;
                OnPropertyChanged("Dist");
            }
        }

        private int rec_size;
        public int Rec_size
        {
            get { return rec_size; }
            set
            {
                rec_size = value;
                OnPropertyChanged("Rec_size");
            }
        }

        private int sresault_size;
        public int Sresault_size
        {
            get { return sresault_size; }
            set
            {
                sresault_size = value;
                OnPropertyChanged("Sresault_size");
            }
        }

        private string video_name;
        public string Video_name
        {
            get { return video_name; }
            set
            {
                video_name = value;
                string[] photonames = new string[] { ".jpg", ".png", ".jpeg", ".tiff", ".gif", ".bmp", ".webp", ".raw" };
                if (!string.IsNullOrEmpty(video_name) && System.IO.File.Exists(video_name) && Directory.Exists(System.IO.Path.GetDirectoryName(video_name)))
                { //1
                    if (photonames.Contains(System.IO.Path.GetExtension(value)))
                    {

                        BitmapImage X = new BitmapImage();
                        X.BeginInit();
                        X.UriSource = new Uri(value, UriKind.RelativeOrAbsolute);
                        X.EndInit();
                        Video_source = X;

                    }
                    else
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(value);
                        BitmapSource X1 = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle, System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        Video_source = X1;
                    }
                }
            }
        }

        private Dictionary<int, BitmapSource> list_of_frames;
        public Dictionary<int, BitmapSource> List_Of_Frames
        {
            get { return list_of_frames; }
            set
            {
                list_of_frames = value;
                OnPropertyChanged("List_Of_Frames");
            }
        }
        private int progress;
        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }


    }

    public class ControlWriter : TextWriter
    {
        private TextBlock _output;
        private ScrollViewer _scrollViewer;

        public ControlWriter(TextBlock output, ScrollViewer scrollViewer)
        {
            _output = output;
            _scrollViewer = scrollViewer;
        }

        public override void Write(char value)
        {
            base.Write(value);
            _output.Dispatcher.Invoke(() =>
            {
                _output.Text += value;
                _scrollViewer.ScrollToBottom(); // Прокручиваем вниз при добавлении нового текста
            });
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
        public void ClearOutput()
        {
            _output.Dispatcher.Invoke(() =>
            {
                _output.Text = string.Empty; // Очищаем содержимое TextBlock
            });
        }
    }


    public partial class MainWindow : System.Windows.Window
    {
       
       
        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        string Path = " ";
        int currentIndex = 0;
        string[] filenames;
        double Sec = 0.05;
        int flag = 0;
        System.Windows.Point startPoint;
        bool selectFlag = false;
        bool searchcomplflag = false;
        
        Dictionary<int, BitmapSource> dic_image = new Dictionary<int, BitmapSource>();
        Dictionary<int, BitmapSource> dic_image2 = new Dictionary<int, BitmapSource>();
        static Dictionary<int, List<double>> dic_spos = new Dictionary<int, List<double>>();    //в листе верхний левый X, верхний левый Y, коэффициент соответствия
        string[] photonames = new string[] { ".jpg", ".png", ".jpeg", ".tiff", ".gif", ".bmp", ".webp", ".raw" };
        string[] videonames = new string[] { ".mp4", ".avi",".mov",".mkv"};
        double crop_im;
        List<double> X = new List<double>();
        List<double> Y = new List<double>();
        ChartValues<double> X1 = new ChartValues<double>();
        ChartValues<double> Y1 = new ChartValues<double>();
        static List<int> countofFrames = new List<int>();
        static Dictionary<int, BitmapSource> tmp = new Dictionary<int, BitmapSource>();
        Point PosCanv = new Point();
        List<Thread> threads = new List<Thread>();
        bool fl = false;


        static Mat imageCv;
        static Mat tempCv;
        static double minVal, maxVal;
        static OpenCvSharp.Point minLoc, maxLoc;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new Video();
            (DataContext as Video).Rec_size = 50;
            ControlWriter controlWriter = new ControlWriter(textBlock, scrollViewer);
            Console.SetOut(new ControlWriter(textBlock, scrollViewer));
            

        }
        

        static void SearchForSimilar()
        {
            Console.WriteLine("Loading...");
            int i = 0;
            dic_spos.Clear();
            foreach (BitmapSource s in tmp.Values)
            {              
                
                s.Dispatcher.Invoke(() =>
                {
                    imageCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(s));
                    

                });    
                var result = new Mat();
                result = imageCv.MatchTemplate(tempCv, TemplateMatchModes.CCoeffNormed);
                result.MinMaxLoc(out minVal, out maxVal, out minLoc, out maxLoc);           
                List<double> TP = new List<double>() { maxLoc.X, maxLoc.Y, maxVal };
                dic_spos.Add(i, TP);
                i++;
                countofFrames.Add(i);
                GC.Collect();
                
            }
            Console.WriteLine("FINISH");



        }
        void ShowSimilar(int slk)
        {
            if (dic_spos.ContainsKey(slk))
            {
                if (dic_spos[slk][2] > 0.55)
                {
                    Canvas.SetLeft(sresault, dic_spos[slk][0] * 1.0 / imageCv.Width * image.ActualWidth);
                    Canvas.SetTop(sresault, dic_spos[slk][1] * 1.0 / imageCv.Height * image.ActualHeight);
                    sresault.Visibility = Visibility.Visible;
                }
                else sresault.Visibility = Visibility.Hidden;
            }

        }
        static Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
        

        public BitmapSource GetBitmapSource(Bitmap bitmap)
        {
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap
            (
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            return bitmapSource;
        }

        public static CroppedBitmap GetCroppedBitmap(BitmapSource src, double x, double y, double w, double h)
        {
            double factorX, factorY;

            factorX = src.PixelWidth / src.Width;
            factorY = src.PixelHeight / src.Height;
            try
            {
                return new CroppedBitmap(src, new Int32Rect((int)Math.Round(x * factorX), (int)Math.Round(y * factorY), (int)Math.Round(w * factorX), (int)Math.Round(h * factorY)));
            }
            catch (ArgumentException)
            {
                factorX = 0;
                factorY = 0;
                return new CroppedBitmap(src, new Int32Rect((int)Math.Round(x * factorX), (int)Math.Round(y * factorY), (int)Math.Round(w), (int)Math.Round(h)));
            }
        }

        public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ControlWriter controlWriter = new ControlWriter(textBlock, scrollViewer);
            controlWriter.ClearOutput();
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog()==System.Windows.Forms.DialogResult.OK)
            {
                if (threads.Count > 0)
                {
                    Thread t2 = threads[threads.Count - 1];
                    t2.Abort();
                    threads.RemoveAt(threads.Count - 1);
                }
                flag = 1;
                //Canvas.SetLeft(rec, 0.0);
                //Canvas.SetTop(rec, 0.0);
                searchcomplflag = false;
                (DataContext as Video).Video_source = null;
                (DataContext as Video).List_Of_Frames = null;
                img.Source = null;
                dic_image2.Clear();
               
                Path = fbd.SelectedPath;
                if (Path != "" && Path != null) filenames = Directory.GetFiles(Path).ToArray();
                List<string> fileNames = new List<string>();
                if (filenames != null) for (int i = 0; i < filenames.Length; ++i)
                    {
                        if (photonames.Contains(System.IO.Path.GetExtension(filenames[i])))
                        {
                            fileNames.Add(filenames[i]);
                        }
                    }

                int SIZE = fileNames.Count();
                if (fileNames != null)
                {
                    for (int i = 0; i < SIZE; i++)
                    {
                        BitmapImage bitmapImage = new BitmapImage(new Uri(fileNames[i], UriKind.Absolute));
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        BitmapSource bitmapSource = bitmapImage as BitmapSource;

                        if (!dic_image2.ContainsKey(i))
                        {
                            dic_image2.Add(i, bitmapSource);
                        }

                    }

                    (DataContext as Video).List_Of_Frames = dic_image2;
                    

                }
                if (dic_image2.Count != 0)
                {                  
                    (DataContext as Video).Video_source = dic_image2[0];
                    
                    if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                    {
                        crop_im = (DataContext as Video).Video_source.Width / image.Width;
                    }
                    else
                    {
                        crop_im = (DataContext as Video).Video_source.Height / image.Height;

                    }
                    Point p = rec.TranslatePoint(new Point(0, 0), image);
                    BitmapSource temp1 = GetCroppedBitmap((DataContext as Video).Video_source, p.X*crop_im, p.Y * crop_im, (DataContext as Video).Rec_size*crop_im, (DataContext as Video).Rec_size*crop_im);
                    img.Source = temp1;
                    tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(temp1));

                }
            }

        }


        private void Button_Click_1(object sender, RoutedEventArgs e) //Преобразование видео в кадры. Библиотека OpenCvSharp
        {
            ControlWriter controlWriter = new ControlWriter(textBlock, scrollViewer);
            controlWriter.ClearOutput();
            System.Windows.Forms.OpenFileDialog fbd = new System.Windows.Forms.OpenFileDialog();
            if (fbd.ShowDialog()==System.Windows.Forms.DialogResult.OK && videonames.Contains(System.IO.Path.GetExtension(fbd.FileName)))
            {
                if (threads.Count > 0)
                {
                    Thread t2 = threads[threads.Count - 1];
                    t2.Abort();
                    threads.RemoveAt(threads.Count - 1);
                }
                flag = 2;
                //Canvas.SetLeft(rec, 0);
                //Canvas.SetTop(rec, 0);
                img.Source = null;
                (DataContext as Video).Video_source = null;
                (DataContext as Video).List_Of_Frames = null;
                searchcomplflag = false;
                dic_image.Clear();
                Path = fbd.FileName;
                string videoFile = " ";
                if (Path != "" && Path != null) videoFile = Path;
                var capture = new VideoCapture(videoFile);
                var Image = new Mat();

                int i = 0;
                while (capture.IsOpened()) 
                {
                    capture.Read(Image);

                    if (Image.Empty()) break;

                    if (i % 5 == 0)
                    {
                        BitmapSource frame = GetBitmapSource(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(Image)); // в битмап
                        if (!dic_image.ContainsKey(i)) dic_image[i] = frame;
                    }
                    i++;
                    imageCv = Image;
                }


             (DataContext as Video).List_Of_Frames = dic_image;
                if (dic_image.Count != 0)
                {
                    (DataContext as Video).Video_source = dic_image[0];
                    if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                    {
                        crop_im = (DataContext as Video).Video_source.Width / image.Width;
                    }
                    else
                    {
                        crop_im = (DataContext as Video).Video_source.Height / image.Height;
                    }
                    Point p = rec.TranslatePoint(new Point(0, 0), image);
                    var temp = GetCroppedBitmap(dic_image[0], p.X * crop_im, p.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                    img.Source = temp;
                    tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(temp));                  
                }
            }
        }


        private void rec_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectFlag = true;
            rec.CaptureMouse();
            startPoint = e.GetPosition(rec);
            sresault.Visibility = Visibility.Hidden;
            if (selectFlag && (DataContext as Video).Video_source != null)
            {
                if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                {
                    crop_im = (DataContext as Video).Video_source.Width / image.ActualWidth;
                }
                else
                {
                    crop_im = (DataContext as Video).Video_source.Height / image.ActualHeight;
                }

            }

        }
        Point newPoint = new Point();
        private void rec_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (selectFlag && (DataContext as Video).Video_source != null)
            {
                newPoint = e.GetPosition((IInputElement)rec.Parent);

                if ((newPoint.X - startPoint.X + rec.ActualWidth) < image.ActualWidth && (newPoint.Y - startPoint.Y + rec.ActualHeight) < image.ActualHeight && (newPoint.X - startPoint.X) > 0 && (newPoint.Y - startPoint.Y) > 0)
                {
                    Canvas.SetLeft(rec, newPoint.X - startPoint.X);
                    Canvas.SetTop(rec, newPoint.Y - startPoint.Y);
                }


                PosCanv = rec.TranslatePoint(new Point(0, 0), image);
                var tempCrB = GetCroppedBitmap((DataContext as Video).Video_source, PosCanv.X * crop_im, PosCanv.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(tempCrB));
                img.Source = tempCrB;

            }


        }

        private void rec_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            selectFlag = false;

            rec.ReleaseMouseCapture();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ChartValues<ScatterPoint> m = new ChartValues<ScatterPoint>();


            if (dic_spos != null)
            {
                X1.Clear();
                Y1.Clear();
                foreach (var value in dic_spos.Values)
                {
                    ScatterPoint tmp = new ScatterPoint(value[0], value[1]);
                    m.Add(tmp);                               
                }
                
                SeriesCollection = new LiveCharts.SeriesCollection
            {
                new LineSeries
                {
                    Values = m,
                },

            };
                chart.Series = SeriesCollection;
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
           
            if ((newPoint.X - startPoint.X + rec.ActualWidth) < image.ActualWidth && (newPoint.Y - startPoint.Y + rec.ActualHeight) < image.ActualHeight && (newPoint.X - startPoint.X) > 0 && (newPoint.Y - startPoint.Y) > 0)
            {
                if ((DataContext as Video).Rec_size <= 70) (DataContext as Video).Rec_size += 10;
                
            }
             
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {

          if ((DataContext as Video).Rec_size >= 20) (DataContext as Video).Rec_size -= 10;
            
        
        }

      

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedKey = box.SelectedIndex;
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);
            sresault.Visibility = Visibility.Hidden;

            if (flag == 2 && (DataContext as Video).Video_source != null)
            {
                (DataContext as Video).Video_source = dic_image[selectedKey * 5];
                if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                {
                    crop_im = (DataContext as Video).Video_source.Width / image.Width;
                }
                else
                {
                    crop_im = (DataContext as Video).Video_source.Height / image.Height;
                }
                //PosCanv = rec.TranslatePoint(new Point(0, 0), image);
               Canvas.SetLeft(rec, PosCanv.X);
               Canvas.SetTop(rec, PosCanv.Y);
                var temp = GetCroppedBitmap(dic_image[selectedKey * 5], PosCanv.X * crop_im, PosCanv.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                img.Source = temp;
                tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(temp));
            }
            if (flag == 1 && (DataContext as Video).Video_source != null)
            {
                (DataContext as Video).Video_source = dic_image2[selectedKey];
                if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                {
                    crop_im = (DataContext as Video).Video_source.Width / image.Width;
                }
                else
                {
                    crop_im = (DataContext as Video).Video_source.Height / image.Height;
                }
                //PosCanv = rec.TranslatePoint(new Point(0, 0), image);
                Canvas.SetLeft(rec, PosCanv.X);
                Canvas.SetTop(rec, PosCanv.Y);
                var temp = GetCroppedBitmap(dic_image2[selectedKey], PosCanv.X * crop_im, PosCanv.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                img.Source = temp;
                tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(temp));
            }

            if ((DataContext as Video).Video_source != null && img.Source != null && searchcomplflag == true)
            {
                ShowSimilar(selectedKey);
            }

            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if ((DataContext as Video).Video_source != null && img.Source != null)
            {
                ControlWriter controlWriter = new ControlWriter(textBlock, scrollViewer);
                controlWriter.ClearOutput();
                searchcomplflag = true;

                (DataContext as Video).Sresault_size = (DataContext as Video).Rec_size;
                if (threads.Count > 0)
                {
                    Thread t2 = threads[threads.Count - 1];
                    t2.Abort();
                    (DataContext as Video).Progress = 0;
                    threads.RemoveAt(threads.Count - 1);
                   
                }
                Thread t1 = new Thread(SearchForSimilar);
                threads.Add(t1);
                tmp = (DataContext as Video).List_Of_Frames;
                t1.Start();

            }
        }

    }
}