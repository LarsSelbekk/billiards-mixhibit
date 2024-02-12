using UnityEngine;

using System.Threading;
using System.Collections.Generic;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Exact
{
    ///<summary>
    /// MQTT handler class for receiving and sending MQTT messages to the system.
    /// Holds references to all the twin devices in the game client and sends mqtt message updates to them accordingly.
    ///</summary>
    public class MQTTHandler
    {
        ///<summary>
        /// MQTTClient class object from the MQTT Library used.
        ///</summary>
        private MqttClient client = null;
        private Thread connectThread = null;
        private Thread sendThread = null;

        private bool shutdown = false;

        ///<summary>
        /// List of the devices added to the system.
        ///<summary>
        private List<Device> devices = new List<Device>();

        ///<summary>
        /// Message buffers
        ///</summary>
        private Queue<MessagePair> messageBufferIn = new Queue<MessagePair>();
        private Queue<MessagePair> messageBufferOut = new Queue<MessagePair>();

        ///<summary>
        /// Constructor and initialization of the MQTT handler object. Creates the MQTT Client object with the host address and port. 
        /// Sets the message received callback method, and the client's id on the network.
        /// Sets the client to subscribe to the unity topic on the MQTT network.
        ///</summary>
        ///<param name="hostaddress">MQTT broker's ip on the network. Defaults to 127.0.0.1.</param>
        ///<param name="port">MQTT broker's port on the network. Defaults to 1883.</param>
        public MQTTHandler(string hostaddress = "192.168.4.1", int port = 1883)
        {
            connectThread = new Thread(() =>
            {
                Debug.Log("Starting connect thread");
                Debug.Log("Creating client");
                client = new MqttClient(hostaddress, port, false, null, null, MqttSslProtocols.None);
                client.MqttMsgPublishReceived += HandleMQTTMessage;

                Debug.Log("Connecting");
                client.Connect("Unity_Client", null, null, false, MqttMsgConnect.QOS_LEVEL_AT_MOST_ONCE, true,
                                            "exact/all_devices/reset_all_components", "mqtt_broker", true, MqttMsgConnect.KEEP_ALIVE_PERIOD_DEFAULT);
                Debug.Log("Connected");
                client.Subscribe(new string[] { "exact/connected/#", "exact/disconnected/#", "exact/device_message/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                client.Publish("exact/all_devices/are_you_connected", new byte[] { 0 });
                Debug.Log("Exiting connect thread");
            });
            connectThread.Start();

            sendThread = new Thread(() => { SendThread(); });
            sendThread.Start();
        }
        public void Shutdown()
        {
            Debug.Log("Shutting down MQTTHandler");
            shutdown = true;
            if (client != null)
            {
                client.Disconnect();
            }
        }

        public void Abort()
        {
            if (client != null)
            {
                client.Disconnect();
            }
            if (connectThread != null)
            {
                connectThread.Abort();
            }
            if (sendThread != null)
            {
                sendThread.Abort();
            }
        }

        private void SendThread()
        {
            Debug.Log("Starting send thread");
            while (!shutdown)
            {
                while (messageBufferOut.Count > 0 && client != null)
                {
                    MessagePair message = messageBufferOut.Dequeue();
                    if (message.payload == null)
                    {
                        message.payload = new byte[] { 0 };
                    }
                    client.Publish(message.topic, message.payload);
                }
                Thread.Sleep(1);
            }
            Debug.Log("Exiting send thread");

        }

        ///<summary>
        /// MQTT message handler. 
        /// Handles incoming MQTT messages and creates a message pair object with the topic and payload. 
        /// The message pair is added to the messageBufferIn object which is called in the standard unity thread for messages to be further handled.
        ///<summary>
        ///<param name="sender"></param>
        ///<param name="e">Message object created by the MQTT client. Contains the topic and payload.</param>
        void HandleMQTTMessage(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.Log("Received: " + e.Topic);
            messageBufferIn.Enqueue(new MessagePair(e.Topic, e.Message));
        }

        /// <summary>
        /// Update function called from the unity thread to handle messages in the unity thread.
        /// </summary>
        public void Update()
        {
            while (messageBufferIn.Count > 0)
            {
                MessagePair message = messageBufferIn.Dequeue();

                string[] topicSplit = message.topic.Split('/');

                if (topicSplit.Length < 3)
                {
                    Debug.LogWarning("Invalid message: " + message.topic);
                    continue;
                }

                if (topicSplit[0] != "exact") { continue; }

                string messageType = topicSplit[1];
                string deviceId = topicSplit[2];
                string[] subTopic = topicSplit.SubArray(3, topicSplit.Length - 3);

                switch (messageType)
                {
                    case "connected":
                        DeviceConnect(deviceId, subTopic);
                        break;

                    case "disconnected":
                        DeviceDisconnect(deviceId);
                        break;

                    case "device_message":
                        Device device = GetDeviceById(deviceId);
                        if (device != null)
                        {
                            ProcessDeviceMessage(device, subTopic, message.payload);
                        }
                        break;

                    default:
                        Debug.Log("Unknown message type: " + messageType);
                        break;
                }
            }
        }

        ///<summary>
        /// Called when a new device connects. Extracts the configuration and ID of the device.
        /// If the ID exists, it reconnects the device, if not it checks to find a configuration that fits and links that.
        ///</summary>
        ///<param name="deviceId">The id of the connected device</param>
        ///<param name="subTopic">String array of the split topic from the MQTT connect message</param>
        private void DeviceConnect(string deviceId, string[] subTopic)
        {
            // exact/connected/id/type/name
            string deviceType = subTopic.Length > 0 ? subTopic[0] : "";
            string deviceName = subTopic.Length > 1 ? subTopic[1] : "";

            Debug.LogFormat("New Device detected: {0}, {1}, {2}", deviceId, deviceType, deviceName);

            foreach (var device in devices)
            {
                if (device.GetLinkStatus() == false)
                {
                    Debug.Log("Found object without link!");
                    if (device.GetDeviceId() == deviceId)
                    {
                        Debug.Log("Id connect!");
                        device.SetLinkStatus(true);
                        device.LinkDevice(deviceId);
                        return;
                    }
                }
            }

            foreach (var device in devices)
            {
                if (device.GetLinkStatus() == false && device.useDeviceName)
                {
                    Debug.LogFormat("Device is using name! Digital: '{0}', Physical: '{1}'", device.GetDeviceName(), deviceName);
                    if (device.GetDeviceName() == deviceName)
                    {
                        Debug.Log("Name connect!");
                        device.LinkDevice(deviceId);
                        return;
                    }
                }
            }

            foreach (var device in devices)
            {
                if (device.GetLinkStatus() == false && device.useDeviceType)
                {
                    Debug.LogFormat("Device is using type! Digital: '{0}', Physical: '{1}'", device.GetDeviceType(), deviceType);
                    if (device.GetDeviceType() == deviceType)
                    {
                        Debug.Log("Type connect!");
                        device.LinkDevice(deviceId);
                        device.SetDeviceName(deviceName);
                        return;
                    }
                }
            }

            Debug.Log("Link not possible!");
        }

        private void DeviceDisconnect(string deviceId)
        {
            Device to = GetDeviceById(deviceId);
            if (to != null)
            {
                if (to.GetLinkStatus())
                {
                    to.SetLinkStatus(false);
                }
            }
        }

        private void ProcessDeviceMessage(Device device, string[] subTopic, byte[] payload)
        {
            if (subTopic.Length < 1)
            {
                Debug.LogWarning("Invalid message");
                return;
            }

            var sub = subTopic.SubArray(1, subTopic.Length - 1);
            switch (subTopic[0])
            {
                case "ping_ack":
                    DevicePing(device);
                    break;

                case "event":
                    DeviceEvent(device, sub, payload);
                    break;

                case "value":
                    DeviceValue(device, sub, payload);
                    break;

                default:
                    Debug.Log("Unknown device message:" + subTopic[0]);
                    break;
            }
        }

        ///<summary>
        /// Method called when an event type MQTT message is received. Sends the split topic and payload to the corresponding twin object.
        ///</summary>
        ///<param name="topicSplit">MQTT message topic split to a string array.</param>
        ///<param name="payload">MQTT message payload.</param>
        private void DeviceEvent(Device device, string[] subTopic, byte[] payload)
        {
            if (subTopic.Length < 2)
            {
                Debug.LogWarning("Invalid message");
                return;
            }
            string component = subTopic[0];
            string eventType = subTopic[1];

            device.EventMessage(component, eventType, payload);
        }

        ///<summary>
        /// Method called when a value type MQTT message is received. Sends the split topic and payload to the corresponding twin object.
        ///</summary>
        ///<param name="topicSplit">MQTT message topic split to a string array.</param>
        ///<param name="payload">MQTT message payload.</param>
        private void DeviceValue(Device device, string[] subTopic, byte[] payload)
        {
            if (subTopic.Length < 2)
            {
                Debug.LogWarning("Invalid message");
                return;
            }
            string component = subTopic[0];
            string variable = subTopic[1];

            device.ValueMessage(component, variable, payload);
        }

        ///<summary>
        /// Method called when a ping type MQTT message is received. Update's the twin object's link status if needed and calls the PingResponse method on the twin object.
        ///</summary>
        ///<param name="deviceID">Message sender's ID.</param>
        private void DevicePing(Device device)
        {
            if (!device.GetLinkStatus())
            {
                device.SetLinkStatus(true);
            }
        }

        ///<summary>
        /// Sends a simple message to the MQTT network. Message built on TwinObject.
        ///</summary>
        ///<param name="topic">Topic of the MQTT message sent.</param>
        ///<param name="payload">Payload of the MQTT message sent.</param>
        public void SendMessage(string topic, byte[] payload = null)
        {
            messageBufferOut.Enqueue(new MessagePair(topic, payload));
        }

        ///<summary>
        /// Sends a simple message to the MQTT network. Message built on TwinObject.
        ///</summary>
        ///<param name="topic">Topic of the MQTT message sent.</param>
        ///<param name="payload">Payload of the MQTT message sent.</param>
        public void SendMessageImediate(string topic, byte[] payload = null)
        {
            if (payload == null)
            {
                payload = new byte[] { 0 };
            }
            client.Publish(topic, payload);
        }

        ///<summary>
        /// Checks the list of devices and returns the one with the corresponding id.
        ///</summary>
        ///<param name="deviceId">Device Id of twin object to get.</param>
        ///<returns>Returns the device with the Id, or null if no device was found</returns>
        private Device GetDeviceById(string deviceId)
        {
            foreach (var device in devices)
            {
                if (device.GetDeviceId() == deviceId)
                {
                    return device;
                }
            }
            return null;
        }

        ///<summary>
        /// Adds a device to the list of devices to be controlled and updated by the MQTT Handler.
        ///</summary>
        ///<param name="device">A TwinObject object.</param>
        public void AddDevice(Device device)
        {
            device.SetMQTTHandler(this);
            if (device.useDeviceName)
            {
                devices.Insert(0, device);
            }
            else
            {
                devices.Add(device);
            }
        }

        ///<summary>
        /// Returns the list of devices in the MQTT Handler.
        ///</summary>
        ///<returns>List of devices</returns>
        public List<Device> GetDeviceList()
        {
            return devices;
        }

    }
}