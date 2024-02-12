using UnityEngine;
using UnityEngine.Events;

using System.Collections;
using System.Collections.Generic;

namespace Exact
{
    ///<summary>
    /// Digital Twin device. Can have multiple components connected to it. 
    /// Serves as the base for the digital representation of a physical object.
    ///</summary>
    public class Device : MonoBehaviour
    {
        ///<summary>
        /// Reference variable to the MQTT Handler for MQTT messages.
        ///</summary>
        private MQTTHandler mqttHandler;

        ///<summary>
        /// Sets the reference to the MQTTHandler object class. 
        ///</summary>
        ///<param name="mqttHandler">MQTTHandler object reference.</param>
        public void SetMQTTHandler(MQTTHandler mqttHandler)
        {
            this.mqttHandler = mqttHandler;
        }

        private void Awake()
        {
            FindDeviceComponents();
        }

        #region Link
        ///<summary>
        /// The state of the link between Digital Twin device(this) and physical device.
        ///</summary>
        public bool linked = false;

        public UnityEvent OnConnect;
        public UnityEvent OnDisconnect;

        ///<summary>
        /// Returns the status of whether this TwinObject is connected to a physical device.
        ///<summary>
        ///<returns>Bool of linked status.</returns>
        public bool GetLinkStatus()
        {
            return linked;
        }

        ///<summary>
        /// Method to set the linked status of this TwinObject. 
        ///</summary>
        ///<param name="linked">Bool to set for the linked status.</param>
        public void SetLinkStatus(bool linked)
        {
            if (this.linked == linked) { return; }

            StopAllCoroutines();

            if (linked)
            {
                StartCoroutine(DelayedOnConnect());
            }
            else
            {
                this.linked = false;
                transform.SendMessage("OnDisconnect");
                OnDisconnect.Invoke();
            }
        }

        IEnumerator DelayedOnConnect()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            linked = true;
            transform.SendMessage("OnConnect");
            OnConnect.Invoke();
        }

        ///<summary>
        /// Method to set the linked status of this TwinObject and the device ID of the physical object.
        /// Called by the MQTTHandler when a new device connects that matches this twin object's configuration ID.
        /// Sends a ping message in return to the physical device to let it know that it has been successfully linked.
        ///</summary>
        ///<param name="deviceID">MAC-address of the physical device.</param>
        public void LinkDevice(string deviceId)
        {
            this.deviceId = deviceId;
            SetLinkStatus(true);
        }
        #endregion

        #region IdNameType

        ///<summary>
        /// Gets the device ID of the object. This is the MAC-address of the physical object.
        ///</summary>
        ///<returns>A Device ID string is returned.</returns>
        public string GetDeviceId() { return deviceId; }
        [SerializeField]
        private string deviceId;

        ///<summary>
        /// Set if the device name variable should be used to connect.
        ///</summary>
        public bool useDeviceName = false;

        ///<summary>
        /// Name of the device, to be set in the editor, or before the device connects. Used to prepare one digital twin for one specific physical device. Eg. Controller 1 in Unity linked up with Controller 1 physical.
        ///</summary>
        public string GetDeviceName() { return deviceName; }
        [SerializeField]
        private string deviceName;

        ///<summary>
        /// Set if the config name variable should be used to connect.
        ///</summary>
        public bool useDeviceType = false;

        ///<summary>
        /// Device type set in the editor and on the physical device to be used when linking the digital object to a physical one.
        ///</summary>
        ///<summary>
        public string GetDeviceType() { return deviceType; }
        [SerializeField]
        private string deviceType;

        ///<summary>
        /// Sets the unique name for the device.
        ///</summary>
        ///<param name="deviceName">Unique name identifier set for the device.</param>
        public void SetDeviceName(string deviceName)
        {
            this.deviceName = deviceName;
            useDeviceName = true;
        }

        ///<summary>
        /// Sets whether the unique device name should use used or not.
        ///</summary>
        ///<param name="useDeviceName">Boolean value for using device name.</param>
        public void SetUseDeviceName(bool useDeviceName)
        {
            this.useDeviceName = useDeviceName;
        }

        ///<summary>
        /// Gets whether the device should use the unique name or not.
        ///</summary>
        public bool IsUsingDeviceName()
        {
            return useDeviceName;
        }

        #endregion

        #region Components

        ///<summary>
        /// Dictionary for the DeviceComponent type components added to the twin object.
        ///</summary>
        private Dictionary<string, DeviceComponent> deviceComponents = new Dictionary<string, DeviceComponent>();

        private void FindDeviceComponents()
        {
            var comps = GetComponentsInChildren<DeviceComponent>();
            foreach (var comp in comps)
            {
                deviceComponents.Add(comp.GetComponentType(), comp);
            }
        }

        #endregion

        ///<summary>
        /// Sends a message to the MQTTHandler to be sent to a device. Takes the topic and a payload.
        /// Payload is converted from string to a byte array.
        ///</summary>
        public void SendMessage(string subtopic, byte[] payload)
        {
            if (mqttHandler == null) { Debug.LogWarning("No MQTT handler"); return; }
            if (string.IsNullOrEmpty(deviceId)) { Debug.LogWarning("No device id"); return; }
            if (!linked) { Debug.LogWarning("Not linked"); return; }

            mqttHandler.SendMessage(string.Format("exact/{0}/{1}", deviceId, subtopic), payload);
        }

        ///<summary>
        /// Adds an action message to the action message buffer. Takes the name of the component to be updated and a string payload.
        /// Payload is converted from string to a byte array.
        /// Payload has an added variable in front for the payload length.
        ///</summary>
        public void SendAction(string component, string action, byte[] payload)
        {
            SendMessage(string.Format("action/{0}/{1}", component, action), payload);
        }

        ///<summary>
        /// Adds a get message to the get message buffer. Get message is based on the component's name. 
        /// The component name and an empty byte array are added to a message pair object which is added to the buffer.
        ///</summary>
        public void SendGetMessage(string component, string variable)
        {
            SendMessage(string.Format("get/{0}/{1}", component, variable));
        }


        ///<summary>
        /// Method called by the MQTTHandler when an Event type message is received from the physical device.
        /// Resets the ping message timer.
        ///</summary>
        ///<param name="component">The component thas issued the event.</param>
        ///<param name="eventType">The type of event.</param>
        ///<param name="payload">The payload of the event.</param>
        public void EventMessage(string component, string eventType, byte[] payload)
        {
            if (deviceComponents.ContainsKey(component))
            {
                deviceComponents[component].OnEvent(eventType, payload);
            }
        }

        ///<summary>
        /// Method called by the MQTTHandler when a Value type message is received from the physical device. 
        /// Value type messages are responses from the Get type messages sent from the digital device.
        /// Resets the ping message timer.
        ///</summary>
        ///<param name="component">The component thas issued the event.</param>
        ///<param name="variable">The variable name.</param>
        ///<param name="payload">The value of the variable.</param>
        public void ValueMessage(string component, string variable, byte[] payload)
        {
            if (deviceComponents.ContainsKey(component))
            {
                deviceComponents[component].OnValue(variable, payload);
            }
        }
    }
}
