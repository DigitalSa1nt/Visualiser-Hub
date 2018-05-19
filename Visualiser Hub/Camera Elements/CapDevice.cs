using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Visualiser_Hub
{
    internal class CapDevice:DependencyObject,IDisposable
    {
               
        #region Variables
        
        private double _frames; //local variable used for framerate calculations

        CapGrabber capGrabber; //initialises the CapGrabber class.

        private static string deviceMoniker; //The static variable for the device that is being accessed

        IntPtr map; //?

        IntPtr section; //?

        private CancellationTokenSource _cancelToken; //Used in order to end the main task worker.

        public InteropBitmap BitmapSource //The returned bitmap source.
        {
            get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
            private set { SetValue(BitmapSourcePropertyKey, value); }
        }

        private static readonly DependencyPropertyKey BitmapSourcePropertyKey = DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CapDevice), new UIPropertyMetadata(default(InteropBitmap)));

        public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;

        public float Framerate //integer that is passed back to calling method as the Framerate.
        {
            get { return (float)GetValue(FramerateProperty); }
            set { SetValue(FramerateProperty, value); }
        }

        public static readonly DependencyProperty FramerateProperty = DependencyProperty.Register("Framerate", typeof(float), typeof(CapDevice), new UIPropertyMetadata(default(float)));

        private readonly Stopwatch timer = Stopwatch.StartNew(); //timer used in the update of the framerate integer.

        #endregion

        #region Device Moniker - Check, Return, Base.

        public bool Monikercheck()
        {
            if (DeviceMonikers.Count() <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public FilterInfo[] DeviceList() //Checker method that passes through monkier list if one is available
        {
                return DeviceMonikers.ToArray();
        }

        public static FilterInfo[] DeviceMonikers //Does all the hard work checking the system for Guid Specific video devices
        {
            get
            {
                List<FilterInfo> filters = new List<FilterInfo>();
                IMoniker[] ms = new IMoniker[1];
                ICreateDevEnum enumD = Activator.CreateInstance(Type.GetTypeFromCLSID(SystemDeviceEnum)) as ICreateDevEnum;
                IEnumMoniker moniker;
                Guid g = VideoInputDevice;

                if (enumD.CreateClassEnumerator(ref g, out moniker, 0) == 0)
                {
                    while (true)
                    {
                        int r = moniker.Next(1, ms, IntPtr.Zero);
                        if (r != 0 || ms[0] == null)
                            break;
                        filters.Add(new FilterInfo(ms[0]));
                        Marshal.ReleaseComObject(ms[0]);
                        ms[0] = null;
                    }
                }

                return filters.ToArray();
            }
        }

        #endregion

        #region CapDevice start methods

        public CapDevice(string moniker) //used if you want to pass a moniker manually to start method
        {
            deviceMoniker = moniker;

            Start();
        }

        public void Start()
        {

            //creates new instance of the cancellationtoken
            _cancelToken = new CancellationTokenSource();
            
            Task.Factory.StartNew(() =>
            {
                // Create new grabber
                capGrabber = new CapGrabber();
                capGrabber.PropertyChanged += capGrabber_PropertyChanged;
                capGrabber.NewFrameArrived += capGrabber_NewFrameArrived;

                var graph = Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph)) as IGraphBuilder;

                var sourceObject = FilterInfo.CreateFilter(deviceMoniker);

                var grabber = Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber)) as ISampleGrabber;
                var grabberObject = grabber as IBaseFilter;

                if (graph == null) return;

                graph.AddFilter(sourceObject, "source");
                graph.AddFilter(grabberObject, "grabber");
                using (var mediaType = new AMMediaType())
                {
                    mediaType.MajorType = MediaTypes.Video;
                    mediaType.SubType = MediaSubTypes.RGB32;
                    if (grabber != null)
                    {
                        grabber.SetMediaType(mediaType);

                        if (graph.Connect(sourceObject.GetPin(PinDirection.Output, 0), grabberObject.GetPin(PinDirection.Input, 0)) >= 0)
                        {
                            if (grabber.GetConnectedMediaType(mediaType) == 0)
                            {
                                VideoInfoHeader header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                                capGrabber.Width = header.BmiHeader.Width;
                                capGrabber.Height = header.BmiHeader.Height;
                            }
                        }
                        graph.Render(grabberObject.GetPin(PinDirection.Output, 0));
                        grabber.SetBufferSamples(false);
                        grabber.SetOneShot(false);
                        grabber.SetCallback(capGrabber, 1);
                    }

                    // Get the video window
                    var wnd = (IVideoWindow)graph;
                    wnd.put_AutoShow(false);

                    // Create the control and run
                    var control = (IMediaControl)graph;
                    control.Run();

                    // Wait for the stop signal
                    var stopSignal = new ManualResetEventSlim(false);
                    using (_cancelToken.Token.Register(stopSignal.Set))
                        stopSignal.Wait();

                    // Stop when ready
                    control.StopWhenReady();
                    capGrabber = null;
                }
            }, _cancelToken.Token);

        }

        #endregion

        #region methods dealing with new frames

        void capGrabber_NewFrameArrived(object sender, EventArgs e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(DispatcherPriority.Render, (SendOrPostCallback)delegate
                {
                    if (BitmapSource != null)
                    {
                        BitmapSource.Invalidate();
                        UpdateFramerate();
                    }
                }, null);
            }
        }

        void capGrabber_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.DataBind, (SendOrPostCallback)delegate
            {
                try
                {
                    if (capGrabber.Width != default(int) && capGrabber.Height != default(int))
                    {
                        //retrieves the pixel count
                        uint pcount = (uint)(capGrabber.Width * capGrabber.Height * PixelFormats.Bgr32.BitsPerPixel / 8);

                        //creates the intial file mapping
                        section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, pcount, null);
                        map = MapViewOfFile(section, 0xF001F, 0, 0, pcount);

                        //Gets the bitmap
                        BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(section, capGrabber.Width, capGrabber.Height, PixelFormats.Bgr32,
                            capGrabber.Width * PixelFormats.Bgr32.BitsPerPixel / 8, 0) as InteropBitmap;
                        capGrabber.Map = map;
                        if (OnNewBitmapReady != null)
                            OnNewBitmapReady(this, null);
                    }
                }
                catch (Exception ex)
                {
                    // Trace
                    Trace.TraceError(ex.Message);
                }
            }, null);
        }

        #endregion

        #region Grabs framerate

        void UpdateFramerate()
        {
            // Increase the frames
            _frames++;

            // Check the timer
            if (timer.ElapsedMilliseconds < 1000) return;
            // Set the framerate
            Framerate = (float)Math.Round(_frames * 1000 / timer.ElapsedMilliseconds);

            // Reset the timer again so we can count the framerate again
            timer.Reset();
            timer.Start();
            _frames = 0;
        }

        #endregion

        #region Stop and release memory

        public void Stop()
        {
            if (_cancelToken != null)
            {
                _cancelToken.Cancel();
                capGrabber = null;
            }
        }
        
        #endregion

        #region static read only Guids defining the underlying Pins

        static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

        static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

        public static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        [ComVisible(false)]
        internal class MediaTypes
        {
            public static readonly Guid Video = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Interleaved = new Guid(0x73766169, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Audio = new Guid(0x73647561, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Text = new Guid(0x73747874, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Stream = new Guid(0xE436EB83, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);
        }

        [ComVisible(false)]
        internal class MediaSubTypes
        {
            public static readonly Guid YUYV = new Guid(0x56595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid IYUV = new Guid(0x56555949, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid DVSD = new Guid(0x44535644, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid RGB1 = new Guid(0xE436EB78, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB4 = new Guid(0xE436EB79, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB8 = new Guid(0xE436EB7A, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB565 = new Guid(0xE436EB7B, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB555 = new Guid(0xE436EB7C, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB24 = new Guid(0xE436Eb7D, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB32 = new Guid(0xE436EB7E, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Avi = new Guid(0xE436EB88, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Asf = new Guid(0x3DB80F90, 0x9412, 0x11D1, 0xAD, 0xED, 0x00, 0x00, 0xF8, 0x75, 0x4B, 0x99);
        }

        #endregion

        #region DLL imports to create file mapping properties

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        #endregion

        public event EventHandler OnNewBitmapReady;

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
