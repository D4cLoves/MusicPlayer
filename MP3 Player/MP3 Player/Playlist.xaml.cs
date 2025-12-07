using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace MP3_Player
{




    /// <summary>
    /// Логика взаимодействия для Playlist.xaml
    /// </summary>
    public partial class Playlist : Page
    {
     
        public Playlist()
        {
            InitializeComponent();

        }

        private void test(object sender, RoutedEventArgs e)
        {
            // Получаем MainWindow через Application
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new LikedPlaylistPage());
            }
        }
    }
}
