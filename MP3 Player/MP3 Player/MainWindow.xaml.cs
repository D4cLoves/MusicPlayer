using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TagLib;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;


namespace MP3_Player
{
    // ----------------------------------------------------------------------------------------------
    //|                КЛАССЫ ДЛЯ ХРАНЕНИЯ ТРЕКОВ                                                   |
    //_______________________________________________________________________________________________
    [Serializable]
    public class Music
    {
        // ПОЛЯ ПРИСУЩИЕ КАЖДОМУ ТРЕКУ
        public string Title { get; set; }
        public string Artist { get; set; }
        public string PathToFile { get; set; }
        public TimeSpan Time { get; set; }
        public string Album { get; set; }
        public BitmapImage Art { get; set; }
        public bool IsLiked {  get; set; }

        // КОНСТРУКТОР, ПО УМОЛЧАНИЮ ОБЛОЖКИ НЕТ, ЕСЛИ ЕСТЬ ДОБАВИТЬСЯ ПОЗЖЕ
        public Music(string title, string artist, string pathToFile, TimeSpan time, string album, BitmapImage art = null)
        {
            Title = title;
            Artist = artist;
            PathToFile = pathToFile;
            Time = time;
            Album = album;
            Art = art;
            IsLiked = false;
        }
        public Music()
        {

        }

        public override string ToString()
        {
            return $"{Title} - {Artist} ({Album}) [{Time}]";
        }

    }
    public static class MusicLibrary 
    {
        // ДЛЯ ХРАНЕНИЯ И ОТСЛЕЖИВАНИЕ ДОБАВЛЕНИЕ НОВЫХ ПЕСЕН
        public static ObservableCollection<Music> Tracks { get; private set; } = new ObservableCollection<Music>();

