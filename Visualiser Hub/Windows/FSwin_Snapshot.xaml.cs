using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Visualiser_Hub.Utility_Classes;
using ColourPallette;

namespace Visualiser_Hub
{
    /// <summary>
    /// Interaction logic for FSwin_Snapshot.xaml
    /// </summary>
    public partial class FSwin_Snapshot : Window
    {
        public FSwin_Snapshot()
        {
            InitializeComponent();
        }

        #region Variables

        public static bool blDraw = false;
        public static bool blErase = false;
        public static bool blSelect = false;

        public static DateTime lastSave;
        public static DateTime lastStroke;

        public static int intPointSize;

        CustomBrushes custBrush = new CustomBrushes();

        #endregion

        private void winFSSnap_Loaded(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.Cursor = Cursors.Pen;
            inkCanvas.pointSize = 2;
            inkCanvas.imageStretch = Stretch.Uniform;

            colChooser.RecSize = 15;

            cbThickness.Items.Add("2");
            cbThickness.Items.Add("4");
            cbThickness.Items.Add("6");
            cbThickness.Items.Add("8");
            cbThickness.Items.Add("10");
            cbThickness.SelectedIndex = 0;

            intPointSize = int.Parse(cbThickness.SelectedItem.ToString());
        }

        private void winFSSnap_Unloaded(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.strokeCount > 0)
            {
                if (lastSave < lastStroke)
                {
                    MessageBoxResult result = MessageBox.Show("You've made changes to the snapshot, would you like to save?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveFileDialog dlgSave;
                        Nullable<bool> dlgResult;

                        dlgSave = new SaveFileDialog();
                        dlgSave.FileName = "Image";
                        dlgSave.DefaultExt = ".jpg";
                        dlgSave.Filter = "JPEG Image (.jpeg)|*.jpeg|Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|Png Image (.png)|*.png|Tiff Image (.tiff)|*.tiff|Wmf Image (.wmf)|*.wmf";
                        dlgResult = dlgSave.ShowDialog();

                        if (dlgResult == true)
                        {
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(inkCanvas.savedBitmap));

                            using (var stream = dlgSave.OpenFile())
                            {
                                encoder.Save(stream);
                            }
                        }
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlgSave;
            Nullable<bool> dlgResult;

            dlgSave = new SaveFileDialog();
            dlgSave.FileName = "Image";
            dlgSave.DefaultExt = ".jpg";
            dlgSave.Filter = "JPEG Image (.jpeg)|*.jpeg|Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|Png Image (.png)|*.png|Tiff Image (.tiff)|*.tiff|Wmf Image (.wmf)|*.wmf";
            dlgResult = dlgSave.ShowDialog();

            if (dlgResult == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(inkCanvas.savedBitmap));

                using (var stream = dlgSave.OpenFile())
                {
                    encoder.Save(stream);
                }

                lastSave = DateTime.Now;
            }
        }

        #region Draw controls

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            if (blDraw == false)
            {
                blDraw = true;

                if (blErase == true)
                {
                    blErase = false;
                    btnUndo.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                if (blSelect == true)
                {
                    blSelect = false;
                    btnSelect.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                btnDraw.BorderBrush = new SolidColorBrush(Colors.Black);
                btnDraw.BorderThickness = new Thickness(1, 1, 1, 1);
                btnDraw.BorderBrush = new SolidColorBrush(Colors.LightGreen);

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                inkCanvas.pointSize = intPointSize;

            }
            else if (blDraw == true)
            {
                blDraw = false;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                btnDraw.BorderBrush = new SolidColorBrush(Colors.Transparent);
                btnDraw.BorderThickness = new Thickness(1, 1, 1, 1);
            }
        }

        private void cbThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            intPointSize = int.Parse(cbThickness.SelectedItem.ToString());
            inkCanvas.pointSize = intPointSize;
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (blErase == false)
            {
                blErase = true;

                if (blDraw == true)
                {
                    blDraw = false;
                    btnDraw.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                if (blSelect == true)
                {
                    blSelect = false;
                    btnSelect.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                btnUndo.BorderBrush = new SolidColorBrush(Colors.LightGreen);
                btnUndo.BorderThickness = new Thickness(1, 1, 1, 1);

                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                inkCanvas.pointSize = intPointSize;

            }
            else if (blErase == true)
            {
                blErase = false;
                
                btnUndo.BorderBrush = new SolidColorBrush(Colors.Black);
                btnUndo.BorderThickness = new Thickness(1, 1, 1, 1);
                btnUndo.BorderBrush = new SolidColorBrush(Colors.Transparent);

                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (blSelect == false)
            {
                blSelect = true;

                if (blDraw == true)
                {
                    blDraw = false;
                    btnDraw.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                if (blErase == true)
                {
                    blErase = false;
                    btnUndo.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }

                inkCanvas.EditingMode = InkCanvasEditingMode.Select;

                btnSelect.BorderBrush = new SolidColorBrush(Colors.Black);
                btnSelect.BorderThickness = new Thickness(1, 1, 1, 1);
                btnSelect.BorderBrush = new SolidColorBrush(Colors.LightGreen);
            }
            else if (blSelect == true)
            {
                blSelect = false;

                inkCanvas.EditingMode = InkCanvasEditingMode.None;

                btnSelect.BorderBrush = new SolidColorBrush(Colors.Black);
                btnSelect.BorderThickness = new Thickness(1, 1, 1, 1);

                btnSelect.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            
        }

        private void inkCanvas_newStrokeEvent(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            lastStroke = DateTime.Now;
        }

        private void btnColPicker_Click(object sender, RoutedEventArgs e)
        {
            if (!colChooser.IsVisible)
            {
                colChooser.Visibility = Visibility.Visible;
            }
            else if (colChooser.IsVisible)
            {
                colChooser.Visibility = Visibility.Collapsed;
            }
        }

        private void colChooser_ItemHasBeenSelected(object sender, colorPallette.SelectedItemEventArgs e)
        {
            recColDisp.Fill = new SolidColorBrush(e.SelectedChoice);
            inkCanvas.StrokeColor = e.SelectedChoice;
            recColDisp.ToolTip = e.SelectedChoice.ToString();
        }

        private void colChooser_LostFocus(object sender, RoutedEventArgs e)
        {
            colChooser.Visibility = Visibility.Collapsed;
        }

        #endregion

        private void viewBoxMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            string imageWidth = viewBoxMain.ActualWidth.ToString();
            string imageHeight = viewBoxMain.ActualHeight.ToString();

            lblSize.Content = ("Current Image Size: " + imageWidth + "x" + imageHeight);
        }
    }
}
