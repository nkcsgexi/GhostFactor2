using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using warnings.algorithms;

namespace warnings.util
{
    public class StringUtil
    {
        /* Concatenate all the strings in an array into one single string. */
        public static String ConcatenateAll(String seperator, String[] texts)
        {
            if (texts.Count() == 0)
                return "";
            var sb = new StringBuilder(texts[0]);
            for (int i = 1; i < texts.Count(); i++ )
            {
                sb.Append(seperator + texts[i]);
            }
            return sb.ToString();
        }

        /* Same as above, different input. */
        public static String ConcatenateAll(String seperator, IEnumerable<String> texts)
        {
            return ConcatenateAll(seperator, texts.ToArray());
        }


        /* Replace the specified start(length) in a text with the replacer. */
        public static String ReplaceWith(String text, String replacer, int start, int length)
        {
            var sb = new StringBuilder();
            sb.Append(text.Substring(0, start));
            sb.Append(replacer);
            sb.Append(text.Substring(start + length));
            return sb.ToString();
        }

        private static Levenshtein distance = new Levenshtein();

        /* Get the distance between two strings. */
        public static int GetStringDistance(String a, String b)
        {
            return distance.LD(a, b);
        }

        /* Get the distance concerning the original length of two strings, scale to [0..1]. */
        public static double GetScaledDistance(string a, string b)
        {
            var distance = (double) GetStringDistance(a, b);
            var length = (double) Math.Max(a.Length, b.Length);
            return distance/length;
        }


        /* Convert an array of bytes to a string. */
        public static String Bytes2String(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /* Replace the new line in a text with multiple lines to something else as specified. */
        public static String ReplaceNewLine(String text, String replacement)
        {
            return text.Replace(Environment.NewLine, replacement);
        }

        /* Get the longest common string of two strings. */
        public static string GetLongestCommonString(string s1, string s2)
        {
            var str1 = s1.ToCharArray();
            var str2 = s2.ToCharArray();

            var l = new int[str1.Length, str2.Length];
            int lcs = -1;
            string substr = string.Empty;
            int end = -1;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] == str2[j])
                    {
                        if (i == 0 || j == 0)
                        {
                            l[i, j] = 1;
                        }
                        else
                            l[i, j] = l[i - 1, j - 1] + 1;
                        if (l[i, j] > lcs)
                        {
                            lcs = l[i, j];
                            end = i;
                        }

                    }
                    else
                        l[i, j] = 0;
                }
            }

            for (int i = end - lcs + 1; i <= end; i++)
            {
                substr += str1[i];
            }

            return substr;
        } 

    }
}
