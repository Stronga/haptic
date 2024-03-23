// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageSeekTo : MessageBase
    {
        public long videoTime;
        public long timestamp;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.SeekTo;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageSeekTo();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            videoTime = reader.GetLong();
            timestamp = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(videoTime);
            writer.Put(timestamp);
        }

        public override string ToString() {
            return $"[MessageSeekTo] videoTime: {videoTime} - timestamp: {timestamp}";
        }

        public long GetLocalTimestamp(NetPeer server) {
            ServerMetadata serverMeta = (ServerMetadata)server.Tag;
            return timestamp - serverMeta.TimeOffset;
        }
    }
}