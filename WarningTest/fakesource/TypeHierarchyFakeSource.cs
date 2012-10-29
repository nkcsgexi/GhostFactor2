using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarningTest.fakesource
{
    interface IFake
    {
        
    }

    interface IFakeSource : IFake
    {
        
    }

    class FakeSource : IFakeSource
    {
        
    }

    class HierarchyFakeSource : FakeSource
    {
        
    }


    class TypeHierarchyFakeSource : HierarchyFakeSource
    {

        private class InnerClassLevelOne1
        {
            private class InnerClassLevelTwo
            {
                
            }
        }

        private class InnerClassLevelOne2
        {
            
        }

        private class InnerClassLevelOne3
        {
            
        }
    }
}
