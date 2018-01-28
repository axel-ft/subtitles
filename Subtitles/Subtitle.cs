using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subtitles
{
    class Subtitle
    {
        private List<string> _Text;
        private int _Init;
        private TimeSpan _Duration;

        public Subtitle(List<string> Text, int Init, TimeSpan Duration)
        {
            _Text = Text;
            _Init = Init;
            _Duration = Duration;
        }

        public List<string> Text { get => _Text;}
        public int Init { get => _Init;}
        public TimeSpan Duration { get => _Duration;}

        public override string ToString()
        {
            string ToString = "";

            foreach (string Line in _Text)
            {
                ToString += Line + "\n";
            }

            return ToString;
        }
    }
}
