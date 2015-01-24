using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    internal class MirrorApplication
    {
        private MirrorArguments _mirrorArguments;
        private TcpClient tcpClient;
        
        const int BufferSize = 2024;
        readonly byte[] _buffer = new byte[BufferSize];
        public MirrorApplication(MirrorArguments mirrorArguments)
        {
            _mirrorArguments = mirrorArguments;
        }

        public void MakeMirror()
        {
            tcpClient = new TcpClient();
            
            tcpClient.Connect(_mirrorArguments.Uri, 21);


            using (var networkStream = tcpClient.GetStream())
            {
                string message;
                ReadFromStream(networkStream);

                string cmd = "USER " + _mirrorArguments.UserName + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);

                Console.Clear();
                Console.WriteLine("Enter password [unix-style-without-star-and-stuff]: ");
                string password = EnterPasswordFromConsole();
                Console.Clear();


                cmd = "PASS " + password + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);


                cmd = "PASV" + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);

                cmd = "CWD " + _mirrorArguments.RemoteDir + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);

                string listData;


                listData = GoPassiveAndExecute(networkStream, "LIST *.*" + "\r\n");
                

                IList<FtpFileInfo> files;
                FtpListParser flp = new FtpListParser(listData);
                files = flp.FilesInfo;

                cmd = "TYPE I "  + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);
                byte[] fileData;
                foreach (var fi in files.Where(f => f.IsFile))
                {
                    cmd = "TYPE I " + "\r\n";
                    WriteToStream(cmd, networkStream);
                    ReadFromStream(networkStream);

                    fileData = GoPassiveAndGetBinary(networkStream, "RETR " + fi.FileName + "\r\n");
                    File.WriteAllBytes(Path.Combine(_mirrorArguments.LocalDir, fi.FileName), fileData);
                }

                cmd = "QUIT" + "\r\n";
                WriteToStream(cmd, networkStream);
                ReadFromStream(networkStream);

            }

            Console.WriteLine("All Files Downloaded (press any key to exit)");
            Console.ReadKey(true);

            tcpClient.Close();
        }

        private string GoPassiveAndExecute(NetworkStream networkStream, string executeCommand)
        {
            string listData;
            var passiveClient = GoPassive(networkStream);

            WriteToStream(executeCommand, networkStream);

            using (var passiveStream = passiveClient.GetStream())
            {
                listData = ReadFromStream(passiveStream);
            }

            passiveClient.Close();

            ReadFromStream(networkStream);
            ReadFromStream(networkStream);
            return listData;
        }

        private byte[] GoPassiveAndGetBinary(NetworkStream networkStream, string executeCommand)
        {
            byte[] downloadedBytes;
            var passiveClient = GoPassive(networkStream);

            WriteToStream(executeCommand, networkStream);

            using (var passiveStream = passiveClient.GetStream())
            {
                downloadedBytes = ReadFromStreamBytes(passiveStream);
            }

            passiveClient.Close();

            ReadFromStream(networkStream);
            ReadFromStream(networkStream);
            return downloadedBytes;
        }

        private TcpClient GoPassive(NetworkStream networkStream)
        {
            TcpClient passiveClient = new TcpClient();
            string cmd;
            string message;
            string listData;
            cmd = "PASV" + "\r\n";
            WriteToStream(cmd, networkStream);
            message = ReadFromStream(networkStream);
            var ipAndPortStart = message.IndexOf('(');
            var ipAndPort = message.Substring(ipAndPortStart, message.Length - ipAndPortStart)
                .Replace("(", "")
                .Replace(")", "");

            Console.WriteLine(ipAndPort);
            var ipAndPortTokens = ipAndPort.Split(',');
            var ip = new IPAddress(new byte[]
            {
                byte.Parse(ipAndPortTokens[0]),
                byte.Parse(ipAndPortTokens[1]),
                byte.Parse(ipAndPortTokens[2]),
                byte.Parse(ipAndPortTokens[3]),
            });

            var port = int.Parse(ipAndPortTokens[4])*256 + int.Parse(ipAndPortTokens[5]);

            passiveClient.Connect(ip, port);
            return passiveClient;
        }

        private static string EnterPasswordFromConsole()
        {
            string password;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                var consoleKeyInfo = Console.ReadKey(true);
                if (consoleKeyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
                sb.Append(consoleKeyInfo.KeyChar);
            }

            password = sb.ToString();
            return password;
        }

        private static void WriteToStream(string cmd, NetworkStream networkStream)
        {
            var package = GetBytes(cmd);
            networkStream.Write(package, 0, package.Length);
            networkStream.Flush();
        }

        private string ReadFromStream(NetworkStream networkStream)
        {
            string message = "";

            while (true)
            {
                var messageSize = networkStream.Read(_buffer, 0, BufferSize);
                if (messageSize <= 0)
                {
                    return message;
                }
                message += GetString(_buffer.Take(messageSize).ToArray());

                return message;    
            }
            
        }


        private byte[] ReadFromStreamBytes(NetworkStream networkStream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = networkStream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }

        }


        static byte[] GetBytes(string str)
        {
            return System.Text.Encoding.ASCII.GetBytes(str);
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            return System.Text.Encoding.ASCII.GetString(bytes);
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}