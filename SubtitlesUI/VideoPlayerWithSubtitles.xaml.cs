using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace SubtitlesUI
{
    public partial class VideoPlayerWithSubtitles : Window
    {
        private bool IsPlaying;
        private bool IsPaused;
        private SubtitlesHandler Subs;
        private bool MouseIsCaptured;
        private bool IsSubTaskRunning;
        DispatcherTimer Timer;
        CancellationTokenSource Token;

        public VideoPlayerWithSubtitles()
        {
            IsPlaying = false;
            IsPaused = false;
            MouseIsCaptured = false;
            IsSubTaskRunning = false;

            InitializeComponent();
            DataContext = this;

            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// Permet de parcouris un fichier vidéo et de le définir en source du MediaElement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FetchVideo(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog Dialog = new Microsoft.Win32.OpenFileDialog();
            
            Dialog.DefaultExt = "mp4";
            Dialog.Filter = "AVI Videos (*.avi)|*.avi|MKV Videos (*.mkv)|*.mkv|MP4 Videos (*.mp4)|*.mp4|MPG Videos (*.mpg)|*.mpg|WMV Videos (*.wmv)|*.wmv";

            try
            {
                if (Dialog.ShowDialog() == true)
                {
                    VideoFileName.Text = Dialog.FileName;
                    VideoPlaying.Source = new Uri(Dialog.FileName);
                }
            }
            catch (Exception exc)
            {
                Subtitles.Text = exc.Message;
            }
        }
        
        /// <summary>
        /// Permet de parcourir un fichier de sous-titres a afficher pendant la lecture de la vidéo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FetchSubtitles(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog Dialog = new Microsoft.Win32.OpenFileDialog();
            
            Dialog.DefaultExt = "srt";
            Dialog.Filter = "SRT File (*.srt)|*.srt";

            try
            {
                if (Dialog.ShowDialog() == true && File.Exists(Dialog.FileName))
                {
                    SubtitleFileName.Text = Dialog.FileName;
                    Subs = new SubtitlesHandler(Dialog.FileName);
                }
            }
            catch (Exception exc)
            {
                Subtitles.Text = exc.Message; 
            }
            
        }
        
        /// <summary>
        /// Lance la lecture de la vidéo et des sous-titres
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PlayVideo(object sender, RoutedEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null && !IsPlaying)
            {
                VideoPlaying.Play();
                Timer.Start();
                IsPlaying = true;
                if (Subs != null && (!IsSubTaskRunning || IsPaused))
                {
                    IsSubTaskRunning = true;
                    Token = new CancellationTokenSource();
                    if (IsPaused)
                        await Task.WhenAll(Subs.DisplaySubtitles(Subtitles, VideoPlaying, Token.Token, true));
                    else
                        await Task.WhenAll(Subs.DisplaySubtitles(Subtitles, VideoPlaying, Token.Token, false));
                }
            }
        }
        
        /// <summary>
        /// Mets en pause la vidéo et les sous-titres (le sous-titre en cours d'affichage ne se fige pas, d'où le décalage à la reprise)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseVideo(object sender, RoutedEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null && IsPlaying)
            {
                VideoPlaying.Pause();
                IsPlaying = false;
                IsPaused = true;
                if (Token != null)
                    Token.Cancel();
            }
        }
        
        /// <summary>
        /// Arrête la vidéo et les sous-titres
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopVideo(object sender, RoutedEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null)
            {
                VideoPlaying.Stop();
                IsPlaying = false;
                IsSubTaskRunning = false;
                if (Token != null)
                    Token.Cancel();
            }
        }

        
        /// <summary>
        /// Redémarre la vidéo et les sous-titres au début
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RestartVideo(object sender, RoutedEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null)
            {
                StopVideo(sender, e);
                PlayVideo(sender, e);
                IsPlaying = true;
            }
        }

        /// <summary>
        /// Gestion mouvement souris avec clic gauche dans la barre de volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && MouseIsCaptured && VideoPlaying != null)
            {
                double X = e.GetPosition(VolumeBar).X;
                double Ratio = X / VolumeBar.ActualWidth;
                VideoPlaying.Volume = Ratio;
                VolumeBar.Value = Ratio * VolumeBar.Maximum;
            }
        }

        /// <summary>
        /// Gestion clic gauche souris dans la barre de volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (VideoPlaying != null)
            {
                MouseIsCaptured = true;
                double X = e.GetPosition(VolumeBar).X;
                double Ratio = X / VolumeBar.ActualWidth;
                VideoPlaying.Volume = Ratio;
                VolumeBar.Value = Ratio * VolumeBar.Maximum;
            }
        }

        /// <summary>
        /// Gestion du lacher de clic sur la barre de volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeMouseUp(object sender, MouseButtonEventArgs e)
        {
            VolumeBar.Value = e.GetPosition(VolumeBar).X / VolumeBar.ActualWidth * VolumeBar.Maximum;
            MouseIsCaptured = false;
        }

        /// <summary>
        /// Gestion du mouvement de souris au clic dans la barre de temps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimeMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && MouseIsCaptured && VideoPlaying != null && VideoPlaying.Source != null && VideoPlaying.NaturalDuration.HasTimeSpan)
            {
                double X = e.GetPosition(TimeBar).X;
                double Ratio = X / TimeBar.ActualWidth;
                TimeBar.Value = Ratio * TimeBar.Maximum;
                VideoPlaying.Position = TimeSpan.FromMilliseconds(TimeBar.Value / 100 * VideoPlaying.NaturalDuration.TimeSpan.TotalMilliseconds);
                if (Token != null)
                    Token.Cancel();
                Token = new CancellationTokenSource();
                await Task.WhenAll(Subs.DisplaySubtitles(Subtitles, VideoPlaying, Token.Token, true));
            }
        }

        /// <summary>
        /// Gestion du clic dans la barre de temps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null && VideoPlaying.NaturalDuration.HasTimeSpan)
            {
                MouseIsCaptured = true;
                double X = e.GetPosition(TimeBar).X;
                double Ratio = X / TimeBar.ActualWidth;
                TimeBar.Value = Ratio * TimeBar.Maximum;
                VideoPlaying.Position = TimeSpan.FromMilliseconds(TimeBar.Value / 100 * VideoPlaying.NaturalDuration.TimeSpan.TotalMilliseconds);
                if (Token != null)
                    Token.Cancel();
                Token = new CancellationTokenSource();
                await Task.WhenAll(Subs.DisplaySubtitles(Subtitles, VideoPlaying, Token.Token, true));
            }
        }

        /// <summary>
        /// Gestion du lacher de clic dans la barre de temps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimeMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (VideoPlaying != null && VideoPlaying.Source != null && VideoPlaying.NaturalDuration.HasTimeSpan)
            {
                TimeBar.Value = e.GetPosition(TimeBar).X / TimeBar.ActualWidth * TimeBar.Maximum;
                if (Token != null)
                    Token.Cancel();
                Token = new CancellationTokenSource();
                await Task.WhenAll(Subs.DisplaySubtitles(Subtitles, VideoPlaying, Token.Token, true));
                MouseIsCaptured = false;
            }
            
        }

        /// <summary>
        /// Gestion du compteur de position de la vidéo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer_Tick(object sender, EventArgs e)
        {
            if (VideoPlaying.Source != null && VideoPlaying.Source != null && VideoPlaying.NaturalDuration.HasTimeSpan)
            {
                if (VideoPlaying.NaturalDuration.HasTimeSpan)
                {
                    TimerLabel.Content = String.Format(VideoPlaying.Position.ToString(@"hh\:mm\:ss"));
                    TimeBar.Value = VideoPlaying.Position.TotalMilliseconds / VideoPlaying.NaturalDuration.TimeSpan.TotalMilliseconds * 100;
                }
            }
            else
                TimerLabel.Content = "00:00:00";
        }
    }
}
