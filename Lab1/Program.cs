using System;
using System.IO;
using System.Text;

namespace Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "/Users/coladaadminuser/Documents/university/7sem/sharp/Lab1/Lab1/";

            var file = path + args[0];
            var filename = file[..file.IndexOf('.')];
            var format = args[1];

            var i = 1;
            while (File.Exists($"{filename}{i:000}.txt"))
            {
                i += 1;
            }

            Console.WriteLine(file);

            var fileText = File.ReadAllText(file);
            var updatedLine = new StringBuilder();

            var linesDos = fileText.Split("\r\n");
            foreach (var lineD in linesDos)
            {
                var lineUnix = lineD.Split("\n");
                foreach (var lineU in lineUnix)
                {
                    updatedLine.Append(lineU + (format == "DOS" ? "\r\n" : "\n"));
                }
            }

            File.WriteAllText($"{filename}{i:000}.txt", updatedLine.ToString());
        }
    }
}
