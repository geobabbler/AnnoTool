using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Zekiah.Samples
{
    //Simple container class for font properties
    public class Font
    {
        public FontFamily Family { get; set; }
        public FontStyle Style { get; set; }
        public FontWeight Weight { get; set; }
        public double Size { get; set; }
    }

    public static class Extensions
    {
        //Extension method on System.String
        public static Size Measure(this String str, Font font)
        {
            Size retval = new Size();
            try
            {
                TextBlock l = new TextBlock();
                l.FontFamily = font.Family;
                l.FontSize = font.Size;
                l.FontStyle = font.Style;
                l.FontWeight = font.Weight;
                l.Text = str;
                ////l.UpdateLayout();
                retval.Height = l.ActualHeight;
                retval.Width = l.ActualWidth;
            }
            catch
            {
                retval.Height = 0;
                retval.Width = 0;
            }
            return retval;
        }
    }
}
