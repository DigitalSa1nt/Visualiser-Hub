using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Visualiser_Hub.Utility_Classes
{
    class CustomBrushes
    {
        public LinearGradientBrush defaultBtnBG()
        {
            LinearGradientBrush brush = new LinearGradientBrush();

            brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(243, 243, 243), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(235, 235, 235), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(221, 221, 221), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(174, 174, 174), 1.0));

            return brush;
        }

        public LinearGradientBrush lgbGreen()
        {
            LinearGradientBrush brush = new LinearGradientBrush();

            brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(1, 180, 66), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(8, 247, 95), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(33, 255, 114), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(3, 102, 39), 1.0));

            return brush;
        }

        public LinearGradientBrush lgbDarkGrey()
        {
            LinearGradientBrush brush = new LinearGradientBrush();

            brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(151, 151, 151), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(235, 235, 235), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(221, 221, 221), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(112, 104, 104), 1.0));

            return brush;
        }


    }
}
