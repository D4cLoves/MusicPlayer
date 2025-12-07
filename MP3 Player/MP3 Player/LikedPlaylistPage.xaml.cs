using System;
using System.Collections.Generic;
using System.Globalization;
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

    


    public partial class LikedPlaylistPage : Page
    {
        public LikedPlaylistPage()
        {
            InitializeComponent();
            LikedLibrary.LoadLikedMusic();
        }

        private void PlayMusic_Click(object sender, RoutedEventArgs e)
        {
                    
        
            // ЗАПОМИНАЕМ ТЕКУЩУЮ КНОПКУ
            StateMusic.button = sender as System.Windows.Controls.Button;

            if (StateMusic.button != null)
            {
                // ПОЛУЧАЕМ ДАННЫЕ ИЗ КНОПКИ И ПРИВОДИ ЕЕ К ТИПУ  Music
                var track = StateMusic.button.DataContext as Music;

                // СТАВИМ НА СТОП ЕСЛИ ИГРАЕТ ДРУГАЯ ПЕСНЯ
                if (GlobalVariables.Player.NaturalDuration.HasTimeSpan && GlobalVariables.Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    GlobalVariables.Player.Stop();
                }

                //mainwindow.SongTitleLabel.DataContext = track;
                //mainwindow.ArtistLabel.DataContext = track;

                // mainwindow.SongTitleLabel.Text = track.Title;
                // mainwindow.ArtistLabel.Text = track.Artist;

                // И КАК ОБЫЧНО ОТКРЫВАЕМ ЗАПУСКАЕМ И МЕНЯЕМ ЗНАЧЕНИЕ 
                GlobalVariables.Player.Open(new Uri(track.PathToFile));
                GlobalVariables.Player.Play();

                //TrackManager s = mainwindow.trackManager;
                //s.PlayTrack(track);

                //GlobalVariables.Media.Source = new Uri(track.PathToFile);
                //GlobalVariables.Media.Play();
                StateMusic.isPlaying = true;

                //MainWindow m = new MainWindow();
                //m.UpdatePanel_Tick();

            }
        
    }

        // для удаления из избранного плейлиста
        private void deleteLikedLibrary(object sender, RoutedEventArgs e)
        {
            try
            {
                // получаем кнопку
                var track = StateMusic.button.DataContext as Music;

                // метод для удаления из класса
                LikedLibrary.RemoveLikeMusicFromCollection(track);
                // на всяк случай удаляеем из самого itemscontrol
                UpdateUIAfterRemoval(track);
                // сохраняем все в файл
                LikedLibrary.LoadLikedMusic();

            }
            catch
            {

            }

        }

        // для удаления из самого itemscontrol
        private void UpdateUIAfterRemoval(Music track)
        {
            
            // Предположим, что у вас есть ListBox или другой элемент управления для отображения избранной музыки
            Songs.Items.Remove(track); // Замените YourListBox на ваш элемент управления
        }
    }
}
