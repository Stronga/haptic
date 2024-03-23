// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDeviceNumber : MessageBase
    {
        public int deviceNumber;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.DeviceNumber;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDeviceNumber();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            deviceNumber = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(deviceNumber);
        }

        public override string ToString() {
            return $"[MessageDeviceNumber] device number: {deviceNumber}";
        }
    }
}