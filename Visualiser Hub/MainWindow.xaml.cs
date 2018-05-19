using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Interop;

namespace Visualiser_Hub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Global Variables

        CapDevice vidDevice; //initialises the capture device but without an active Moniker

        DispatcherTimer timerFPS = new DispatcherTimer();

        public static ObservableCollection<string> statusList = new ObservableCollection<string>();

        public static string moniker; //stores current selected moniker

        public static FilterInfo[] devList; //stores current device list

        public static BitmapImage CapturedImage = null; //stores snapshot image

        public class ComboMonikers //defines an item with two properties
        {
            public string _Name { get; set; }
            public string _Moniker { get; set; }

            public ComboMonikers(string _name, string _moniker)
            {
                _Name = _name;
                _Moniker = _moniker;
            }
        } 

        public static Label lblFPS = new Label();

        public static string currentCapDev;

        public static Button imageControl;

        public static List<BitmapImage> imageHistory = new List<BitmapImage>();

        Style buttonStyle = new Style(typeof(Button));

        #endregion

        #region onForm Load Commands

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            if (Dispatcher != null)
            {
                 Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (SendOrPostCallback)delegate
                 {
                     vidDevice = new CapDevice(null);

                     if (cbDevices.Items.Count > 0)
                     {
                         cbDevices.ItemsSource = null;
                     }

                     bool isDevices = vidDevice.Monikercheck(); //performs initial device check
                     
                     List<ComboMonikers> listDev = new List<ComboMonikers>(); //initiates the list of items
                     
                     if (!isDevices)
                     {
                         MessageBox.Show("No video capture device detected, please connect one and press refresh.");
                         statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - No video devices found!.");
                     }
                     else if (isDevices)
                     {
                         devList = vidDevice.DeviceList();

                         foreach (FilterInfo device in devList)
                         {
                             listDev.Add(new ComboMonikers(device.Name, device.MonikerString));
                         }
                     }

                     cbDevices.DisplayMemberPath = "_Name";
                     cbDevices.SelectedValuePath = "_Moniker";
                     cbDevices.ItemsSource = listDev;
                     cbDevices.SelectedIndex = 0;

                     statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - Populated available video devices.");

                     vidDevice.Stop();
                 }, null);

                lbStatus.ItemsSource = statusList;
                statusList.CollectionChanged += statusList_Sourceupdated;
            }
        }

        #endregion

        #region Loads selected Device

        private void btnCamSet_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source != null)
            {
                MessageBox.Show("Please disconnect the currently active video source first.");
            }
            else if (imgStream.Source == null)
            {
                try
                {
                    currentCapDev = cbDevices.Text;
                    moniker = cbDevices.SelectedValue.ToString();
                    vidDevice = new CapDevice(moniker); //re-initialises the capture device with chosen moniker.
                    vidDevice.OnNewBitmapReady += new EventHandler(_device_OnNewBitmapReady);
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Connected to " + currentCapDev + ".");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        void _device_OnNewBitmapReady(object sender, EventArgs e)
        {
            if (imgStream.Source != null) imgStream.Source = null;
            imgStream.Source = vidDevice.BitmapSource;
        }

        #endregion

        #region Stops Selected Device

        private void btnCamStop_Click(object sender, RoutedEventArgs e)
        {
            vidDevice.Stop();
            imgStream.Source = null;
            statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - Disconnected from " + currentCapDev + ".");

            if (grdStream.Children.Contains(lblFPS))
            {
                timerFPS.Tick -= get_FrameRate;
                timerFPS.Stop();
                statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - 'Frames per Second' hidden.");

                grdStream.Children.Remove(lblFPS);
            }
        }

        #endregion

        #region Refresh Device List

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Root_Loaded(null, null);
        }

        #endregion

        #region Takes Snapshot of Stream

        public void btnSnap_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source == null)
            {
                MessageBox.Show("Unable to take snapshot when no video stream is active.");
            }
            else if (imgStream.Source != null)
            {
                Utility_Classes.SourcetoImage srcImage = new Utility_Classes.SourcetoImage();
                CapturedImage = srcImage.Convert((BitmapSource)imgStream.Source);

                imgCapture.Source = CapturedImage;
                statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - Snapshot Taken.");

                imageHistory.Add((BitmapImage)imgCapture.Source);

                updateImageHistory();
            }
        }

        #endregion

        #region Save Image Method

        private void btnSaveSnap_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlgSave;
            Nullable<bool> dlgResult;

            if (imgStream.Source != null && imgCapture.Source == null)
            {
                btnSnap_Click(null, null);
            }

            dlgSave = new SaveFileDialog();
            dlgSave.FileName = "Image";
            dlgSave.DefaultExt = ".jpg";
            dlgSave.Filter = "JPEG Image (.jpeg)|*.jpeg|Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|Png Image (.png)|*.png|Tiff Image (.tiff)|*.tiff|Wmf Image (.wmf)|*.wmf";

            if (imgCapture.Source != null)
            {
                dlgResult = dlgSave.ShowDialog();

                if (dlgResult == true)
                {
                    var encoder = new JpegBitmapEncoder(); // Or PngBitmapEncoder, or whichever encoder you want
                    encoder.QualityLevel = 100;
                    encoder.Frames.Add(BitmapFrame.Create(CapturedImage));
                    using (var stream = dlgSave.OpenFile())
                    {
                        encoder.Save(stream);
                    }
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - " + dlgSave.FileName + " saved.");
                }
            }
        }

        #endregion

        #region Open image method

        private void btnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlgOpen;
            Nullable<bool> dlgResult;

            dlgOpen = new OpenFileDialog();
            dlgOpen.FileName = "Image";
            dlgOpen.DefaultExt = ".jpg";
            dlgOpen.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

            dlgResult = dlgOpen.ShowDialog();

            if (dlgResult == true)
            {
                BitmapImage bmpOpen = new BitmapImage(new Uri(dlgOpen.FileName));
                imgCapture.Source = bmpOpen;
                CapturedImage = bmpOpen;
                statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - " + dlgOpen.FileName + " opened.");
            }
        }

        #endregion

        #region Clears image from the Capture Box

        private void btnClearSnap_Click(object sender, RoutedEventArgs e)
        {
            CapturedImage = null;
            imgCapture.Source = null;
            statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - Snapshot cleared.");
        }

        #endregion

        #region Methods for displaying FPS of Stream

        private void btnShowFPS_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source != null)
            {
                SolidColorBrush bg = new SolidColorBrush(Colors.Black);
                bg.Opacity = 0.5;
                lblFPS.VerticalAlignment = VerticalAlignment.Top;
                lblFPS.HorizontalAlignment = HorizontalAlignment.Left;
                lblFPS.Content = "FPS: ";
                lblFPS.Width = 60;
                lblFPS.Height = 30;
                lblFPS.FontSize = 14;
                lblFPS.FontWeight = FontWeights.Bold;
                lblFPS.Margin = new Thickness(5, 5, 0, 0);
                lblFPS.Foreground = new SolidColorBrush(Colors.Red);
                lblFPS.Background = bg;

                
                timerFPS.Interval = TimeSpan.FromSeconds(1);

                if (!grdStream.Children.Contains(lblFPS))
                {
                    if (vidDevice != null)
                    {
                        timerFPS.Tick += get_FrameRate;
                        timerFPS.Start();
                        statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - 'Frames per Second' visible.");

                        grdStream.Children.Add(lblFPS);
                    }
                }
                else if (grdStream.Children.Contains(lblFPS))
                {
                    timerFPS.Tick -= get_FrameRate;
                    timerFPS.Stop();
                    statusList.Insert(0,DateTime.Now.ToString("hh:mm:ss") + " - 'Frames per Second' hidden.");

                    grdStream.Children.Remove(lblFPS);
                }
            }
        }

        private void get_FrameRate(object sender, EventArgs e)
        {
            lblFPS.Content = ("FPS: " + vidDevice.Framerate);
        }

        #endregion

        #region Status box source updated event

        private void statusList_Sourceupdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            lbStatus.SelectedIndex = 0;

            if (lbStatus.Items.Count > 12)
            {
                lbStatus.ScrollIntoView(lbStatus.Items[lbStatus.SelectedIndex]);
            }
        }

        #endregion

        #region Fullscreen snapshot

        private void btnEnlargeCap_Click(object sender, RoutedEventArgs e)
        {
            if (imgCapture.Source != null)
            {
                FSwin_Snapshot FSSnap = new FSwin_Snapshot();

                FSSnap.inkCanvas.MainImage = CapturedImage;

                statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Fullscreen snapshot opened.");

                Nullable<bool> dlgResult;

                dlgResult = FSSnap.ShowDialog(); ;

                if (dlgResult == false)
                {
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Fullscreen snapshot closed.");
                }
            }
            else if (imgCapture.Source == null)
            {
                //
            }
        }

        #endregion

        #region Fullscreen Stream

        private void btnEnlargeStream_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source != null)
            {
                Windows.FSwin_Stream FSStream = new Windows.FSwin_Stream();
                FSStream.imgStream.Source = vidDevice.BitmapSource;
                imgStream.Source = null;
                
                statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Fullscreen stream opened.");

                Nullable<bool> dlgResult;

                dlgResult = FSStream.ShowDialog();

                if (dlgResult == false)
                {
                    imgStream.Source = vidDevice.BitmapSource;
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Fullscreen stream closed.");
                }
            }
            else if (imgStream.Source == null)
            {
                MessageBox.Show("Can only fullscreen an active stream.");
            }
        }

        #endregion

        #region Splitscreen Methods

        private void btnHSS_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source != null && CapturedImage != null)
            {
                Windows.winHSplitscreen splitscreen = new Windows.winHSplitscreen();
                splitscreen.imgStream.Source = vidDevice.BitmapSource;
                imgStream.Source = null;

                statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Horizontal splitscreen opened.");

                Nullable<bool> dlgResult;

                if (CapturedImage != null)
                {
                    splitscreen.imgSnap.Source = CapturedImage;
                }

                dlgResult = splitscreen.ShowDialog();

                if (dlgResult == false)
                {
                    imgStream.Source = vidDevice.BitmapSource;
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Horizontal splitscreen closed.");
                }
            }
            if (imgStream.Source == null || CapturedImage == null)
            {
                MessageBox.Show("Splitscreen mode requires an active stream and a captured image.");
            }
        }

        private void btnVSS_Click(object sender, RoutedEventArgs e)
        {
            if (imgStream.Source != null && CapturedImage != null)
            {
                winVSplitscreen splitscreen = new winVSplitscreen();
                splitscreen.imgStream.Source = vidDevice.BitmapSource;
                imgStream.Source = null;

                statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Vertical splitscreen opened.");

                Nullable<bool> dlgResult;

                if (CapturedImage != null)
                {
                    splitscreen.imgSnap.Source = CapturedImage;
                }

                dlgResult = splitscreen.ShowDialog();

                if (dlgResult == false)
                {
                    imgStream.Source = vidDevice.BitmapSource;
                    statusList.Insert(0, DateTime.Now.ToString("hh:mm:ss") + " - Vertical splitscreen closed.");
                }
            }
            else if (imgStream.Source == null || CapturedImage == null)
            {
                MessageBox.Show("Splitscreen mode requires an active stream and a captured image.");
            }
        }

        #endregion

        #region Snapshot history methods

        private void updateImageHistory()
        {
            if (imageHistory.Count > 5)
            {
                MessageBoxResult dlgResult;
                dlgResult = MessageBox.Show("Your snap history can only store 5 images at a time, would you like to remove the oldest one?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (dlgResult == MessageBoxResult.Yes)
                {
                    imageHistory.RemoveAt(0);
                    updateImageHistory();
                }
            }
            else if (imageHistory.Count <= 5)
            {
                ContainerWrapPanel.Children.Clear();

                int i = 0;

                buttonStyle = this.FindResource("SimpleButton") as Style;

                foreach (var image in imageHistory)
                {
                    imageControl = new Button();
                    imageControl.Style = buttonStyle;
                    imageControl.Width = 70;
                    imageControl.Height = 70;
                    imageControl.Name = ("Image" + i.ToString());
                    imageControl.Tag = i;
                    imageControl.Cursor = Cursors.Hand;
                    imageControl.Margin = new Thickness(2, 2, 2, 2);
                    imageControl.Click += imageClick;
                    imageControl.Background = new ImageBrush(image);
                    imageControl.BorderBrush = new SolidColorBrush(Colors.DimGray);
                    imageControl.BorderThickness = new Thickness(2, 2, 2, 2);
                    imageControl.LostFocus += imageLostFocus;
                    ContainerWrapPanel.Children.Add(imageControl);

                    i++;
                }
            }
        }

        private void imageClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.BorderBrush = new SolidColorBrush(Colors.LightGreen);
            int index = int.Parse(btn.Tag.ToString());
            imgCapture.Source = imageHistory[index];

            Utility_Classes.SourcetoImage srcImage = new Utility_Classes.SourcetoImage();
            CapturedImage = srcImage.Convert_NoRotate((BitmapSource)imgCapture.Source);
        }

        private void imageLostFocus(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.BorderBrush = new SolidColorBrush(Colors.DimGray);
        }

        private void btnClear_MouseEnter(object sender, MouseEventArgs e)
        {
            btnClear.Opacity = 1;
        }

        private void btnClear_MouseLeave(object sender, MouseEventArgs e)
        {
            btnClear.Opacity = 0.5;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (imageHistory.Count > 0)
            {
                MessageBoxResult dlgResult;
                dlgResult = MessageBox.Show("This will clear your snapshot history, do you want to proceed?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (dlgResult == MessageBoxResult.Yes)
                {
                    imageHistory.Clear();
                    ContainerWrapPanel.Children.Clear();
                }
            }
        }

        #endregion

    }
}
