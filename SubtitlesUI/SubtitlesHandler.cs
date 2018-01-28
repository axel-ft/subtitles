using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SubtitlesUI
{
    class SubtitlesHandler
    {
        private List<Subtitle> Subtitles;
        private static Regex TimeParse;
        private static Regex OneDigit;

        private enum Limit { Begining, End };

        /// <summary>
        /// Lecture d'un fichier de sous-titres au format *.srt pour l'enregistrer dans une liste
        /// </summary>
        /// <param name="FilePath"></param>
        public SubtitlesHandler(string FilePath)
        {
            TimeParse = new Regex(@"\d{2}:\d{2}:\d{2},\d{3}");
            OneDigit = new Regex(@"^\d{1,}");

            Subtitles = new List<Subtitle>();

            try
            {
                using (StreamReader ReadSubtitiles = new StreamReader(FilePath))
                {
                    string Line;
                    int TempInit = 0, TempFromBegining = 0;
                    TimeSpan TempDuration = new TimeSpan();
                    int EndOfPreviousSubtitle = 0;
                    List<string> TempText = new List<string>();

                    while ((Line = ReadSubtitiles.ReadLine()) != null)
                    {
                        if (TimeParse.IsMatch(Line) && RetrieveMillisecondsFromTime(Line, Limit.End) != -1 && RetrieveMillisecondsFromTime(Line, Limit.Begining) != -1)
                        {
                            TempFromBegining = RetrieveMillisecondsFromTime(Line, Limit.Begining);
                            TempInit = RetrieveMillisecondsFromTime(Line, Limit.Begining) - EndOfPreviousSubtitle;
                            TempDuration = TimeSpan.FromMilliseconds(RetrieveMillisecondsFromTime(Line, Limit.End) - RetrieveMillisecondsFromTime(Line, Limit.Begining));
                            EndOfPreviousSubtitle = RetrieveMillisecondsFromTime(Line, Limit.End);
                        }

                        if (Line != "" && !OneDigit.IsMatch(Line))
                        {
                            TempText.Add(Line);
                            if (!ReadSubtitiles.EndOfStream)
                                continue;
                        }

                        if (Line == "" || ReadSubtitiles.EndOfStream)
                        {
                            Subtitles.Add(new Subtitle(new List<string>(TempText), TempInit, TempDuration, TempFromBegining));                                
                            TempInit = 0;
                            TempDuration = TimeSpan.FromMilliseconds(0);
                            TempText.Clear();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Transforme les temps lus dans le fichiers en durées en millisecondes
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="BeginingOrEnd"></param>
        /// <returns></returns>
        private int RetrieveMillisecondsFromTime(string Input, Limit BeginingOrEnd)
        {
            int Hours, Minutes, Seconds, Milliseconds;

            if (TimeParse.IsMatch(Input) && (BeginingOrEnd >= 0 && (int)BeginingOrEnd <= 1))
            {
                string StringInitTime = TimeParse.Matches(Input)[(int)BeginingOrEnd].ToString();
                Hours = int.Parse(StringInitTime.Split(':')[0]) * 60 * 60 * 1000;
                Minutes = int.Parse(StringInitTime.Split(':')[1]) * 60 * 1000;
                Seconds = int.Parse(StringInitTime.Split(':')[2].Split(',')[0]) * 1000;
                Milliseconds = int.Parse(StringInitTime.Split(':')[2].Split(',')[1]) + Seconds + Minutes + Hours;

                return Milliseconds;
            }

            return -1;
        }

        /// <summary>
        /// Trouve les sous-titres restants à afficher en fonction de la position de la vidéo. Sera utilisé pour la gestion de la barre de temps
        /// </summary>
        /// <param name="VideoPlaying"></param>
        /// <returns></returns>
        private List<Subtitle> NextSubs(MediaElement VideoPlaying)
        {
            List<Subtitle> NextSubs = new List<Subtitle>();

            foreach (Subtitle Sub in Subtitles)
            {
                if (Sub.FromBegining >= VideoPlaying.Position.TotalMilliseconds)
                    NextSubs.Add(Sub);
            }

            return NextSubs;
        }

        /// <summary>
        /// Boucle qui affiche les sous-titres au moment souhaité
        /// </summary>
        /// <param name="SubsTextBlock"></param>
        /// <param name="VideoPlaying"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task DisplaySubtitles(TextBlock SubsTextBlock, MediaElement VideoPlaying, CancellationToken Token, bool Resume)
        {
            List<Subtitle> InitOrResume;

            if (Resume)
                InitOrResume = NextSubs(VideoPlaying);
            else
                InitOrResume = Subtitles;

            foreach (Subtitle Sub in InitOrResume)
            {
                try
                {
                    if (Resume && Sub.FromBegining - (int)VideoPlaying.Position.TotalMilliseconds  > 0)
                    {
                        await Task.Delay(Sub.FromBegining - (int)VideoPlaying.Position.TotalMilliseconds, Token);
                        Resume = false;
                    }
                    else 
                        await Task.Delay(Sub.Init, Token);

                    if (Sub.IsItalic())
                        SubsTextBlock.FontStyle = FontStyles.Italic;
                    SubsTextBlock.Text = Sub.ToString();
                } catch
                {
                    SubsTextBlock.Text = "";
                    return;
                }

                try
                {
                    await Task.Delay(Sub.Duration, Token);
                    if (SubsTextBlock.FontStyle == FontStyles.Italic)
                        SubsTextBlock.FontStyle = FontStyles.Normal;
                    SubsTextBlock.Text = "";
                }
                catch
                {
                    SubsTextBlock.Text = "";
                    return;
                }
            }
        }
    }
}
