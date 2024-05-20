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
//using Window = OpenCvSharp.Window;
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
        //private ObservableCollection<Point> lineValues;
        //public ObservableCollection<Point> LineValues
        //{
        //    get { return lineValues; }
        //    set
        //    {
        //        lineValues = value;
        //        OnPropertyChanged("LineValues");
        //    }
        //}

        private int A;
        public int a
        {
            get { return A; }
            set
            {
                A = value;
                OnPropertyChanged("a");
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

        //public List<string> list_of_puctures;
        //public List<string> List_Of_Pictires
        //{
        //    get { return list_of_puctures; }
        //    set { list_of_puctures = value;
        //        OnPropertyChanged("List_Of_Pictires");
        //    }
        //}
    }




    public partial class MainWindow : System.Windows.Window
    {
        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        string Path = " ";
        int currentIndex = 0;
        string[] filenames;
        double Sec = 0.05;
        private DispatcherTimer timer;
        int flag = 0;
        System.Windows.Point startPoint;
        bool selectFlag = false;
        bool searchcomplflag = false;
        Dictionary<int, BitmapSource> dic_image = new Dictionary<int, BitmapSource>();
        Dictionary<int, BitmapSource> dic_image2 = new Dictionary<int, BitmapSource>();
        static Dictionary<int, List<double>> dic_spos = new Dictionary<int, List<double>>();    //в листе верхний левый X, верхний левый Y, коэффициент соответствия
        string[] photonames = new string[] { ".jpg", ".png", ".jpeg", ".tiff", ".gif", ".bmp",".webp", ".raw" };
        double crop_im;
        List<double> X = new List<double>();
        List<double> Y = new List<double>();
        ChartValues<double> X1 = new ChartValues<double>();
        ChartValues<double> Y1 = new ChartValues<double>();
        static List<int> countofFrames = new List<int>();
        List<double> DIST = new List<double>();
        static Dictionary<int, BitmapSource> tmp = new Dictionary<int, BitmapSource> ();

        Thread t = new Thread(SearchForSimilar);
        



        static Mat imageCv;
        static Mat tempCv;
        static double minVal, maxVal;
        static OpenCvSharp.Point minLoc, maxLoc;
        Point TP;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new Video();
            (DataContext as Video).a = 50;

            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(Sec);
            ////timer.Tick += Timer_Tick;
            //timer.Start();
        }

        static void SearchForSimilar()
        {
            int i = 0;
            dic_spos.Clear();
            foreach (BitmapSource s in tmp.Values)
            {
                Console.WriteLine(i);
                //imageCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(s));
                s.Dispatcher.Invoke(() =>
                {
                    imageCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(s));
                });
                var result = new Mat();
                result = imageCv.MatchTemplate(tempCv, TemplateMatchModes.CCoeffNormed);
                result.MinMaxLoc(out minVal, out maxVal, out minLoc, out maxLoc);
                //if (i == 50)
                //{
                //    //result.ConvertTo(result, MatType.CV_8U);
                //    Cv2.ImShow("result", result);
                //    Cv2.WaitKey();
                    
                //}              
                List<double> TP = new List<double>() {maxLoc.X, maxLoc.Y, maxVal};
                dic_spos.Add(i,  TP);
                i++;
                countofFrames.Add(i);
            }
                      

            //var window = new OpenCvSharp.Window("Video Frame by Frame");
            //window.ShowImage(tempCv);    // карта сходности


            //Console.WriteLine(maxVal);
            //Console.WriteLine("\nTemp X = {0}, Y = {1}", TP.X * 1.0 / image.ActualWidth * (DataContext as Video).Video_source.Width, TP.Y * 1.0 / image.ActualHeight * (DataContext as Video).Video_source.Height);
            //Console.WriteLine("Result X = {0}, Y = {1}\n", maxLoc.X * 1.0 / imageCv.Width * (DataContext as Video).Video_source.Width, maxLoc.Y * 1.0 / imageCv.Height * (DataContext as Video).Video_source.Height);
           
        }
        void ShowSimilar(int slk)
        {
            if (dic_spos[slk][2] > 0.55)
            {
                Canvas.SetLeft(sreault, dic_spos[slk][0] * 1.0 / imageCv.Width * image.ActualWidth);
                Canvas.SetTop(sreault,dic_spos[slk][1] * 1.0 / imageCv.Height * image.ActualHeight);
                sreault.Visibility = Visibility.Visible;
            }
            else sreault.Visibility = Visibility.Hidden;
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
            //Console.WriteLine(factorY);
            return new CroppedBitmap(src, new Int32Rect((int)Math.Round(x * factorX), (int)Math.Round(y * factorY), (int)Math.Round(w * factorX), (int)Math.Round(h * factorY)));
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
            flag = 1;
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec,0);
            searchcomplflag = false;
            (DataContext as Video).Video_source = null;
            (DataContext as Video).List_Of_Frames=null;
            img.Source = null;
            dic_image2.Clear();
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            Path = fbd.SelectedPath;
            if (Path != "" && Path != null) filenames = Directory.GetFiles(Path).ToArray();
            List<string> fileNames = new List<string>();
            for (int i = 0; i < filenames.Length; ++i)
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
                  //  }

                }
                
                (DataContext as Video).List_Of_Frames = dic_image2;
                if(dic_image2.Count!=0) (DataContext as Video).Video_source = dic_image2[0];
            }


        }
        

        private void Button_Click_1(object sender, RoutedEventArgs e) //Преобразование видео в кадры. Библиотека OpenCvSharp
        {
            flag = 2;
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);
            (DataContext as Video).Video_source = null;
            (DataContext as Video).List_Of_Frames = null;
            searchcomplflag = false;
            dic_image.Clear();
            System.Windows.Forms.OpenFileDialog fbd = new System.Windows.Forms.OpenFileDialog(); //выбор файла. Есть в папке с проектом
            fbd.ShowDialog();
            Path = fbd.FileName;
            string videoFile = " ";
            if (Path != "" && Path != null) videoFile = Path;
            var capture = new VideoCapture(videoFile);
            //var window = new OpenCvSharp.Window("Video Frame by Frame");  //для вывода в окно
            var image = new Mat();
            //var dic_image = new Dictionary<int, Bitmap>(); // словарь с кадрами

            int i = 0;
            while (capture.IsOpened()) //Получение кадров
            {
                capture.Read(image);

                if (image.Empty()) break;
                /*
                if (i == 0)
                {
                    imageCv = image;
                    var window = new OpenCvSharp.Window("Video Frame by Frame");
                    window.ShowImage(imageCv);
                }*/
                if (i % 5 == 0)
                {
                    BitmapSource frame = GetBitmapSource(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image)); // в битмап
                    if (!dic_image.ContainsKey(i)) dic_image[i] = frame;
                    //Console.WriteLine(frame);
                }//добавляем конвертированный в битмап сурс битмап в список битмапов
                i++;
                //var windo = new OpenCvSharp.Window("Video Frame by Frame");
                //windo.ShowImage(image);
                imageCv = image;
                //window.ShowImage(image);  //для вывода в окно
            }


             (DataContext as Video).List_Of_Frames = dic_image;
            if (dic_image.Count != 0) (DataContext as Video).Video_source = dic_image[0];
            /*
            foreach (var frame in dic_image)
            {
                Console.WriteLine($"Key: {frame.Key}, Value: {frame.Value}");
            }
            */


            //Console.WriteLine(Frames.Count);
        }


        private void rec_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectFlag = true;
            rec.CaptureMouse();
            startPoint = e.GetPosition(rec);
            sreault.Visibility = Visibility.Hidden;
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

        private void rec_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (selectFlag && (DataContext as Video).Video_source != null)
            {
                Point newPoint = e.GetPosition((IInputElement)rec.Parent);

                if ((newPoint.X - startPoint.X + rec.ActualWidth) < image.ActualWidth && (newPoint.Y - startPoint.Y + rec.ActualHeight) < image.ActualHeight && (newPoint.X - startPoint.X) > 0 && (newPoint.Y - startPoint.Y) > 0)
                {
                    Canvas.SetLeft(rec, newPoint.X - startPoint.X);
                    Canvas.SetTop(rec, newPoint.Y - startPoint.Y);
                }


                Point PosCanv = rec.TranslatePoint(new Point(0, 0), image);
                TP = rec.TranslatePoint(new Point(0, 0), image);


                var tempCrB = GetCroppedBitmap((DataContext as Video).Video_source, PosCanv.X * crop_im, PosCanv.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                //CroppedBitmap croppedBitmap = new CroppedBitmap((DataContext as Video).Video_source, new Int32Rect((int)PosCanv.X*4, (int)PosCanv.Y*4, (int)rec.ActualWidth, (int)rec.ActualHeight));
                tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(tempCrB));
                /*
                var window = new OpenCvSharp.Window("Video Frame by Frame");
                window.ShowImage(tempCv);
                */

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
                //(DataContext as Video).Dist.Clear();
                foreach (var value in dic_spos.Values)
                {
                    ScatterPoint tmp = new ScatterPoint(value[0], value[1]);
                    m.Add(tmp);
                    //X1.Add(value[0]);
                    //Y1.Add(value[1]);                       
                }
                //int size = X.Count;
                //for (int i = 0; i < size; ++i)
                //{
                //    (DataContext as Video).Dist.Add(Math.Sqrt(((X[i] - X[0])* (X[i] - X[0]))+ ((Y[i] - Y[0]) * (Y[i] - Y[0]))));
                //}

                //SeriesCollection = new LiveCharts.SeriesCollection
                //{
                //    new LineSeries
                //    {
                //        Title = "Example Series",
                //        //Values = (DataContext as Video).Dist
                //        Values = X1
                //    }//,
                //    //new ColumnSeries
                //    //{
                //    // //Values = (DataContext as Video).CountOfFrames
                //    // Values = Y1
                //    //}
                //};
                SeriesCollection = new LiveCharts.SeriesCollection
            {
                new LineSeries
                {
                    Values = m,
                },
             
            };
                chart.Series = SeriesCollection;
                //Console.WriteLine((DataContext as Video).Dist.Count);
            }           
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);
            if ((DataContext as Video).a<=70) (DataContext as Video).a += 10;

        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);
            if ((DataContext as Video).a >=20) (DataContext as Video).a -= 10;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {           
            int selectedKey = box.SelectedIndex;
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);
            sreault.Visibility = Visibility.Hidden;

            //dic_image.ContainsKey(selectedKey)  раньше было в условии ниже
            if (flag == 2 && (DataContext as Video).Video_source != null)
            {
                (DataContext as Video).Video_source = dic_image[selectedKey * 5];
            }
            if (flag == 1 && (DataContext as Video).Video_source != null)
            {
                (DataContext as Video).Video_source = dic_image2[selectedKey];
            }

            if ((DataContext as Video).Video_source != null && img.Source != null && searchcomplflag == true)
            {
                ShowSimilar(selectedKey);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var Tmp = Process.GetCurrentProcess().Threads;
            var ddd = t.ManagedThreadId;
           foreach( ProcessThread i in Tmp)
            {
                Console.WriteLine(i.Id);
            }
            if ((DataContext as Video).Video_source != null && img.Source != null)
            {
                if(t.ThreadState == ThreadState.Running)
                {
                    t.Abort();
                }
                searchcomplflag = true;
                tmp = (DataContext as Video).List_Of_Frames;
                t.Start();
                //SearchForSimilar();
            }
        }
    }
}