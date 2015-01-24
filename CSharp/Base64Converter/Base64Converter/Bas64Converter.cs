using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Converter
{
    class Base64Converter
    {
        public Base64Converter()
        {
            base64Table = new char[] {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d',
            'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',  't','u','v','w','x','y','z','0','1','2','3','4','5','6','7',  
            '8','9','+','/' };

            base64TableToByte = ConstructMapBase64();
        }
        static char[] base64Table;
        static byte[] base64TableToByte;

        private static byte[] ConstructMapBase64()
        {
            byte[] mapBase64 = new byte[255 + 1];
            for (int i = 0; i < mapBase64.Length; i++)
            {
                mapBase64[i] = 0xff;
            }
            for (int i = 0; i < base64Table.Length; i++)
            {
                mapBase64[(int)base64Table[i]] = (byte)i;
            }
            return mapBase64;
        }

        #region Encode
        public string Encode(byte[] data)
        {
            var remainder = (3 - (data.Length % 3)) % 3;
            var goodData = DataPadding(data, remainder);

            string encodedString = InternalEncode(goodData);

            encodedString = ReplaceLastCharsWithEquals(remainder, encodedString);

            encodedString = SplitInLines(encodedString, 76);

            return encodedString;

        }

        private static string ReplaceLastCharsWithEquals(int remainder, string encodedString)
        {
            char[] chars = encodedString.ToCharArray();
            for (int i = encodedString.Length - remainder; i < encodedString.Length; i++)
            {
                chars[i] = '=';
            }
            encodedString = new string(chars);

            return encodedString;
        }

        private static byte[] DataPadding(byte[] data, int remainder)
        {
            var goodData = new byte[data.Length + remainder];

            Array.Copy(data, goodData, data.Length);
            for (int i = goodData.Length - remainder; i < goodData.Length; i++)
            {
                goodData[i] = 0;
            }
            return goodData;
        }

        private string InternalEncode(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int packStart = 0; packStart < data.Length; packStart += 3)
            {
                byte[] tmpBytes = { data[packStart + 2], data[packStart + 1], data[packStart], 0 };

                string part = Encode24Word(BitConverter.ToInt32(tmpBytes, 0));

                sb.Append(part);
            }


            return sb.ToString();
        }

        private string Encode24Word(int b)
        {

            int end = b & 0x3f;
            b >>= 6;
            int middle = b & 0x3f;
            b >>= 6;
            int front = b & 0x3f;
            b >>= 6;
            int superFront = b & 0x3f;

            string output = ToBase64(superFront) + ToBase64(front) + ToBase64(middle) + ToBase64(end);

            return output;
        }

        public string ToBase64(int value)
        {
            return base64Table[value].ToString();
        }




        static string SplitInLines(string str, int chunkSize)
        {
            if ((str.Length / chunkSize) == 0)
                return str;
            return string.Join(Environment.NewLine, Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize)));
        }



        #endregion

        public byte[] Decode(string base64String)
        {
            List<byte> bytes = new List<byte>();
            base64String = base64String.Replace("\n","");
            base64String = base64String.Replace("\r", "");
            var chunks = SplitInChunks(base64String, 4);
            foreach (var chunk in chunks)
            {
                var chunkArray = chunk.ToCharArray();

                int value = 0;
                for (int i = 0, shiftLeft = 18; i < 4; i++, shiftLeft -= 6)
                {
                    if (chunkArray[i] == '=')
                    {
                        continue;
                    }
                    value += (base64TableToByte[chunkArray[i]] << shiftLeft);
                }

                var bvalue = BitConverter.GetBytes(value);
                var equalSigns = chunkArray.Count(c => c == '=');
                var takeBytes = 3 - equalSigns;
                bytes.AddRange(bvalue.Reverse().Skip(1).Take(takeBytes));
            }

            return bytes.ToArray();
        }



        static IList<string> SplitInChunks(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize)).ToList();
        }
    }
}
