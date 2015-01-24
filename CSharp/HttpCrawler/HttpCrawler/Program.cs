using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("usage: HttpCrawler.exe <url>");
                Console.ReadKey(true);
                return;
            }

            var httpUri = new Uri(args[0]);

            
            HttpCrawler crawler = new HttpCrawler();
            crawler.Crawl(httpUri);
            XDocument doc = new XDocument(
                new XElement("site",
                    SiteStructure.Instance.Links.Select(x=> new XElement("document", x.Href.AbsoluteUri)),
                    SiteStructure.Instance.Images.Select(x=> new XElement("image", x.Href.AbsoluteUri))
                    ));
            var fileName = MakeValidFileName(httpUri.AbsoluteUri) +".xml";
            doc.Save(fileName);
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }
    }
    public sealed class SiteStructure
    {
        static readonly SiteStructure _instance = new SiteStructure();

        List<CrawlInfoItem> _links = new List<CrawlInfoItem>();
        List<CrawlInfoItem> _images = new List<CrawlInfoItem>();

        public IEnumerable<CrawlInfoItem> Links
        {
            get { return _links; }
        }

        public IEnumerable<CrawlInfoItem> Images
        {
            get { return _images; }
        }

        public static SiteStructure Instance
        {
            get
            {
                return _instance;
            }
        }
        private SiteStructure()
        {
            // Initialize.
        }

        public void SetAsVisited(Uri link)
        {
            CrawlInfoItem uri = _links.FirstOrDefault(l => l.Href.AbsoluteUri == link.AbsoluteUri);
            if (uri != null)
            {
                uri.WasVisited = true;
            }

        }

        public bool WasVisited(Uri link)
        {
            CrawlInfoItem uri = _links.FirstOrDefault(l => l.Href.AbsoluteUri == link.AbsoluteUri);
            if (uri != null)
            {
                return uri.WasVisited;
            }
            return false;
        }

        public void AddLinks(IEnumerable<CrawlInfoItem> links)
        {
            var newItems = links.Where(x => _links.All(y => x.Href.AbsolutePath != y.Href.AbsolutePath));
            _links.AddRange(newItems);
        }

        public void AddImages(IEnumerable<CrawlInfoItem> images)
        {
            var newItems = images.Where(x => _images.All(y => x.Href.AbsolutePath != y.Href.AbsolutePath));
            _images.AddRange(newItems);
        }
    }

    public class HttpCrawler
    {
        private TcpClient _tcpClient = new TcpClient();

        private string CreateGetRequest(Uri uri)
        {
            string format = @"GET {0} HTTP/1.1
Host: {1}
User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; rv:35.0) Gecko/20100101 Firefox/35.0

";
            return string.Format(format, uri.PathAndQuery, uri.Host);

        }

        public void Crawl(Uri uri)
        {
            Thread.Sleep(1000);
            if (SiteStructure.Instance.WasVisited(uri))
            {
                return;
            }

            Console.WriteLine("Crawling: " + uri);
            string htmlDocument;
            htmlDocument = GetDocumentText(uri);
            var firstLineEnd =  htmlDocument.IndexOf("\n", System.StringComparison.Ordinal);
            string firstLine = htmlDocument.Substring(0, firstLineEnd);
            var tokens = firstLine.Split(' ');
            var responseCode = int.Parse(tokens[1]);
            if (responseCode != 200)
            {
                return;
            }

            var bodyStart = htmlDocument.IndexOf("\r\n\r\n", System.StringComparison.Ordinal);
            htmlDocument = htmlDocument.Substring(bodyStart, htmlDocument.Length - bodyStart);

            HtmlParser parser = new HtmlParser(htmlDocument, uri);
            SiteStructure.Instance.AddImages(parser.Images);
            var onlyLocalHostFiles = parser.Links.Where(link =>
                link.Href.Host == uri.Host
                &&
                (link.Href.LocalPath.EndsWith("htm", StringComparison.CurrentCultureIgnoreCase) ||
                link.Href.LocalPath.EndsWith("html", StringComparison.CurrentCultureIgnoreCase))
                );

            SiteStructure.Instance.SetAsVisited(uri);

            var localHostFiles = onlyLocalHostFiles as CrawlInfoItem[] ?? onlyLocalHostFiles.ToArray();
            SiteStructure.Instance.AddLinks(localHostFiles);
            foreach (var crawlInfoItem in localHostFiles)
            {
                var crawler = new HttpCrawler();
                crawler.Crawl(crawlInfoItem.Href);
                
            }
            
            
        }

        private string GetDocumentText(Uri uri)
        {
            string htmlDocument;
            _tcpClient.Connect(uri.Host, 80);
            using (var networkStream = _tcpClient.GetStream())
            {
                StreamWriter writer = new StreamWriter(networkStream);

                var getRequest = CreateGetRequest(uri);
                writer.Write(getRequest);
                writer.Flush();


                StreamReader reader = new StreamReader(networkStream);

                htmlDocument = reader.ReadToEnd();
                writer.Close();
                reader.Close();
            }
            _tcpClient.Close();
            return htmlDocument;
        }
    }

    public class HtmlParser
    {
        private string _htmlPage;
        private Uri _pageUri;

        public HtmlParser(string htmlPage, Uri pageUri)
        {
            _htmlPage = htmlPage;
            _pageUri = pageUri;
        }

        public IEnumerable<CrawlInfoItem> Links
        {
            get
            {
                List<CrawlInfoItem> list = new List<CrawlInfoItem>();

                // 1.
                // Find all matches in file.
                MatchCollection m1 = Regex.Matches(_htmlPage, @"(<a.*?>.*?</a>)",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                // 2.
                // Loop over each match.
                foreach (Match m in m1)
                {
                    string value = m.Groups[1].Value;


                    // 3.
                    // Get href attribute.
                    Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (m2.Success)
                    {
                        CrawlInfoItem i = new Document();
                        i.Href = CreateAbsoluteUrl(m2.Groups[1].Value);
                        list.Add(i);
                    }
                }
                return list;
            }
        }

        private Uri CreateAbsoluteUrl(string link)
        {

            var isAbsolute = Uri.IsWellFormedUriString(link, UriKind.Absolute);
            if (isAbsolute)
            {
                return new Uri(link);
            }
            var isRelative = Uri.IsWellFormedUriString(link, UriKind.Relative);
            if (isRelative)
            {
                return new Uri(_pageUri, link);
            }
            throw new ArgumentException("cannot create Absolute Url");
        }

        public IEnumerable<CrawlInfoItem> Images
        {
            get
            {
                List<CrawlInfoItem> list = new List<CrawlInfoItem>();

                // 1.
                // Find all matches in file.
                MatchCollection m1 = Regex.Matches(_htmlPage, @"(<img.*?>)",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                // 2.
                // Loop over each match.
                foreach (Match m in m1)
                {
                    string value = m.Groups[1].Value;

                    // 3.
                    // Get href attribute.
                    Match m2 = Regex.Match(value, @"src=\""(.*?)\""",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (m2.Success)
                    {
                        CrawlInfoItem i = new Image();
                        i.Href = CreateAbsoluteUrl(m2.Groups[1].Value);
                        list.Add(i);
                    }

                }
                return list;
            }

        }
    }

    public class CrawlInfoItem : IEquatable<CrawlInfoItem>
    {
        public Uri Href { get; set; }
        public bool WasVisited { get; set; }

        public CrawlInfoItem()
        {
            WasVisited = false;
        }


        public bool Equals(CrawlInfoItem other)
        {
            return other.Href == Href;
        }

        public override string ToString()
        {
            return Href.ToString();
        }
    }

    public class Document : CrawlInfoItem { }
    public class Image : CrawlInfoItem { }
    public class Email : CrawlInfoItem { }
}
