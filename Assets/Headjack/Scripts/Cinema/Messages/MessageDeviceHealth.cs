// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDeviceHealth : MessageBase
    {
        public int battery;
        public int temperature;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.DeviceHealth;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDeviceHealth();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            battery = reader.GetInt();
            temperature = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer)
        {  
            writer.Put(MessageType);

            // devices without a battery occassionaly return a negative battery percentage, so sanitize the number
            int saneBattery = battery;
            if (saneBattery < 0 || saneBattery > 100) {
                saneBattery = 100;
            }
            writer.Put(saneBattery);
            writer.Put(temperature);
        }

        public override string ToString() {
            return $"[MessageDeviceHealth] battery: {battery} - temperature: {temperature}";
        }
    }
}