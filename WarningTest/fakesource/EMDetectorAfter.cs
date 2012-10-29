using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarningTest.fakesource.after
{
    public class FakeClass
    {
        private int field;

        public void method1()
        {
            int k;
            k = 2;
            extracted1();
        }
        private void extracted1()
        {
            int i;
            int j;
            i = j = 10;
        }

        public void method2()
        {
            int i = 0;
            int j = 0;
            extracted2(i, j);
            Console.WriteLine(i);
        }

        private void extracted2(int i, int j)
        {
            i = i + j;
        }

        public int method3()
        {
            int field = 0;
            return extracted3();
        }

        private int extracted3()
        {
            field = field + 1;
            return field;
        }

        public int method4()
        {
            int i = 1;
            int j = 1;

            extracted4(j);
            return i;
        }

        private int extracted4(int j)
        {
            int i = 1;
            return i + j;
        }
    }
}
