using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SubtitlesUI
{
    class Subtitle
    {
        private List<string> _Text;
        private int _Init;
        private int _FromBegining;
        private TimeSpan _Duration;
        private Regex InItalic = new Regex(@"(?<=<i>)(.*?)(?=<\/i>)");
        private Regex BeginItalic = new Regex("<i>"),
                EndItalic = new Regex("</i>");

        public Subtitle(List<string> Text, int Init, TimeSpan Duration, int FromBegining)
        {
            _Text = Text;
            _Init = Init;
            _Duration = Duration;
            _FromBegining = FromBegining;
        }

        public List<string> Text { get => _Text;}
        public int Init { get => _Init;}
        public int FromBegining { get => _FromBegining; }
        public TimeSpan Duration { get => _Duration;}

        /// <summary>
        /// Détermine si le sous-titre est à écrire en italique
        /// </summary>
        /// <returns></returns>
        public bool IsItalic()
        {
            bool Begin = false,
                   End = false;

            foreach (string Line in Text)
            {
                if (InItalic.IsMatch(Line))
                    return true;

                if (BeginItalic.IsMatch(Line))
                    Begin = true;

                if (EndItalic.IsMatch(Line))
                    End = true;
            }

            if (Begin && End)
                return true;
            else return false;
        }

        /// <summary>
        /// Transforme la ou les lignes d'un sous-titre en string pour l'affichage
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ToString = "";

            foreach (string Line in _Text)
            {
                if (InItalic.IsMatch(Line))
                    ToString += InItalic.Match(Line).ToString() + "\n";
                else if (BeginItalic.IsMatch(Line))
                    ToString += BeginItalic.Split(Line)[1].ToString() + "\n";
                else if (EndItalic.IsMatch(Line))
                    ToString += EndItalic.Split(Line)[0].ToString() + "\n";
                else
                    ToString += Line + "\n";
            }

            return ToString;
        }
    }
}