        // МЕТОД ДЛЯ ЗАГРУЗКИ ПЕСЕН ИЗ ПОЛУЧЕННОГО ПУТИ В КОЛЛЕКЦИЮ
        public static void LoadMusicFromDirectory(string directoryPath)
        {
            // ПРОВЕРЯЕМ СУЩЕСТВОВАНИЕ ПАПКИ
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                Debug.WriteLine($"Папка не существует: {directoryPath}");
                MessageBox.Show($"Папка не найдена: {directoryPath}\nПожалуйста, выберите папку с музыкой через меню Settings -> Добавить музыку", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Debug.WriteLine($"Начинаем загрузку музыки из папки: {directoryPath}");

            // ОЧИЩАЕМ КОЛЛЕКЦИЮ ПЕРЕД ЗАГРУЗКОЙ НОВЫХ ТРЕКОВ
            Tracks.Clear();

            // ЗАГРУЗИЛИ ОБЛОЖКУ ДЛЯ ТЕХ У КОТОРЫХ ЕЕ НЕТ, ЧТОБЫ ПУСТО НЕБЫЛО
            BitmapImage DefImage = null;
            try
            {
                DefImage = new BitmapImage(new Uri("pack://application:,,,/Resources/6ac83e964705bd58068ec5ad5fbcf372.jpg"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке изображения по умолчанию: {ex.Message}");
            }
            // ПОЛУЧАЕМ ВСЕ ФАЙЛЫ MP3 (РЕКУРСИВНО ИЗ ВСЕХ ПОДПАПОК)
            var files = Directory.GetFiles(directoryPath, "*.mp3", SearchOption.AllDirectories);
            
            Debug.WriteLine($"Найдено MP3 файлов: {files.Length}");
            
            if (files.Length == 0)
            {
                Debug.WriteLine($"В папке {directoryPath} не найдено MP3 файлов");
                MessageBox.Show($"В выбранной папке не найдено MP3 файлов.\nПапка: {directoryPath}", 
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    // Проверяем формат файла
                    if (Path.GetExtension(file).ToLower() != ".mp3")
                    {
                        Debug.WriteLine($"Файл {file} имеет неподдерживаемый формат.");
                        continue; // Пропускаем файл с неподдерживаемым форматом
                    }

                    // СОЗДАЕМ ОБЬЕКТ ДЛЯ ОБРАЩЕНИЯ К ТЕГАМ И ЕЩЕ КУЧА ФИШЕК
                    var tagfile = TagLib.File.Create(file); // Создаем объект TagLib.File

                    // ПОЛУЧАЕМ ДАННЫЕ ДЛЯ ТРЕКА
                    var title = Path.GetFileNameWithoutExtension(file);
                    var artist = tagfile.Tag.FirstPerformer ?? "Unknown Artist";
                    var album = tagfile.Tag.Album ?? "Unknown Album";
                    var time = tagfile.Properties.Duration;

                    BitmapImage Art = null;
                    // ПРОВЕРЯЕМ ЕСТЬ ЛИ ОБЛОЖКА В ТРЕКЕ
                    if (tagfile.Tag.Pictures?.Length > 0)
                    {
                        // ИЗВЛЕКАЕМ ЕЕ, НУ ЕЩЕ ПРОВЕРЯЕМ ЧТО ОНА ТОЧНО ОБЛОЖКА
                        var picture = tagfile.Tag.Pictures.FirstOrDefault(p => p.Type == TagLib.PictureType.FrontCover);
                        if (picture != null)
                        {
                            using (var stream = new MemoryStream(picture.Data.Data))
                            {
                                try
                                {
                                    Art = new BitmapImage();
                                    Art.BeginInit();
                                    Art.StreamSource = stream;
                                    Art.CacheOption = BitmapCacheOption.OnLoad;
                                    Art.EndInit();
                                    Art.Freeze();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Ошибка при обработке обложки для файла {file}: {ex.Message}");
                                    Art = null; // Установить Art в null, если произошла ошибка
                                }
                            }
                        }
                    }
                    // ЕСЛИ НЕТ, ТО ДАЕМ ПО УМОЛЧАНИЮ
                    if (Art == null && DefImage != null)
                    {
                        Art = DefImage;
                    }

                    // Создаем новый объект Music и добавляем его в коллекцию
                    var musicTrack = new Music(title, artist, file, time, album, Art);
                    Tracks.Add(musicTrack);
                    Debug.WriteLine($"Загружен трек: {title} - {artist}");
                }
                catch (TagLib.CorruptFileException ex)
                {
                    Debug.WriteLine($"Коррупция файла {file}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при загрузке файла {file}: {ex.Message}");
                }
            }
            
            Debug.WriteLine($"Всего загружено треков: {Tracks.Count}");
        }

        public static ObservableCollection<Music> SearchTracks(string searchTerm)
        {
            // Создаем коллекцию для хранения результатов поиска
             var results = new ObservableCollection<Music>();

            // Проверяем, что введенное слово не пустое
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return results; // Возвращаем пустую коллекцию, если слово пустое
            }

            // Приводим строку поиска к нижнему регистру для нечувствительного поиска
            string lowerSearchTerm = searchTerm.ToLower();

            // Ищем среди треков
            foreach (var track in Tracks)
            {
                if (track.Title.ToLower().Contains(lowerSearchTerm) ||
                    track.Artist.ToLower().Contains(lowerSearchTerm))
                {
                    results.Add(track);
                }
            }

            return results; // Возвращаем найденные треки
        }


    }

    // ----------------------------------------------------------------------------------------------
    //|   ЭТОТ ДЛЯ ХРАЕНИЯ СОСТОЯНИЯ ПОКА ЧТО КНОПКИ, НА КОТОРУЮ НАЖАЛИ  И ИНФОРМАЦИИ ИГРАЕТ ЛИ ТРЕК|
    //_______________________________________________________________________________________________
    public static class StateMusic
    {
        public static bool isPlaying { get; set; } = false;
        public static Button? button { get; set; } = null;

    }







    public partial class MainWindow : Window
    {
        // ТАЙМЕР ЧТОБЫ ОБНОВЛЯТЬ ПОЛОЖЕНИЕ ПОЛЗУНКА И ТАЙМЕРА ОТСЧЕТА
        private DispatcherTimer MusicTimer;

        public MainWindow()
        {
            // ЗАГРУЖАЕМ ПЛЕЙЛИСТ С ИЗБРАННЫМИ ТРЕКАМИ
            LikedLibrary.LoadLikedMusic();

            InitializeComponent();
            // УСТАНАВЛИВАЕМ ГРОМКОСТЬ НА ТО ГДЕ И ОСТАНОВИЛИСЬ
            //GlobalVariables.Player.Volume = ValueSlider.Value;
            // ИНИЦИЛИАЛИЗИРУЕМ ПЕРВЫЙ ТАЙМЕР
            MusicTimer = new DispatcherTimer();
            // ЗАДАЕМ ИНТЕРВАЛ ОБНОВЛЕНИЯ В ПОЛ СЕКУНДЫ
            MusicTimer.Interval = TimeSpan.FromMilliseconds(500);
            // ПОДПИСЫВАЕМСЯ НА МЕТОД ОТВЕЧАЮЩИЙ ЗА ОБНОВЛЕНИЯ КАЖДЫЕ ПОЛ СЕКУНДЫ
            MusicTimer.Tick += MusicTimer_Tick;


            // СОБЫТИЯ ОТКРЫТИЯ ТРЕКА И КОГДА ОН КОНЧИТСЯ, ТОЖЕ ПОДПИСЫВАЕМСЯ
            GlobalVariables.Player.MediaOpened += Player_MediaOpen;
            GlobalVariables.Player.MediaEnded += Player_MediaEnd;


            // ----------------------------------------------------------------------------------------------
            //|   ЕСЛИ РАНЕЕ УЖЕ ЗАГРУЗИЛИ ТРЕКИ, ТО ЗАГРУЖАЕМ ИХ С ФАЙЛА ПЕРЕД СОЗДАНИЕМ СТРАНИЦЫ          |
            //_______________________________________________________________________________________________
            string musicFolderPath = LoadMusicFolderPath();                                              // |
            Debug.WriteLine($"Путь к папке с музыкой: {musicFolderPath}");                              // |
                                                                                                         // |
            if (!string.IsNullOrEmpty(musicFolderPath) && Directory.Exists(musicFolderPath))           // |
            {                                                                                            // |
                MusicLibrary.LoadMusicFromDirectory(musicFolderPath);                                    // |
                Debug.WriteLine($"Загружено треков в коллекцию: {MusicLibrary.Tracks.Count}");         // |
            }                                                                                            // |
            else if (string.IsNullOrEmpty(musicFolderPath))                                              // |
            {                                                                                            // |
                // Первый запуск - папка не выбрана                                                      // |
                Debug.WriteLine("Папка с музыкой не выбрана. Пользователь может выбрать её через Settings -> Добавить музыку"); // |
            }                                                                                            // |
            else                                                                                        // |
            {                                                                                            // |
                Debug.WriteLine($"Папка не существует: {musicFolderPath}");                              // |
            }                                                                                            // |
            //_______________________________________________________________________________________________

            // СРАЗУ ЗАГРУЖАЕМСЯ НА СТРАНИЦЕ СО ВСЕМИ ТРЕКАМИ (ПОСЛЕ ЗАГРУЗКИ ТРЕКОВ)
            var allTracksPage = new AllTracks();
            MainFrame.Navigate(allTracksPage);
            Debug.WriteLine($"Страница AllTracks создана. Треков в коллекции: {MusicLibrary.Tracks.Count}");

        } // MAINWINDOW


        #region Основные_Элементы
        // основные элементы


        // кнопка предыдущий трек
        private void PastTrack()
        {
            // ПОЛУЧАЕМ ПРЕДЫДУЩУЮ КНОПКУ
            Button nextButton = GetPastButton(StateMusic.button);
            // СОХРАНЯЕМ ДАННЫЕ О НЫНЕШНЕЙ ТЕПЕРЬ КНОПКИ
            StateMusic.button = nextButton;
            // ПРОВЕРОЧКА
            if (nextButton == null)
            {
                MessageBox.Show("Следующая кнопка не найдена.");
                return;
            }
            // БЕРЕМ ДАННЫЕ ИЗ КНОПКИ И ПРИВОДИМ ИХ К ТИПУ Music 
            var track = nextButton.DataContext as Music;

            // СТОПАЕМ ТРЕК ЕСЛИ ОН ИГРАЕТ
            if (GlobalVariables.Player.NaturalDuration.HasTimeSpan && GlobalVariables.Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                GlobalVariables.Player.Stop();
            }

            // ОТКРЫВАЕМ ТРЕК ИСПОЛЬЗУЯ ПЕРЕМЕННУЮ С ДАННЫМИ ТИПА Music ОТКУДА И БЕРЕМ ПУТЬ ЭТОГО ТРЕКА
            GlobalVariables.Player.Open(new Uri(track.PathToFile));
            // ЗАПУСКАЕМ ТРЕК
            GlobalVariables.Player.Play();
            // СОХРАНЯЕМ ЕГО СОСТОЯНИЕ
            StateMusic.isPlaying = true;

        }
        private void PastTrack_Click(object sender, RoutedEventArgs e)
        {
            PastTrack();
        }

        // кнопка для перехода на след трек
        private void NextTrack()
        {
            // ПОЛУЧАЕМ СЛЕДУЮЩИЙ ТРЕК(НУ КНОПКУ)
            Button nextButton = GetNextButton(StateMusic.button);
            // ЗАПОМИНАЕМ ЭТУ КНОПКУ КАК НЫНЕШНЮЮ
            StateMusic.button = nextButton;
            // ЕСЛИ УПЕРЛИСЬ В КОНЕЦ
            if (nextButton == null)
            {
                MessageBox.Show("Следующая кнопка не найдена.");
                return;
            }

            // ПОЛУЧАЕМ ДАННЫЕ КНОПКИ И ПРИВОДИМ ИХ К ТИПУ Music
            var track = nextButton.DataContext as Music;

            // ЕСЛИ ИГРАЛА ПЕСНЯ ТО СТАВИМ НА СТОП
            if (GlobalVariables.Player.NaturalDuration.HasTimeSpan && GlobalVariables.Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                GlobalVariables.Player.Stop();
            }

            // ОТКРЫВАЕМ ТРЕК ИСПОЛЬЗУЯ ПЕРЕМЕННУЮ С ДАННЫМИ ТИПА Music ОТКУДА И БЕРЕМ ПУТЬ ЭТОГО ТРЕКА
            GlobalVariables.Player.Open(new Uri(track.PathToFile));
            // ЗАПУСКАЕМ ТРЕК
            GlobalVariables.Player.Play();
            // СОХРАНЯЕМ ЕГО СОСТОЯНИЕ
            StateMusic.isPlaying = true;



        }
        private void NextTrack_Click(object sender, RoutedEventArgs e)
        {
            NextTrack();
        }

        // кнопка play/pause
        private void Stop_Start(object sender, RoutedEventArgs e)
        {
            // ЕСЛИ ПЕСНЯ ИГРАЕТ, СТОПАЕМ И ПЕРЕВОДИМ ПЕРЕМЕННУЮ В ФОЛЗ
            if (StateMusic.isPlaying)
            {
                // СТОПАЕМ ПЕСНЮ
                GlobalVariables.Player.Pause();
                // СТОПАЕМ ТАЙМЕР
                MusicTimer.Stop();
                // ПЕРЕВОДИМ ПОЛОЖЕНИЕ ПЕРЕМЕННОЙ
                StateMusic.isPlaying = false;
                // МЕНЯЕМ ИКОНКУ НА PLAY
                UpdatePlayPauseIcon(false);
            }
            // ЕСЛИ СТОЯЛА НА СТОПЕ ТО ВОЗООБНОВЛЯЕМ ИГРУ И ОПЯТЬ ПЕРЕВОДИМ ПЕРЕМЕННУЮ НО УЖЕ В ТРУ
            else
            {
                // ЗАПУСКАЕМ ПЕСНЮ
                GlobalVariables.Player.Play();
                // ЗАПУСКАЕМ ТАЙМЕР
                MusicTimer.Start();
                // И СНОВА ПЕРЕВОДИМ ПЕРЕМЕННУЮ
                StateMusic.isPlaying = true;
                // МЕНЯЕМ ИКОНКУ НА PAUSE
                UpdatePlayPauseIcon(true);
            }
        }

        // Обновление иконки play/pause
        private void UpdatePlayPauseIcon(bool isPlaying)
        {
            var playPauseIcon = FindName("PlayPauseIcon") as TextBlock;
            if (playPauseIcon != null)
            {
                playPauseIcon.Text = isPlaying ? "⏸" : "▶";
            }
        }

        // для работы кнопок скипа

        // ДВА МЕТОДА ДЛЯ ОПРЕДЕЛЕНИЯ ПРЕДЫДУЩЕЙ И СЛЕДУЮЩЕЙ КНОПКИ В ITEMSCONTROL(ДЛЯ ПЕРЕКЛЮЧЕНИЯ ТРЕКОВ ТУДА СЮДА)
        private Button GetPastButton(Button currentButton)
        {
            // Получаем ItemsControl, в котором находятся кнопки
            var itemsControl = FindAncestor<ItemsControl>(currentButton);

            if (itemsControl != null)
            {
                // Получаем индекс текущего элемента
                int index = itemsControl.Items.IndexOf(currentButton.DataContext);

                // ПРОВЕРЯЕМ ЕСТЬ ЛИ ЭЛЕМЕНТ ПОЗАДИ
                if (index > 0 && index - 1 < itemsControl.Items.Count)
                {
                    // ПОЛУЧАЕМ ИНДЕКС КНОПКИ СТОЯЩЕЙ ЗА НЫНЕШНЕЙ
                    var PastItem = itemsControl.Items[index - 1];
                    // ПОЛУЧАЕМ КОНТЕЙНЕР В КОТОРОМ ХРАНИТСЯ НАША КНОПКА
                    var nextContainer = itemsControl.ItemContainerGenerator.ContainerFromItem(PastItem) as FrameworkElement;

                    if (nextContainer != null)
                    {
                        // ЕСЛИ КОНТЕЙНЕР НАШЛИ ТО ИЩЕМ КНОПКУ УЖЕ ВНУТРИ НЕГО УЖЕ В ДРУГОМ МЕТОДЕ
                        return FindVisualChild<Button>(nextContainer);
                    }
                }
            }
            return null;
        }
        private Button GetNextButton(Button currentButton)
        {
            // Получаем ItemsControl, в котором находятся кнопки
            var itemsControl = FindAncestor<ItemsControl>(currentButton);

            if (itemsControl != null)
            {
                // Получаем индекс текущего элемента
                int index = itemsControl.Items.IndexOf(currentButton.DataContext);

                // Проверяем, есть ли следующий элемент
                if (index >= 0 && index + 1 < itemsControl.Items.Count)
                {
                    // ПОЛУЧАЕМ ИНДЕКС СЛЕДУЮЩЕГО ЭЛЕМЕНТА
                    var nextItem = itemsControl.Items[index + 1];
                    // ПОЛУЧАЕМ РОДИТЕЛЬСКИЙ ЭЛЕМЕНТ ЭТОГО СЛЕДУЮЩЕГО ЭЛЕМЕНТА КОТОРОГО ПОЛУЧИЛИ РАНЕЕ
                    var nextContainer = itemsControl.ItemContainerGenerator.ContainerFromItem(nextItem) as FrameworkElement;

                    if (nextContainer != null)
                    {
                        // ТЕПЕРЬ ИЗ ПОЛУЧИВШЕГО КОНТЕЙНЕРА ИЩЕМ ПЕРВУЮ КНОПКУ В НАШЕМ СЛУЧАЕ
                        return FindVisualChild<Button>(nextContainer);
                    }
                }
            }
            return null; // Если следующей кнопки нет
        }

        // МЕТОД ДЛЯ НАХОЖДЕНИЯ РОДИТЕЛЬКОГО ЭЛЕМЕНТА ЛЮБОГО ТИПА, ПРИНИМАЮЩИЙ ЕГО CHILDA  в качестве параметра
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            // ищем пока он есть
            while (current != null)
            {
                // ЕСЛИ НАШ ЭЛЕМЕНТ ЭТО ТОТ КОТОРЫЙ НАМ НУЖЕН СОХРАНЯЕМ ЕГО В ancestor
                if (current is T ancestor)
                {
                    // И ВОЗВРАЩАЕМ ЕГО
                    return ancestor;
                }
                // ЕСЛИ НЕТ ТО ПЕРЕХОДИМ К ЕГО РОДИТЕЛЬКОМУ ЭЛЕМЕНТУ
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        // ПРНИМАЕМ РОД ЭЛЕМЕНТ ИИЩЕМ Т НО ТОЛЬКО ЕСЛИ ОНО НАСЛЕДУЕТСЯ ОТ DependencyObject
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            // ИЗ ДАННОГО НАМ РОД ЭЛЕМЕНТА ЗАПУСКАЕМ ЦИКЛ ПО ВСЕМ ЕГО ДОЧЕРНИМ ЭЛЕМЕНТАМ
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                // ПОЛУЧАЕМ КАЖДЫЙЙ ДОЧ ЭЛЕМЕНТ
                var child = VisualTreeHelper.GetChild(parent, i);
                // ЕСЛИ ПОЛУЧЕННЫЙ ЭЛЕМЕНТ РАВЕН В НАШЕМ СЛЕЧАЕ КНОПКЕ ТО СОХРАНЯЕМ ЕЕ В visualChild И ПЕРЕДАЕМ
                if (child is T visualChild)
                {
                    return visualChild;
                }
                // ЕСЛИ НЕТ ТО РЕКУРСИВНО ВЫЗЫВАЕМ ВСЕ ГЛУБЖЕ И ГЛУБЖЕ ПОГРУЖАЯСЬ
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    // ЕСЛИ ВСЕТАКИ НАШЛИ ТО ВОЗВРАЩАЕМ
                    return childOfChild;
                }
            }
            return null;
        }

