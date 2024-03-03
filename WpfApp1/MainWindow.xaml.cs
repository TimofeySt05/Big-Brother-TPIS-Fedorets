using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

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
        private MediaElement video_source;

        public MediaElement Video_source
        {
            get { return video_source; }
            set { 
                video_source = value;
                OnPropertyChanged("Video_source");
            }
        }

        private string video_name;
        public string Video_name
        {
            get { return video_name; } 
            set 
            { 
                video_name = value;
                MediaElement X = new MediaElement
                {
                    Source = new Uri(value, UriKind.Relative)
                };
                Video_source= X;
                OnPropertyChanged("Video_name"); 
            }
        }

    }
    public partial class MainWindow : Window
    {
        string Path;
        public MainWindow()

        {
            this.DataContext = new Video();
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //FolderBrowserDialog fbd = new FolderBrowserDialog();
            //fbd.ShowDialog();
            //Path = fbd.SelectedPath;
            //if (Path != null)
            //{
            //    (DataContext as Video).Video_name = Path;
            //}

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            Path = ofd.FileName;



        }
    }
}
