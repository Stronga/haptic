// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    /// Sets preferred subtitle. Requires active videoplayer, so don't call with NotReady status
    /// </summary>
    public class MessageSetSubtitle : MessageBase
    {
        // if projectId does not match currently playing project, then subtitle is not changed
        public string projectId;
        // index of -1 is disable subtitles
        public int subtitleIndex = -1;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.SetSubtitle;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageSetSubtitle();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            projectId = reader.GetString(MAX_STRING_LENGTH);
            subtitleIndex = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(projectId);
            writer.Put(subtitleIndex);
        }

        public override string ToString() {
            return $"[MessageSetSubtitle] projectId: {projectId} - subtitleIndex: {subtitleIndex}";
        }
    }
}