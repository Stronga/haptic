// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageStatus : MessageBase
    {
        public enum Status
        {
            Error = 0,
            Suspended,
            Syncing,
            Inactive,
            Ready
        }

        public Status status;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Status;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageStatus();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            status = (Status)reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put((int)status);
        }

        public override string ToString() {
            return $"[MessageStatus] status: {status}";
        }
    }
}