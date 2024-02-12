using UnityEngine;
using NaughtyAttributes;

using System.Collections.Generic;

namespace Exact
{
    public class ExactManager : MonoBehaviour
    {
        [SerializeField, Required]
        Settings settings;

        MQTTHandler mqttHandler = null;

        List<Device> devices = new List<Device>();

        private void Awake()
        {
            Debug.Log("Creating MQTTHandler" + settings.host + settings.port);
            mqttHandler = new MQTTHandler(settings.host, settings.port);

       //     foreach (Device device in FindObjectsOfType<Device>()) // DS 030123. Obsolete Warning CS0618
            foreach (Device device in FindObjectsByType<Device>(FindObjectsSortMode.None))
                {
                Debug.Log(device);
                devices.Add(device);
                mqttHandler.AddDevice(device);
            }
        }

        private void OnDestroy()
        {
            if (mqttHandler != null)
            {
                mqttHandler.SendMessageImediate("exact/all_devices/reset_all_components/unity_quit");
                mqttHandler.Shutdown();
            }
            Debug.Log("Application ending after " + Time.time + " seconds");
        }

        void Update()
        {
            if (mqttHandler != null)
            {
                mqttHandler.Update();
            }
        }

        /// <summary>
        /// Add a device to the device manager. A device must be added to be able to send and recieve messages.
        /// All devices in the scene are automaticly added when the device manager is loaded.
        /// Devices instanced at a later time must be added manualy.
        /// </summary>
        /// <param name="device">The device to be added</param>
        public void AddDevice(Device device)
        {
            if(!devices.Contains(device))
            {
                devices.Add(device);
                mqttHandler.AddDevice(device);
            }
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <returns></returns>
        public List<Device> GetDevices()
        {
            return new List<Device>(devices);
        }

        /// <summary>
        /// Gets a list with all the devices containing a spesific component
        /// </summary>
        /// <typeparam name="T">The component</typeparam>
        /// <param name="onlyLinked">Only return linked devices</param>
        /// <returns>A list of devices with the specified component</returns>
        public List<T> GetDevicesWithComponent<T>(bool onlyLinked = true) where T : MonoBehaviour
        {
            List<T> devices = new List<T>();
            var allDevices = onlyLinked ? GetConnectedDevices() : this.devices;
            foreach (Device device in allDevices)
            {
                T comp = device.GetComponent<T>();
                if (comp != null)
                {
                    devices.Add(comp);
                }
            }
            return devices;
        }

        /// <summary>
        /// Gets a list with all the linked devices
        /// </summary>
        /// <returns>A list of devices</returns>
        public List<Device> GetConnectedDevices()
        {
            var devices = new List<Device>();
            foreach (Device device in this.devices)
            {
                if (device.linked) { devices.Add(device); }
            }
            return devices;
        }

        ///<summary>
        /// Whether all twin devices has connected to a physical device.
        ///</summary>
        /// <returns>True if all devices are linked</returns>
        public bool AllDevicesConnected()
        {
            foreach (var device in devices)
            {
                if (!device.GetLinkStatus())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
