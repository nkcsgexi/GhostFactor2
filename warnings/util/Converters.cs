using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Roslyn.Compilers;
using Roslyn.Services;
using warnings.refactoring;

namespace warnings.util
{

    /* Convert a BitMap to BitmapSource. */
    public class Bitmap2SourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        {
            // Converts a GDI bitmap to an image source
            Bitmap bmp = value as Bitmap;
            if (bmp != null)
            {
                BitmapSource bitmapSource =
                Imaging.CreateBitmapSourceFromHBitmap(
                bmp.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
                return bitmapSource;
            }
            return null;
        }

        public object ConvertBack(object value,
        Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /* This converts from string to IDocument and vice versa. */
    public class String2IDocumentConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert to string.
            String code = (String)value;
            var solutionId = SolutionId.CreateNewId();
            var projectId = ProjectId.CreateNewId(solutionId, "shadow");
            var documentId = DocumentId.CreateNewId(projectId, "shadow");
            var solution = Solution.Create(solutionId).AddProject(projectId, "shadow", "shadow", LanguageNames.CSharp)
                .AddDocument(documentId, "shadow", new StringText(code));
            return solution.GetDocument(documentId);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IDocument doc = (IDocument) value;
            return doc.GetText();
        }
    }
   
}