        // слайдер песни
        private void MusicPlayeback(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ПРОВЕРЯЕМ ИМЕЕТ ЛИ ТРЕК ФИКСИРОВАННУЮ ПРОДОЛЖИТЕЛЬНОСТЬ
            if (GlobalVariables.Player.NaturalDuration.HasTimeSpan)
            {
                // СТАВИМ ТРЕК НА ТУ ПОЗИЦИЮ НА КОТОРОЙ НАХОДИТСЯ ПОЛЗУНОК ПОЛСЕ ЕГО ПЕРЕМЕЩЕНИЯ
                GlobalVariables.Player.Position = TimeSpan.FromSeconds(SliderMusic.Value);
            }
        }
        private void MusicTimer_Tick(object sender, EventArgs e)
        {
            if (GlobalVariables.Player.NaturalDuration.HasTimeSpan)
            {
                // ПЕРЕДВИГАЕМ СЛАЙДЕР ВМЕСТЕ С ТРЕКОМ
                SliderMusic.Value = GlobalVariables.Player.Position.TotalSeconds;
                
                // ОБНОВЛЯЕМ КОНЕЦ ПЕСНИ ДЛЯ СЛАЙДЕРА
                SliderMusic.Maximum = GlobalVariables.Player.NaturalDuration.TimeSpan.TotalSeconds;

                // ОБНОВЛЯЕМ ВРЕМЯ В ЛЕЙБЕЛАХ
                UpdateTimeLabels();
            }
        }

