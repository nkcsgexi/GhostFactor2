using System;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace WarningTest
{
    [TestClass]
    public class DummyTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var assignment = Syntax.ParseExpression("i = 1");
            Assert.IsNotNull(assignment);
            RegionDirectiveSyntax start;
            EndRegionDirectiveSyntax end;
        }
    }

    internal class change : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            IDocument doc;
        }
    }
}
