using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    public partial class SearchTracks : Page
    {
        public SearchTracks()
        {
            InitializeComponent();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем текст из поля ввода
            string searchTerm = SearchTextBox.Text;

            // Ищем треки
            var foundTracks = MusicLibrary.SearchTracks(searchTerm);

            ResultsSongs.ItemsSource = foundTracks;
        }

        private void Like_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var track = StateMusic.button.DataContext as Music;
                LikedLibrary.LoadLikeMusicToCollection(track);
            }
            catch
            {

            }
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

                // И КАК ОБЫЧНО ОТКРЫВАЕМ ЗАПУСКАЕМ И МЕНЯЕМ ЗНАЧЕНИЕ 
                GlobalVariables.Player.Open(new Uri(track.PathToFile));
                GlobalVariables.Player.Play();

                StateMusic.isPlaying = true;
            }
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Проверяем, нажата ли клавиша Enter
            {
                PerformSearch(); // Вызываем метод поиска
                e.Handled = true; // Отменяем дальнейшую обработку события
            }
        }

        private void PerformSearch()
        {
            string searchTerm = SearchTextBox.Text;
            var foundTracks = MusicLibrary.SearchTracks(searchTerm);

            ResultsSongs.ItemsSource = foundTracks; // Привязываем найденные треки к ItemsControl
        }
    }
}