        // Обновление меток времени
        private void UpdateTimeLabels()
        {
            if (GlobalVariables.Player.NaturalDuration.HasTimeSpan)
            {
                var currentTime = GlobalVariables.Player.Position;
                var totalTime = GlobalVariables.Player.NaturalDuration.TimeSpan;

                CurrentTime.Text = FormatTime(currentTime);
                TotalTime.Text = FormatTime(totalTime);
            }
        }

        // Форматирование времени в формат MM:SS
        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        }

        // для регулировуи громкости 
        private void ValuePlayback(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            GlobalVariables.Player.Volume = ValueSlider.Value;
        }


        // когда трек кончается и начинается
        private void Player_MediaOpen(object sender, EventArgs e)
        {
            // ТУТ СТОПАЕМ И НАЧИНАЕМ ЗАНОВО ТАЙМЕР
            MusicTimer.Stop();
            MusicTimer.Start();
            // ОБНОВЛЯЕМ ИНФУ О ТРЕКЕ В НИЖНЕЙ ПАНЕЛЬКИ
            UpdateButtonPanel();
            // Обновляем иконку на pause
            UpdatePlayPauseIcon(true);
            // Обновляем время
            UpdateTimeLabels();
        }
        private void Player_MediaEnd(object sender, EventArgs e)
        {
            // СТАВИМ СЛАЙДЕР В ИЗНАЧАЛЬНОЕ ПОЛОЖЕНИЕ
            SliderMusic.Value = 0;
            // И ЗАПУСКАЕМ СЛЕДУЮЩИЙ ТРЕК
            NextTrack();
            // ОБНОВЛЯЕМ ИНФУ О ТРЕКЕ В НИЖНЕЙ ПАНЕЛЬКИ
            UpdateButtonPanel();



        }

