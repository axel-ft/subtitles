using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Subtitles
{
    class SubtitlesHandler
    {
        private List<Subtitle> Subtitles;
        private static Regex TimeParse;
        private static Regex OneDigit;

        private enum Limit { Begining, End };

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
                    int TempInit = 0;
                    TimeSpan TempDuration = new TimeSpan();
                    int EndOfPreviousSubtitle = 0;
                    List<string> TempText = new List<string>();

                    while ((Line = ReadSubtitiles.ReadLine()) != null)
                    {
                        if (TimeParse.IsMatch(Line) && RetrieveMillisecondsFromTime(Line, Limit.End) != -1 && RetrieveMillisecondsFromTime(Line, Limit.Begining) != -1)
                        {
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
                            Subtitles.Add(new Subtitle(new List<string>(TempText), TempInit, TempDuration));                                
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

        public async Task ReadLoop()
        {
            foreach (Subtitle Sub in Subtitles)
            {
                await DisplaySubtitle(Sub);
            }
        }

        public async Task DisplaySubtitle(Subtitle Sub)
        {
            await Task.Delay(Sub.Init);
            Console.WriteLine(Sub.ToString());
            await Task.Delay(Sub.Duration);
            Console.Clear();
        }
    }
}
