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
        private static Guid serviceUuid             = new Guid("00000af0-0000-1000-8000-00805f9b34fb");
        private static Guid characteristicUuid      = new Guid("00000af6-0000-1000-8000-00805f9b34fb");
        private static Guid readServiceUuid         = new Guid("00000af0-0000-1000-8000-00805f9b34fb");
        private static Guid readCharacteristicUuid  = new Guid("00000af6-0000-1000-8000-00805f9b34fb");
        private Boolean stopWatcher = true;

        private List<String> runAllId = new List<String>();

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
            d.lapsTimes = new List<DateTime>();

            //Add View to device
            Grid g = startBtn.Parent as Grid;
            d.stateText = g.FindName("state") as TextBlock;
            d.lapsText = g.FindName("laps") as TextBlock;


            //Initialize run when the start is near the computer
            DateTime t = DateTime.Now;
            String formatTime =t.ToLongTimeString() + "." + t.Millisecond;
            Debug.WriteLine("Date de départ du coureur : " + d.HumanName + " " + formatTime);
            String rt = await writeTime(d, formatTime);

            if (rt != null) d.firstDetection = true;

            BleDevices.Instance.getRunners().Add(d);
            showTime(d);
            if (stopWatcher)
            {
                stopWatcher = false;
                Task task1 = Task.Run(() => runWatcher() );
            }
        }


        private async void startAll(object sender, RoutedEventArgs e)
        {
            foreach(var x in runAllId)
            {
                Device d = BleDevices.Instance.findRegisteredById(x);
                d.lapsTimes = new List<DateTime>();

                //Initialize run when the start is near the computer
                DateTime t = DateTime.Now;
                String formatTime = t.ToLongTimeString() + "." + t.Millisecond;
                Debug.WriteLine("Date de départ du coureur : " + d.HumanName + " " + formatTime);
                String rt = await writeTime(d, formatTime);

                if (rt != null) d.firstDetection = true;

                BleDevices.Instance.getRunners().Add(d);
                showTime(d);
                if (stopWatcher)
                {
                    stopWatcher = false;
                    Task task1 = Task.Run(() => runWatcher());
                }
            }
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

                if (ble != null)
                {
                    if (!d.firstDetection) await readTimeValue(d);
                    ble.Dispose();
                }
                ble = await BluetoothLEDevice.FromIdAsync(d.BleID);
                Debug.WriteLine("Appareil connecté : " + d.BleID);

                if (ble != null)
                {
                    addLaps(d, t);
                    if (d.lapsTimes.Count > Device.maxLaps)
                    {
                        BleDevices.Instance.getRunners().RemoveAt(index);
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            endShowTime(d);
                        }); 
                    }
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


        private async Task<String> readTimeValue(Device d)
        {
            if (ble != null) ble.Dispose();
            String rt = null;
            try
            {
                ble = await BluetoothLEDevice.FromIdAsync(d.BleID);
                Debug.WriteLine("Appareil connecté : " + d.BleID);

                GattDeviceServicesResult result = await ble.GetGattServicesForUuidAsync(readServiceUuid, BluetoothCacheMode.Uncached);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    GattDeviceService service = result.Services[0];
                    Debug.WriteLine("Found the service for " + d.BleID);

                    GattCharacteristicsResult cResult = await service.GetCharacteristicsForUuidAsync(readCharacteristicUuid, BluetoothCacheMode.Uncached);
                    if (cResult.Status == GattCommunicationStatus.Success)
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



        private async void addLaps(Device d, DateTime t)
        {
            if (d.lapsTimes.Count != 0)
            {
                //step one do now-lastTime
                DateTime lastTime = d.lapsTimes[d.lapsTimes.Count - 1];
                float diff = subInSec(t, lastTime);

                //10 is a global value that cover the minimum distance to cover to be considered as a new laps
                if (diff > 10)
                {
                    d.lapsTimes.Add(t);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        showTime(d);
                    });
                    Debug.WriteLine("Liste des different laps : ");
                    foreach (var x in d.lapsTimes) Debug.WriteLine("laps : " + x.ToLongTimeString() + "." + x.Millisecond);
                }
            }
            else
            {
                d.lapsTimes.Add(t);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    showTime(d);
                });
                Debug.WriteLine("Liste des different laps : ");
                foreach (var x in d.lapsTimes) Debug.WriteLine("laps : " + x.ToLongTimeString() + "." + x.Millisecond);
            }
        }

        private float subInSec(DateTime after, DateTime before)
        {
            float diff = (after.Hour - before.Hour)*3600 + (after.Minute - before.Minute)*60 + after.Second - before.Second;
            diff = diff + ((float)(after.Millisecond - before.Millisecond)) / 1000;
            return diff;
        }


        private String subInString(DateTime after, DateTime before)
        {
            double fdiff = (after.Hour - before.Hour) * 3600 + (after.Minute - before.Minute) * 60 + after.Second - before.Second;
            fdiff = fdiff + ((double)(after.Millisecond - before.Millisecond)) / 1000;

            int diff = (int)Math.Truncate(fdiff);
            int minute = (diff % 3600) / 60;
            int second = ((diff % 3600) % 60);

            int milli = (int)Math.Truncate((fdiff - diff)*1000);

            String r = "";
            if (minute < 10 && minute != 0) r = "0" + minute + ":";
            else r = minute + ":";

            if (second < 10 && second != 0) r += "0" + second + ".";
            else r += second + ".";

            if (milli < 10 && milli != 0) r += "0" + milli;
            else r += milli;
            return r;
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox c = sender as CheckBox;

            String id = c.Tag as String;

            Device d = BleDevices.Instance.findRegisteredById(id);

            //Add View to device
            Grid g = c.Parent as Grid;
            d.stateText = g.FindName("state") as TextBlock;
            d.lapsText = g.FindName("laps") as TextBlock;

            runAllId.Add(id);
        }

        private void Checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox c = sender as CheckBox;

            String id = c.Tag as String;
            runAllId.Remove(id);
        }


        private void showTime(Device d)
        {

            int length = d.lapsTimes.Count();
            d.stateText.Text = "Course en cours, Tour : " + (length);
            

            if (length <= 1)
                d.lapsText.Text = "";
            else
            {
                if (length > 2) d.lapsText.Text += "\n";
                d.lapsText.Text += "Tour " + (length - 1) + " : " + subInString(d.lapsTimes[length-1], d.lapsTimes[length-2]);
            }
        }


        private void endShowTime(Device d)
        {
            d.stateText.Text = "Course terminée";
        }

    }
}
