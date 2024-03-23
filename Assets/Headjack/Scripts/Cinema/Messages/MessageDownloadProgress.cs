// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using System;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDownloadProgress : MessageBase
    {
        [Flags]
        public enum Status {
            None = 0,
            Downloadable = 0x00000002,
            Downloading = 0x00000004,
            Downloaded = 0x00000008
        }

        public string projectId;
        public long progress;
        public long total;
        public Status status;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.DownloadProgress;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDownloadProgress();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            projectId = reader.GetString();
            progress = reader.GetLong();
            total = reader.GetLong();
            status = (Status)reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(projectId);
            writer.Put(progress);
            writer.Put(total);
            writer.Put((int)status);
        }

        public override string ToString() {
            return $"[MessageDownloadProgress] projectId: {projectId} - progress: {progress} - total: {total} - status: {status}";
        }
    }
}