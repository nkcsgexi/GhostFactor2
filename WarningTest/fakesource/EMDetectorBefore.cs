using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarningTest.fakesource.before
{
    public class FakeClass
    {
        private int field = 0;

        public void method1()
        {
            int i;
            int j;
            i = j = 10;
        }

        public void method2()
        {
            int i = 0; 
            int j = 0;
            i = i + j;
            Console.WriteLine(i);       
        }

        public int method3()
        {
            int field = 0;
            field = field + 1;
            return field;
        }

        public int method4()
        {
            int i = 1;
            int j = 1;

            i = i + j;
            return i;
        }
    }
}
