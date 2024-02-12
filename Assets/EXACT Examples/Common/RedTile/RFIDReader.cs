using UnityEngine;
using NaughtyAttributes;

using UnityEngine.Events;

namespace Exact.Example
{
    [RequireComponent(typeof(Device))]
    public class RFIDReader : DeviceComponent
    {
        public override string GetComponentType() { return "rfid_reader"; }

        [SerializeField, OnValueChanged("OnReaderEnabledChanged")]
        bool readerEnabled;

        [ShowNonSerializedField]
        string lastReadId = "";

        /// <summary>
        /// Called when a RFID tag enters or exits the RFID reader
        /// </summary>
        public UnityEvent<string> OnEnter, OnExit;

        public void OnConnect()
        {
            SetReaderEnabled(readerEnabled, true);
        }

        public override void OnEvent(string eventType, byte[] payload)
        {
            string payloadString = System.Text.Encoding.Default.GetString(payload);
            switch (eventType)
            {
                case "rfid_enter":
                    Enter(payloadString);
                    break;
                case "rfid_leave":
                    Exit(payloadString);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Called when a RFID tag is detected
        /// </summary>
        /// <param name="tag">The tag</param>
        public void Enter(string tag)
        {
            Debug.Log("RFID enter: " + tag);
            lastReadId = tag;
            OnEnter.Invoke(tag);
        }

        /// <summary>
        /// Called when a RFID tag is no longer detected
        /// </summary>
        /// <param name="tag">The tag</param>
        public void Exit(string tag)
        {
            Debug.Log("RFID exit: " + tag);
            OnExit.Invoke(tag);
        }

        /// <summary>
        /// Ënables or disables the RFID reader
        /// </summary>
        /// <param name="enabled">Whether the RFID reader is enabled</param>
        /// <param name="forceUpdate">Whether the physical device is updated even if the sensitivity has not changed.</param>
        public void SetReaderEnabled(bool readerEnabled, bool forceUpdate = false)
        {
            if (this.readerEnabled != readerEnabled || forceUpdate)
            {
                this.readerEnabled = readerEnabled;
                if (readerEnabled) 
                { 
                    SendAction("enable_rfid"); 
                }
                else 
                { 
                    SendAction("disable_rfid"); 
                }
            }
        }

        public string GetLastReadId() { return lastReadId; }
        public byte[] GetLastReadIdAsBytes() { return Utility.StringToByteArray(lastReadId, ':'); }

        //
        // Value changed callbacks
        //

        private void OnReaderEnabledChanged()
        {
            SetReaderEnabled(readerEnabled, true);
        }
    }
}
