// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageStopPlayback : MessageBase
    {
        public long timestamp;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.StopPlayback;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageStopPlayback();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            timestamp = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(timestamp);
        }

        public override string ToString() {
            return $"[MessageStopPlayback] timestamp: {timestamp}";
        }

        public long GetLocalTimestamp(NetPeer server) {
            ServerMetadata serverMeta = (ServerMetadata)server.Tag;
            return timestamp - serverMeta.TimeOffset;
        }
    }
}