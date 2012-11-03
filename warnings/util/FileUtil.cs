using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NLog;

namespace warnings.util
{
    public class FileUtil
    {
        private static Logger logger = NLoggerUtil.GetNLogger(typeof (FileUtil));


        /* Read all the lines in a given file. */
        public static IEnumerable<string> ReadFileLines(string path)
        {
            return ReadFileLines(path, int.MinValue, int.MaxValue);
        }

        /* 
         * Read specified lines in a file, starts with line start to line end, inclusively.
         * the minumum value of start is 0.
         */
        public static String[] ReadFileLines(String path, int start, int end)
        {
            var lines = new List<string>();
            int counter = 0;
            string line;
            var file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null){
                Console.WriteLine(line);
                if(counter >= start && counter <= end){
                    lines.Add(line);
                }
                if(counter > end)
                    break;
                counter++;
            }
            file.Close();
            return lines.ToArray();
        }

        //Xi: to read a file, from the specified line number to the end of such file.
        public static String ReadFileFromLine(String path, int start)
        {
            var sb = new StringBuilder();
            int end = int.MaxValue;
            String[] lines = ReadFileLines(path, start, end);
            foreach(String line in lines){
                sb.Append(line);
            }
            return sb.ToString();
        }

        //Xi: append multiple lines to a file with specified path.
        public static void AppendMultipleLines(string path, string[] lines)
        {
            File.AppendAllLines(path, lines);
        }

        //Xi: create file by the given path.
        public static FileStream CreateFile(string path)
        {
            if (File.Exists(path)){
                File.Delete(path);
            }
            return File.Create(path);
        }

        
        public static void WriteToFileStream(FileStream fs, string text)
        {
            Byte[] info = new UTF8Encoding(true).GetBytes(text);
            fs.Write(info, 0, info.Length);
            fs.Close();
        }

        public static string ReadAllText(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }catch(Exception e)
            {
                logger.Fatal(e);
                return null;
            }
        }

        public static void Delete(string sourcePath)
        {
            if (File.Exists(sourcePath))
                File.Delete(sourcePath);
        }

        /* Create a directory if such directory does not exist. */
        public static void CreateDirectory(string root)
        {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
        }

        /* 
         * Delete a directory if such directory exists. All the contents in the directory will be 
         * deleted. 
         */
        public static void DeleteDirectory(String root)
        {
            if(Directory.Exists(root))
                Directory.Delete(root, true);
        }

        /* Check whether a given url's file exist remotely. */
        public static bool DoesRemoteFileExist(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                var request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                var response = request.GetResponse() as HttpWebResponse;
                //Returns TURE if the Status code == 200
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }
    }
}
