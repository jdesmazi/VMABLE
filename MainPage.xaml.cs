using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Bluetooth;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VMALE
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            //BluetoothLEController b = new BluetoothLEController();
            //b.startBleDeviceWatcher();
        }

        private void DemarrerTest(object sender, RoutedEventArgs e)
        {
            TestVMA p = new TestVMA();
            this.Content = p;
        }

        private void SynchroniserAppareil(object sender, RoutedEventArgs e)
        {
            Synchro p = new Synchro();
            this.Content = p;
        }
    }
}
