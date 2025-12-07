using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;

namespace MP3_Player
{
    // класс для плеера просто чтобы удобнее
    public static class GlobalVariables
    {
        public static MediaPlayer Player { get; set; } = new MediaPlayer();
        
       // public static MediaElement Media {  get; set; } = new MediaElement();

    }

    // класс для избранных треков
    public static class LikedLibrary
    {
        // куда сохраняем избранные треки
        private const string FilePath = "likedMusic.json";
       
        // ъраним тут
        public static HashSet<Music> LikedMusic { get; set; } = new HashSet<Music>();

        // для добавления в плейлист
        public static void LoadLikeMusicToCollection(Music track)
        {
            // добавляем
            LikedMusic.Add(track);
            // сохраняем в файл
            SaveLikedMusic();
        }
        // для удаления из избранных
        public static void RemoveLikeMusicFromCollection(Music track)
        {
            // удаляем
            LikedMusic.Remove(track);
            // опять сохраняем
            SaveLikedMusic();
        }

        // для сохранения класса в json
        public static void SaveLikedMusic()
        {
            try
            {
                // Преобразуем BitmapImage в Base64 перед сериализацией

                // временный список для сереиализации
                var tempList = new List<TempMusic>();
                // проходим по всем обьектам и создаем такие же но временные для сериализации
                foreach (var track in LikedMusic)
                {
                    tempList.Add(new TempMusic
                    {
                        Title = track.Title,
                        Artist = track.Artist,
                        PathToFile = track.PathToFile,
                        Time = track.Time,
                        Album = track.Album,
                        IsLiked = track.IsLiked,
                        ArtBase64 = ConvertBitmapImageToBase64(track.Art)
                    });
                }

                // сохраняем это все в строку json
                string json = JsonConvert.SerializeObject(tempList);
                // сохраняем все в файл
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении избранной музыки: {ex.Message}");
            }
        }

        // для загрузки из файла
        public static void LoadLikedMusic()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    // читаем из файла
                    string json = File.ReadAllText(FilePath);
                    // десериализуем обратно во временный список 
                    var likedTracks = JsonConvert.DeserializeObject<List<TempMusic>>(json);
                    if (likedTracks != null)
                    {
                        // чистим текущую коллекцию
                        LikedMusic.Clear();
                        // проходимся по каждому временному элементу и создаем уже норм элемент
                        foreach (var tempTrack in likedTracks)
                        {
                            var musicTrack = new Music
                            {
                                Title = tempTrack.Title,
                                Artist = tempTrack.Artist,
                                PathToFile = tempTrack.PathToFile,
                                Time = tempTrack.Time,
                                Album = tempTrack.Album,
                                IsLiked = tempTrack.IsLiked,
                                Art = ConvertBase64ToBitmapImage(tempTrack.ArtBase64)
                            };
                            // и добавляем его в наш класс
                            LikedMusic.Add(musicTrack);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке избранной музыки: {ex.Message}");
            }
        }

        // из BitmapImage в Base64 без этого не сериализуется, либо уже переделывать класс Music и хранить вместо изображения его путь
        private static string ConvertBitmapImageToBase64(BitmapImage bitmapImage)
        {
            if (bitmapImage == null) return null;

            // создаем поток данных для временного хранения картинки
            using (var memoryStream = new MemoryStream())
            {
                // Кодирует изображение в формате PNG.
                var encoder = new PngBitmapEncoder();
                // Сохраняет данные изображения и возвращает строку Base64.
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        // Конвертация Base64 обратно в BitmapImage
        private static BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;

            // делаем из строки base64 массив байтов    
            byte[] bytes = Convert.FromBase64String(base64String);
            // загркжаем изображение из потока данных 
            using (var memoryStream = new MemoryStream(bytes))
            {
                // строим его и замораживаем
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Замораживаем объект для потокобезопасности
                return bitmapImage;
            }
        }

        // Временный класс для сериализации, тот же Music но изза изображения пришлось делать его а то не сериализуется ничего
        private class TempMusic
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string PathToFile { get; set; }
            public TimeSpan Time { get; set; }
            public string Album { get; set; }
            public bool IsLiked { get; set; }
            public string ArtBase64 { get; set; } // Храним изображение в формате Base64
        }
    }

    public partial class AllTracks : Page
    {


        public AllTracks()
        {
            InitializeComponent();
            
            // Обновляем при загрузке страницы
            this.Loaded += AllTracks_Loaded;
            
            // подписка на песни
            Songs.ItemsSource = MusicLibrary.Tracks;
            System.Diagnostics.Debug.WriteLine($"AllTracks создан. Треков в коллекции: {MusicLibrary.Tracks.Count}");
            
            // Подписываемся на изменения коллекции
            MusicLibrary.Tracks.CollectionChanged += Tracks_CollectionChanged;
        }

        private void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Коллекция треков изменилась. Действие: {e.Action}, Треков: {MusicLibrary.Tracks.Count}");
            // Обновляем UI при изменении коллекции
            if (Dispatcher.CheckAccess())
            {
                Songs.ItemsSource = null;
                Songs.ItemsSource = MusicLibrary.Tracks;
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Songs.ItemsSource = null;
                    Songs.ItemsSource = MusicLibrary.Tracks;
                });
            }
        }

        private void AllTracks_Loaded(object sender, RoutedEventArgs e)
        {
            // Обновляем ItemsSource при загрузке страницы
            Songs.ItemsSource = null;
            Songs.ItemsSource = MusicLibrary.Tracks;
            System.Diagnostics.Debug.WriteLine($"AllTracks загружен. Треков в коллекции: {MusicLibrary.Tracks.Count}, ItemsSource установлен");
        }

        //  ПРИ НАЖАТИИ НА ПЕСНЮ
        private void PlayMusic_Click(object sender, RoutedEventArgs e)
        {
            // ЗАПОМИНАЕМ ТЕКУЩУЮ КНОПКУ
            StateMusic.button = sender as Button;

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

        private void Like_Click(object sender, RoutedEventArgs e)
        {
            //var button = sender as Button;
            try
            {
                var track = StateMusic.button.DataContext as Music;
                LikedLibrary.LoadLikeMusicToCollection(track);
            }
            catch
            {

            }
        }
    }
}
