using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication2
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Error");
                Console.WriteLine("Usage: mirror user@host/remoteDir localDir [-n]");
                Console.ReadKey(true);
                return;
            }

           var mirrorArguments =  ParseArguments(args);

            var mirror = new MirrorApplication(mirrorArguments);
            try
            {
                mirror.MakeMirror();
            }
            catch
            {
                Console.WriteLine("Something went terribly wrong!");
                Console.WriteLine("(press any key to exit)");
                Console.ReadKey(true);
            }
            
         
        }

        public static MirrorArguments ParseArguments(string[] args)
        {
            var host = args[0];
            var localDir = args[1];

            var tokens = host.Split('@');
            var userName = tokens[0];
            var uri = tokens[1];

            var remoteFolderStart = uri.IndexOf('/');
            var remoteDir = uri.Substring(remoteFolderStart, uri.Length - remoteFolderStart);
            uri = uri.Substring(0, remoteFolderStart);
            

            return new MirrorArguments
            {
                UserName = userName,
                Uri = uri,
                LocalDir = localDir,
                RemoteDir = remoteDir
            };


            
        }
    }

    public class MirrorArguments
    {
        public string LocalDir { get; set; }
        public string Uri { get; set; }
        public string UserName { get; set; }
        public string RemoteDir { get; set; }
    }
}
