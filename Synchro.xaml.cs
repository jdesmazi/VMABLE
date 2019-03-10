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
using Windows.Devices.Enumeration;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace VMALE
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class Synchro : Page
    {
        private static DeviceWatcher deviceWatcher;
        private static List<DeviceInformation> detectedDevices = new List<DeviceInformation>();

        private static BluetoothLEDevice ble;


        public Synchro()
        {
            this.InitializeComponent();
            loadDevices();
        }


        private void ReturnButton(object sender, RoutedEventArgs e)
        {
            MainPage p = new MainPage();
            this.Content = p;
        }


        private void searchDevice(object sender, RoutedEventArgs e)
        {
            startSearch.Visibility = Visibility.Collapsed;
            stopSearch.Visibility = Visibility.Visible;
            startBleDeviceWatcher();
        }


        private void stopSearchDevice(object Sender, RoutedEventArgs e)
        {
            startSearch.Visibility = Visibility.Visible;
            stopSearch.Visibility = Visibility.Collapsed;
            stopBleDeviceWatcher();
        }


        private void loadDevices()
        {
            foreach(var x in BleDevices.Instance.getRegister())
            {
                deviceList.Items.Add(x);
            }
        }



        #region discover

        #region Device Watcher
        public void startBleDeviceWatcher()
        {
            string[] requestedProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // Protocole to call for Bluetooth LE
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            //Add event Handler
            deviceWatcher.Added += onAdded;
            deviceWatcher.Updated += onUpdated;
            deviceWatcher.Removed += onRemoved;
            deviceWatcher.EnumerationCompleted += onEnumerationCompleted;
            deviceWatcher.Stopped += onStopped;

            deviceWatcher.Start();
        }


        public void stopBleDeviceWatcher()
        {
            deviceWatcher.Added -= onAdded;
            deviceWatcher.Updated -= onUpdated;
            deviceWatcher.Removed -= onRemoved;
            deviceWatcher.EnumerationCompleted -= onEnumerationCompleted;
            deviceWatcher.Stopped -= onStopped;

            deviceWatcher.Stop();
        }


        private void onAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            Debug.WriteLine("Appareil detecté : " + deviceInfo.Id + ", Nom : " 
                + deviceInfo.Name + ", connectale : " 
                + deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]);


            if (deviceInfo.Id != null && !deviceInfo.Name.Equals(""))
            {
                detectedDevices.Add(deviceInfo);
                if ((bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"])
                {
                    addFoundDevice(deviceInfo);
                }
            }
        }

        private void onUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("Appareil modifié : " + args.Id + ", Connectable : "
                + args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]);
            if (args.Id != null)
            {
                if((bool)args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"])
                {
                    foreach (DeviceInformation x in detectedDevices)
                    {
                        if (x.Id == args.Id)
                        {
                            x.Update(args);
                            addFoundDevice(x);
                        }
                    }
                }
            }

        }

        private void onRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }


        private async void onEnumerationCompleted(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("Enumeration completed");
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                startSearch.Visibility = Visibility.Visible;
                stopSearch.Visibility = Visibility.Collapsed;
            });
            stopBleDeviceWatcher();
        }

        private void onStopped(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("Unexpected Stop of watcher");
        }
        #endregion


        private async void addFoundDevice(DeviceInformation deviceInfo)
        {
            Device d = new Device(deviceInfo);
            if (!BleDevices.Instance.getRegister().Contains(d))
            {
                //Print one device found
                BleDevices.Instance.getRegister().Add(d);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    deviceList.Items.Add(d);
                });
            }
        }






        private async void connect(object sender, RoutedEventArgs e)
        {
            if(ble != null) ble.Dispose();
            var connectBtn = sender as Button;
            String id = (String)connectBtn.Tag;

            Button discoBtn = (Button)((StackPanel)connectBtn.Parent).FindName("Disconnect");
            connectBtn.Visibility = Visibility.Collapsed;
            discoBtn.Visibility = Visibility.Visible;

            try
            {
                ble = await BluetoothLEDevice.FromIdAsync(id);
                Debug.WriteLine("Appareil normalement connecté : "+id);
                if (ble == null)
                {
                    Debug.WriteLine("Fail to connect to adress "+id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            GattDeviceServicesResult result = await ble.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success)
            {
                var services = result.Services;
                Debug.WriteLine("Found " + services.Count + " services on "+id);
            }
            else Debug.WriteLine("Fail to query gatt services on "+id);
        }


        private void disconnect(object sender, RoutedEventArgs e)
        {
            if (ble != null) ble.Dispose();

            var discoBtn = sender as Button;

            Button connectBtn = (Button)((StackPanel)discoBtn.Parent).FindName("Connect");
            connectBtn.Visibility = Visibility.Visible;
            discoBtn.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
