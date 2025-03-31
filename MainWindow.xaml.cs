using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Wave;
using MaterialDesignThemes.Wpf;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer timer;
        private List<string> playlist = new List<string>();
        private int currentTrackIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += Timer_Tick;
            LoadMusicFiles();
        }

        private void LoadMusicFiles()
        {
            string musicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

            if (Directory.Exists(musicFolder))
            {
                string[] musicFiles = Directory.GetFiles(musicFolder, "*.*", SearchOption.AllDirectories)
                                               .Where(f => f.EndsWith(".mp3") || f.EndsWith(".wav") || f.EndsWith(".flac"))
                                               .ToArray();

                if (musicFiles.Length > 0)
                {
                    playlist.Clear();
                    playlist.AddRange(musicFiles);
                    currentTrackIndex = 0;
                    PrepareAudio(playlist[currentTrackIndex]); // Chỉ chuẩn bị, không phát ngay
                }
            }
        }

        private void PrepareAudio(string filePath)
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
            }
            if (audioFile != null)
            {
                audioFile.Dispose();
            }

            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);

            progressBar.Maximum = audioFile.TotalTime.TotalSeconds;
            progressBar.Value = 0;

            txtSongTitle.Text = Path.GetFileNameWithoutExtension(filePath);
            txtTotalTime.Text = audioFile.TotalTime.ToString(@"mm\:ss");

            SetAlbumArt(filePath);
        }

        private void SetAlbumArt(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                if (file.Tag.Pictures.Length > 0)
                {
                    var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                    using (MemoryStream ms = new MemoryStream(bin))
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ms;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        albumArt.Source = image;
                    }
                }
                else
                {
                    albumArt.Source = new BitmapImage(new Uri("pack://application:,,,/Images/placeholder.png"));
                }
            }
            catch
            {
                albumArt.Source = new BitmapImage(new Uri("pack://application:,,,/Images/placeholder.png"));
            }
        }

        private void ProgressBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (audioFile != null && outputDevice != null)
            {
                // Dừng animation trước khi tua
                progressBar.BeginAnimation(ProgressBar.ValueProperty, null);

                // Chờ layout cập nhật để lấy kích thước thực tế
                progressBar.Dispatcher.InvokeAsync(() =>
                {
                    double clickX = e.GetPosition(progressBar).X;
                    double progressWidth = progressBar.ActualWidth;

                    if (progressWidth > 0)
                    {
                        double ratio = Math.Max(0, Math.Min(1, clickX / progressWidth));
                        double newTime = ratio * audioFile.TotalTime.TotalSeconds;

                        // Cập nhật giá trị ngay
                        audioFile.CurrentTime = TimeSpan.FromSeconds(newTime);
                        progressBar.Value = newTime;
                        txtCurrentTime.Text = audioFile.CurrentTime.ToString(@"mm\:ss");

                        // Nếu đang phát, chạy lại animation từ vị trí mới
                        if (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            StartProgressBarAnimation(TimeSpan.FromSeconds(progressBar.Maximum - newTime));
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.flac",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                playlist.Clear();
                playlist.AddRange(openFileDialog.FileNames);
                currentTrackIndex = 0;
                PlayAudio(playlist[currentTrackIndex]);
            }
        }

        private void PlayAudio(string filePath)
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
            }
            if (audioFile != null)
            {
                audioFile.Dispose();
            }

            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();
            timer.Start();

            progressBar.Maximum = audioFile.TotalTime.TotalSeconds;
            progressBar.Value = 0; // Đặt lại giá trị ban đầu

            StartProgressBarAnimation(audioFile.TotalTime); // Kích hoạt animation khi phát nhạc
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioFile != null && outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                progressBar.Value = audioFile.CurrentTime.TotalSeconds;
                txtCurrentTime.Text = audioFile.CurrentTime.ToString(@"mm\:ss");
            }
        }
        private void StartProgressBarAnimation(TimeSpan duration)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = progressBar.Value, 
                To = progressBar.Maximum, 
                Duration = new Duration(duration),
                FillBehavior = FillBehavior.HoldEnd
            };

            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice == null)
                return;

            if (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();
                timer.Stop();
                progressBar.BeginAnimation(ProgressBar.ValueProperty, null); // Dừng animation
            }
            else
            {
                outputDevice.Play();
                timer.Start();
                StartProgressBarAnimation(audioFile.TotalTime); // Tiếp tục animation từ vị trí hiện tại
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count > 0 && currentTrackIndex < playlist.Count - 1)
            {
                currentTrackIndex++;
                PlayAudio(playlist[currentTrackIndex]);
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (playlist.Count > 0 && currentTrackIndex > 0)
            {
                currentTrackIndex--;
                PlayAudio(playlist[currentTrackIndex]);
            }
        }
    }
}
