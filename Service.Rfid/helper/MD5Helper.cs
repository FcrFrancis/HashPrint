using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Service.Rfid
{
    public class MD5Helper
    {
        public static string MakeMD5(string original)
        {
            return MakeMD5(original, Encoding.UTF8);
        }

        public static string MakeMD5(string original, Encoding encoding)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] md5Hash = md5.ComputeHash(encoding.GetBytes(original));
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < md5Hash.Length; i++)
            {
                stringBuilder.Append(md5Hash[i].ToString("x2"));
            }
            return stringBuilder.ToString();
        }
    }
}
