using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using warnings.util;

namespace warnings.refactoring
{
    public enum RefactoringType
    {
        RENAME,
        EXTRACT_METHOD,
        CHANGE_METHOD_SIGNATURE,
        INLINE_METHOD,
        UNKOWN
    }

    public enum RefactoringConditionType
    {
        EXTRACT_METHOD_PARAMETER,
        EXTRACT_METHOD_RETURN_VALUE,
        CHANGE_METHOD_SIGNATURE_UNUPDATED,
        INLINE_METHOD_MODIFIED_DATA
    }


    public class RefactoringTypeUtil
    {
        public static IEnumerable<RefactoringType> GetAllValidRefactoringTypes()
        {
            var types = Enum.GetValues(typeof(RefactoringType)).Cast<RefactoringType>();
            return types.Where(t => t != RefactoringType.UNKOWN);
        }

        public static string GetRefactoringTypeName(RefactoringType type)
        {
            var converter = new RefactoringType2StringConverter();
            return (string) converter.Convert(type, null, null, null);
        }

        /// <summary>
        /// Convert a refactoring type to a string to describe this type.
        /// </summary>
        private class RefactoringType2StringConverter : IValueConverter
        {
            /* Static strings to describe refactoring type names. */
            private const string EXTRACT_METHOD = "Extract method";
            private const string RENAME = "Rename";
            private const string CHANGE_METHOD_SIGNATURE = "Change method signature";
            private const string INLINE_METHOD = "Inline method";
            private const string UNKNOW = "Unknown refactoring type";

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var type = (RefactoringType)value;
                switch (type)
                {
                    case RefactoringType.RENAME:
                        return RENAME;
                    case RefactoringType.EXTRACT_METHOD:
                        return EXTRACT_METHOD;
                    case RefactoringType.CHANGE_METHOD_SIGNATURE:
                        return CHANGE_METHOD_SIGNATURE;
                    case RefactoringType.INLINE_METHOD:
                        return INLINE_METHOD;
                    default:
                        return UNKNOW;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
