using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WarningTest.fakesource
{
    class DataFlowExample
    {
        private string[] method1(string path, int start, int end)
        {
            List<String> lines = new List<string>();
            int counter = 0;
            string line;
            StreamReader file =
               new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (counter >= start && counter <= end)
                {
                    lines.Add(line);
                }
                if (counter > end)
                    break;
                counter++;
            }
            return lines.ToArray();
       
        }

        private void method2()
        {
            int a = 0;
            int b = 0;
            
            b ++;
            a = b + 1;

            a ++;

        }
        private void method3()
        {

        }
        private void method4()
        {

        }
    }
}
