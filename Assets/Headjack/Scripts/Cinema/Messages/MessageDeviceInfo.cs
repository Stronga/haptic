// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDeviceInfo : MessageBase
    {
        public string vrSdk;
        public string os;
        public string model;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.DeviceInfo;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDeviceInfo();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            vrSdk = reader.GetString(MAX_STRING_LENGTH);
            os = reader.GetString(MAX_STRING_LENGTH);
            model = reader.GetString(MAX_STRING_LENGTH);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);
            
            writer.Put(SanitizeText(vrSdk));
            writer.Put(SanitizeText(os));
            writer.Put(SanitizeText(model));
        }

        public override string ToString() {
            return $"[MessageDeviceInfo] vrSdk: {vrSdk} - os: {os} - model: {model}";
        }
    }
}