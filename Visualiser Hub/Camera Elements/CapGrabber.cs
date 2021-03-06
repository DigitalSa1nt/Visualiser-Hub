﻿using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Visualiser_Hub
{
    internal class CapGrabber:ISampleGrabberCB,INotifyPropertyChanged
    {
        public CapGrabber()
        {
            
        }

        #region variables

        public IntPtr Map { get; set; }
        
        public int Width
        {
            get { return m_Width; }
            set
            {
                m_Width = value;
                OnPropertyChanged("Width");
            }
        }

        public int Height
        {
            get { return m_Height; }
            set
            {
                m_Height = value;
                OnPropertyChanged("Height");
            }
        }

        int m_Height = default(int);

        int m_Width = default(int);

        #endregion

        #region ISampleGrabberCB Members

        public int SampleCB(double sampleTime, IntPtr sample)
        {
            return 0;
        }

        public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (Map != IntPtr.Zero)
            {
                CopyMemory(Map, buffer, bufferLen);
                OnNewFrameArrived();
            }
            return 0;
        }

        #endregion

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        public event EventHandler NewFrameArrived;

        void OnNewFrameArrived()
        {
            if (NewFrameArrived != null)
                NewFrameArrived(this, null);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        public void Stop()
        {
        }


    }
}
