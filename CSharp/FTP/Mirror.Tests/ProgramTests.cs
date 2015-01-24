using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApplication2;
using NUnit.Framework;

namespace Mirror.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        [Test]
        public void ArgumentParsing()
        {
            var mirrorArgument = Program.ParseArguments(new string[]
            {
                "user@ftp.server/dir/dir",
                "localdir"
            });

            Assert.AreEqual("user", mirrorArgument.UserName);
            Assert.AreEqual("ftp.server", mirrorArgument.Uri);
            Assert.AreEqual("/dir/dir", mirrorArgument.RemoteDir);
            Assert.AreEqual("localdir", mirrorArgument.LocalDir);



        }

        [Test]
        public void FileListParsing()
        {
            
            var ftpListParser = new FtpListParser(File.ReadAllText("ftp_list.txt"));
            
        }

    }
}
