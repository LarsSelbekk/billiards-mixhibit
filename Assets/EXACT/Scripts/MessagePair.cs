namespace Exact
{
    ///<summary>
    /// Class used by the MQTTHandler to hold data on the incoming messages.
    ///</summary>
    public class MessagePair
    {
        ///<summary>
        /// Topic of the incoming message from MQTT.
        ///</summary>
        public string topic;
        ///<summary>
        /// Payload of the incoming message from MQTT.
        ///</summary>
        public byte[] payload;

        ///<summary>
        /// Constructor. Sets the topic and payload variables of this object from the MQTT message.
        ///</summary>
        ///<param name="topic">String of the message topic.</param>
        ///<param name="payload">Byte array of the message payload.</param>
        public MessagePair(string topic, byte[] payload)
        {
            this.topic = topic;
            this.payload = payload;
        }
    }
}
