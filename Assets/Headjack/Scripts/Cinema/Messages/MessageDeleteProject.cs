﻿// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDeleteProject : MessageBase
    {
        public string projectId;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.DeleteProject;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDeleteProject();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            projectId = reader.GetString(MAX_STRING_LENGTH);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(projectId);
        }

        public override string ToString() {
            return $"[MessageDeleteProject] projectId: {projectId}";
        }
    }
}