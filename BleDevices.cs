using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace VMALE
{
    class Device
    {
        public static int maxLaps = 3;

        public string HumanName { get; set; }
        public string BleName { get; set; }
        public string BleID { get; set; }
        public Boolean firstDetection = false;
        public List<DateTime> lapsTimes { get; set; } = new List<DateTime>();

        public TextBlock stateText = null;
        public TextBlock lapsText = null;

        public Device() { }

        public Device(DeviceInformation deviceInfo)
        {
            HumanName = "Coureur";
            BleName = deviceInfo.Name;
            BleID = deviceInfo.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if(obj.GetType().Equals(this.GetType()))
            {
                Device d = (Device)obj;
                return BleID.Equals(d.BleID);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return BleID.GetHashCode();
        }
    }


    class BleDevices
    {
        static private BleDevices instance = null;
        static private List<Device> registerDevices = null;
        static private List<Device> runners = null;


        private BleDevices()
        {
            registerDevices = new List<Device>();
            runners = new List<Device>();
        }

        public static BleDevices Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BleDevices();
                }
                return instance;
            }
        }

        public List<Device> getRegister()
        {
            return registerDevices;
        }


        public Device findRegisteredById(String id)
        {
            foreach(Device d in registerDevices)
            {
                if (d.BleID.Equals(id)) return d;
            }
            return null;
        }

        
        public List<Device> getRunners()
        {
            return runners;
        }

    }
}
