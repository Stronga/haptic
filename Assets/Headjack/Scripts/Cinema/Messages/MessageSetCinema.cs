// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageSetCinema : MessageBase
    {
        public bool enable = true;
        public bool downloadAll = true;
        public long timestamp;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.SetCinema;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageSetCinema();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            enable = reader.GetBool();
            downloadAll = reader.GetBool();
            timestamp = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(enable);
            writer.Put(downloadAll);
            writer.Put(timestamp);
        }

        public override string ToString() {
            return $"[MessageSetCinema] enable: {enable} - downloadAll: {downloadAll} - timestamp: {timestamp}";
        }
    }
}