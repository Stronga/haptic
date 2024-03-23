// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessagePauseAt : MessageBase
    {
        public long timestamp;
        public long videoTime;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.PauseAt;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessagePauseAt();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            timestamp = reader.GetLong();
            videoTime = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(timestamp);
            writer.Put(videoTime);
        }

        public override string ToString() {
            return $"[MessagePauseAt] timestamp: {timestamp} - videoTime: {videoTime}";
        }

        public long GetLocalTimestamp(NetPeer server) {
            ServerMetadata serverMeta = (ServerMetadata)server.Tag;
            return timestamp - serverMeta.TimeOffset;
        }
    }
}