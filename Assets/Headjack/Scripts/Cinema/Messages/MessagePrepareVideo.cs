// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessagePrepareVideo : MessageBase
    {
        public string projectId;
        public long videoTime;
        public bool stream;


        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.PrepareVideo;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessagePrepareVideo();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            projectId = reader.GetString(MAX_STRING_LENGTH);
            videoTime = reader.GetLong();
            stream = reader.GetBool();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(projectId);
            writer.Put(videoTime);
            writer.Put(stream);
        }

        public override string ToString() {
            return $"[MessagePrepareVideo] projectId: {projectId} - videoTime: {videoTime} - stream: {stream}";
        }
    }
}