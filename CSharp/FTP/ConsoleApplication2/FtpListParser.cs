using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    public class FtpListParser
    {
        private string _listData;

        public FtpListParser(string listData)
        {
            _listData = listData;
            Parse();
        }

        public IList<FtpFileInfo> FilesInfo { get; set; }

        private void Parse()
        {

            FilesInfo = new List<FtpFileInfo>();

            var lines = _listData.Split('\n');
            string regex =
                       @"^" +
                       @"(?<dir>[\-ld])" +
                       @"(?<permission>[\-rwx]{9})" +
                       @"\s+" +
                       @"(?<filecode>\d+)" +
                       @"\s+" +
                       @"(?<owner>\w+)" +
                       @"\s+" +
                       @"(?<group>\w+)" +
                       @"\s+" +
                       @"(?<size>\d+)" +
                       @"\s+" +
                       @"(?<month>\w{3})" +
                       @"\s+" +
                       @"(?<day>\d{1,2})" +
                       @"\s+" +
                       @"(?<timeyear>[\d:]{4,5})" +
                       @"\s+" +
                       @"(?<filename>(.*))" +
                       @"$";

            var reg = new Regex(regex);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var copy = line.Replace("\r", "");
                var split = reg.Match(copy);
                var dir = split.Groups["dir"].ToString();
                var permission = split.Groups["permission"].ToString();
                var filecode = split.Groups["filecode"].ToString();
                var owner = split.Groups["owner"].ToString();
                var group = split.Groups["group"].ToString();
                var size = split.Groups["size"].ToString();
                var month = split.Groups["month"].ToString();
                var timeYear = split.Groups["timeyear"].ToString();
                var day = split.Groups["day"].ToString();
                var filename = split.Groups["filename"].ToString();

                var isFile = dir != "d";
                FilesInfo.Add(new FtpFileInfo
                {
                    IsFile =  isFile,
                    ContentLength = int.Parse(size),
                    FileName = filename
                });
            }

        }
    }

    public class FtpFileInfo
    {
        public bool IsFile { get; set; }
        public string FileName { get; set; }
        public int ContentLength { get; set; }
    }
}