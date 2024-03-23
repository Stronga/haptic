// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    /// Video playback state (progress, player state, etc.)
    /// </summary>
    public class MessagePlayback : MessageBase
    {
        public enum Status {
            Error = 0,
            NotReady,
            Ready,
            Buffering,
            Playing,
            Paused
        }

        public Status status;
        public string activeProject;
        public long startTimestamp;
        public long videoStartTime;
        public long videoTotalTime;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Playback;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessagePlayback();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            status = (Status)reader.GetInt();
            activeProject = reader.GetString(MAX_STRING_LENGTH);
            startTimestamp = reader.GetLong();
            videoStartTime = reader.GetLong();
            videoTotalTime = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put((int)status);
            writer.Put(activeProject);
            writer.Put(startTimestamp);
            writer.Put(videoStartTime);
            writer.Put(videoTotalTime);
        }

        public override string ToString() {
            return $"[MessagePlayback] status: {status} - activeProject: {activeProject} - startTimestamp: {startTimestamp} - videoStartTime: {videoStartTime} - videoTotalTime: {videoTotalTime}";
        }
    }
}