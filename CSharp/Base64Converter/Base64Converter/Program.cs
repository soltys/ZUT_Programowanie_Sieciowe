using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            ApplicationMode mode = ApplicationMode.Invalid;
            string inputFile = "";
            string outputFile = "";
            foreach(var inputArg in args)
            {
                var arg = inputArg .Trim();
                if (arg == "-k")
                {
                    mode = ApplicationMode.Encode;
                    continue;
                }
                else if (arg == "-d")
                {
                    mode = ApplicationMode.Decode;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(inputFile))
                {
                    inputFile = arg;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    outputFile = arg;
                    continue;
                }
            }

            Base64Converter conv = new Base64Converter();

            if (mode == ApplicationMode.Encode)
            {
                var encoded = conv.Encode(File.ReadAllBytes(inputFile));    
                File.WriteAllText(outputFile, encoded);

            }
            else if (mode == ApplicationMode.Decode)
            {
                var decoded = conv.Decode(File.ReadAllText(inputFile));
                File.WriteAllBytes(outputFile, decoded);
            }
            else
            {
                Console.WriteLine("You must use -k to encode OR -d to decode");
            }
        }

    }

    public enum ApplicationMode
    {
        Invalid,
        Encode,
        Decode
    }
}