        // переход к странице Songs
        private void SongsHome_Click(object sender, RoutedEventArgs e)
        {
            var allTracksPage = new AllTracks();
            MainFrame.Navigate(allTracksPage);
            System.Diagnostics.Debug.WriteLine($"Переход на страницу Songs. Треков: {MusicLibrary.Tracks.Count}");
        }

        // выход из приложения
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // переход на страницу поиска
        private void Searchee_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SearchTracks());
        }

        // переход на страницу с плейлистами
        private void Playlist_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Playlist());
        }

        // для перетаскивания приложения
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // для получения треков из папки
        private void AddMusic_Click(object sender, RoutedEventArgs e)
        {
            // ВЫЗЫВАЕМ ДИАЛОГ ОКНО (ВИН ФОРМС)
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку с музыкой";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // ПОЛУЧАЕМ ПУТЬ К ВЫБРАННОЙ ПАПКЕ
                    string path = dialog.SelectedPath;

                    // МЕТОД ДЛЯ ЗАГРУЗКИ ПЕСЕН ИЗ ПУТИ В КЛАСС ДЛЯ ИХ ХРАНЕНИЯ, НУ В КОЛЛЕКЦИЮ
                    MusicLibrary.LoadMusicFromDirectory(path);

                    // СОХРАНЯЕМ ПУТЬ В КОНФИГ
                    SaveMusicFolderPath(path);

                    // ОБНОВЛЯЕМ UI ЕСЛИ МЫ НА СТРАНИЦЕ СО ВСЕМИ ТРЕКАМИ
                    if (MainFrame.Content is AllTracks allTracksPage)
                    {
                        // Обновляем через Dispatcher для гарантии обновления UI
                        Dispatcher.Invoke(() =>
                        {
                            allTracksPage.Songs.ItemsSource = null;
                            allTracksPage.Songs.ItemsSource = MusicLibrary.Tracks;
                            System.Diagnostics.Debug.WriteLine($"UI обновлен. Треков: {MusicLibrary.Tracks.Count}");
                        });
                    }
                    else
                    {
                        // Если мы не на странице AllTracks, переходим на неё
                        var newAllTracksPage = new AllTracks();
                        MainFrame.Navigate(newAllTracksPage);
                    }
                }
            }
        }

        // сохраняем эту папку в файл
        private void SaveMusicFolderPath(string path)
        {
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MP3_Player", "config.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            System.IO.File.WriteAllText(configPath, path);
        }

        // выгружам треки уже из файла
        private string LoadMusicFolderPath()
        {
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MP3_Player", "config.txt");

            if (System.IO.File.Exists(configPath))
            {
                return System.IO.File.ReadAllText(configPath);
            }
            return string.Empty;
        }

        // для обновления нижней панельки
        public void UpdateButtonPanel()
        {
            if (StateMusic.button?.DataContext is Music track)
            {
                NameTrack.Text = track.Title;
                NameArtist.Text = track.Artist;
                if (track.Art != null)
                {
                    var artBorder = FindName("Art") as Border;
                    if (artBorder != null)
                    {
                        artBorder.Background = new ImageBrush(track.Art);
                    }
                }
            }
            else
            {
                NameTrack.Text = "No track selected";
                NameArtist.Text = "Unknown Artist";
            }
        }

        #endregion
    }
}