using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subtitles
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            FilePath += @"\source\repos\Subtitles\Subtitles\Public\Avengers.srt";
            
            SubtitlesHandler Test = new SubtitlesHandler(FilePath);
            Task.WaitAll(Test.ReadLoop());

            Console.ReadKey();
        }
    }
}
