using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Visualiser_Hub.Windows
{
    /// <summary>
    /// Interaction logic for FSwin_Stream.xaml
    /// </summary>
    public partial class FSwin_Stream : Window
    {
        public FSwin_Stream()
        {
            InitializeComponent();
        }

        private void winStreamFS_Unloaded(object sender, RoutedEventArgs e)
        {
            imgStream.Source = null;
        }
    }
}
