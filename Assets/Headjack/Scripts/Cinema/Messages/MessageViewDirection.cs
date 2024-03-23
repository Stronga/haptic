// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageViewDirection : MessageBase
    {
        public long timestamp;
        public long videoTime;
        public Quaternion viewDirection;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.ViewDirection;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageViewDirection();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            timestamp = reader.GetLong();
            videoTime = reader.GetLong();
            // get latest view direction Quaternion per component
            viewDirection.x = reader.GetFloat();
            viewDirection.y = reader.GetFloat();
            viewDirection.z = reader.GetFloat();
            viewDirection.w = reader.GetFloat();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(timestamp);
            writer.Put(videoTime);

            writer.Put(viewDirection.x);
            writer.Put(viewDirection.y);
            writer.Put(viewDirection.z);
            writer.Put(viewDirection.w);
        }

        public override string ToString() {
            Vector3 eulers = viewDirection.eulerAngles;
            return $"[MessageViewDirection] timestamp: {timestamp} videoTime: {videoTime} viewDirection (XYZ euler): {eulers.x} - {eulers.y} - {eulers.z}";
        }
    }
}