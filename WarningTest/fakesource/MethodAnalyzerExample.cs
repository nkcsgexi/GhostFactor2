using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarningTest.fakesource
{
    class MethodAnalyzerExample
    {
        private int field1;
        private int field2, field3;

        public void method1()
        {
            int variable4;
            int variable5;
            variable4 = 1;
        }

        public int method2()
        {
            return 1;
        }

        public IEnumerable method3()
        {
            return Enumerable.Empty<int>();
        }

        public void method4(int a)
        {
        }

        public IEnumerable<Object> method5(int a, int b, bool c, Object d, IEnumerable<int> e)
        {
            var list = new List<int>();
            list.Add(1);
            list.Where(n => n > 0).First();
            method3();
            return Enumerable.Empty<Object>();
        }

        public void method6(int a, int b, int c)
        {
            int d = a + b;
            int e = b + c;
            var list = new List<int>();
            list.Add(a);
            list.Add(c);
            var m =1;
        }

        public void method7(out IEnumerable<IEnumerable<object>> things)
        {
            things = null;
        }

    }
}
