using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace VMALE
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class TestVMA : Page
    {

        private static BluetoothLEDevice ble;
        
        //Uuid of the service and his characteristic used to write/read values
        private static Guid serviceUuid = new Guid("00000af0-0000-1000-8000-00805f9b34fb");
        private static Guid characteristicUuid = new Guid("00000af6-0000-1000-8000-00805f9b34fb");

        private Boolean stopWatcher = true;

        public TestVMA()
        {
            this.InitializeComponent();
            loadRunner();
        }

        private void loadRunner()
        {
            foreach(Device d in BleDevices.Instance.getRegister())
            {
                deviceList.Items.Add(d);
            }
        }


        private void ReturnButton(object sender, RoutedEventArgs e)
        {
            MainPage p = new MainPage();
            this.Content = p;
        }



        private async void startRunner(object sender, RoutedEventArgs e)
        {
            Button startBtn = sender as Button;
            String idRunner = (String)startBtn.Tag;

            Device d = BleDevices.Instance.findRegisteredById(idRunner);
            //d.laps = 0;

            //Initialize run when the start is near the computer
            DateTime t = DateTime.Now;
            String formatTime =t.ToLongTimeString() + "." + t.Millisecond;
            Debug.WriteLine("Date de départ du coureur : " + d.HumanName + " " + formatTime);
            String rt = await writeTime(d, formatTime);
            //d.lapsTimes.Add(t);

            BleDevices.Instance.getRunners().Add(d);
            if (stopWatcher)
            {
                stopWatcher = false;
                Task task1 = Task.Run(() => runWatcher() );
            }

            //wait 30s
            //startTimer();

            //while(laps < 3)
            //callback indiv ou fnction multiple ?

        }


        private void startAll(object sender, RoutedEventArgs e)
        {

        }


        private async void runWatcher()
        {
            int index = 0;
            while(!stopWatcher)
            {
                if (BleDevices.Instance.getRunners().Count == 0) break;

                if (BleDevices.Instance.getRunners().Count <= index) index = 0;

                Device d = BleDevices.Instance.getRunners()[index];

                DateTime t = DateTime.Now;
                String formatTime = t.ToLongTimeString() + "." + t.Millisecond;

                if (ble != null) ble.Dispose();
                ble = await BluetoothLEDevice.FromIdAsync(d.BleID);
                Debug.WriteLine("Appareil connecté : " + d.BleID);

                if (ble != null)
                {
                    addLaps(d, t);
                    //3 is the max laps of the run
                    if (d.lapsTimes.Count >= 3) BleDevices.Instance.getRunners().RemoveAt(index);
                }
                index++;
            }
            stopWatcher = true;
        }




        private async Task<String> writeTime(Device d, String t)
        {
            if (ble != null) ble.Dispose();
            String rt = null;
            try
            {
                ble = await BluetoothLEDevice.FromIdAsync(d.BleID);
                Debug.WriteLine("Appareil connecté : " + d.BleID);

                GattDeviceServicesResult result = await ble.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Uncached);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    GattDeviceService service = result.Services[0];
                    Debug.WriteLine("Found the service for " + d.BleID);

                    GattCharacteristicsResult cResult = await service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached);
                    if(cResult.Status == GattCommunicationStatus.Success)
                    {
                        GattCharacteristic characteristic = cResult.Characteristics[0];
                        Debug.WriteLine("Found the characteristic for " + d.BleID);

                        //Read Value 
                        GattReadResult readResult = await characteristic.ReadValueAsync();
                        if (readResult.Status == GattCommunicationStatus.Success)
                        {
                            var readBuffer = new byte[readResult.Value.Length];
                            DataReader.FromBuffer(readResult.Value).ReadBytes(readBuffer);
                            rt = Encoding.Default.GetString(readBuffer);
                            Debug.WriteLine(rt);
                        }


                        //Write Value
                        byte[] writeBytes = Encoding.Default.GetBytes(t);
                        var writeBuffer = new DataWriter();
                        writeBuffer.WriteBytes(writeBytes);
                        //var writeBuffer = writeBytes.AsBuffer();
                        GattWriteResult writeResult = await characteristic.WriteValueWithResultAsync(writeBuffer.DetachBuffer());
                        if(writeResult.Status != GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine("Failed to write in the device " + writeResult.Status.ToString());
                        }
                    }
                }
                else Debug.WriteLine("Fail to query gatt services on " + d.BleID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return rt;
        }


        private DateTime stringToDateTime(String s)
        {
            int hour = Convert.ToInt32(s.Split(":")[0]);
            int minute = Convert.ToInt32(s.Split(":")[1]);
            int second = Convert.ToInt32(s.Split(":")[2].Split(".")[0]);
            int millisecond = Convert.ToInt32(s.Split(":")[2].Split(".")[1]);

            DateTime dt = new DateTime(1, 1, 1, hour, minute, second, millisecond);
            return dt;
        }



        private void addLaps(Device d, DateTime t)
        {

            
            if (d.lapsTimes.Count != 0)
            {
                //step one do now-lastTime
                DateTime lastTime = d.lapsTimes[d.lapsTimes.Count - 1];
                float diff = subInSec(t, lastTime);

                //10 is a global value that cover the minimum distance to cover to be considered as a new laps
                if (diff > 10) d.lapsTimes.Add(t);
            }
            else d.lapsTimes.Add(t);
            Debug.WriteLine("Liste des different laps : ");
            foreach (var x in d.lapsTimes) Debug.WriteLine("laps : " + x.ToLongTimeString() + "." + x.Millisecond);
        }

        private float subInSec(DateTime after, DateTime before)
        {
            float diff = (after.Hour - before.Hour)*3600 + (after.Minute - before.Minute)*60 + after.Second - before.Second;
            diff = diff + (after.Millisecond - before.Millisecond)/1000;
            return diff;
        }






    }
}
