// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageSync : MessageBase
    {
        public int syncId;
        public long clientSentTime;
        public long serverReceivedTime;
        public long serverSentTime;
        public long clientReceiveTime;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Sync;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageSync();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            syncId = reader.GetInt();
            clientSentTime = reader.GetLong();
            serverReceivedTime = reader.GetLong();
            serverSentTime = reader.GetLong();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(syncId);
            writer.Put(clientSentTime);
            writer.Put(serverReceivedTime);
            writer.Put(serverSentTime);
        }

        public long getOffset() {
            return ((serverReceivedTime - clientSentTime) + (serverSentTime - clientReceiveTime)) / 2;
        }

        public long getRTT() {
            return (clientReceiveTime - clientSentTime) - (serverSentTime - serverReceivedTime);
        }

        public override string ToString() {
            return $"[MessageSync] syncId: {syncId} - clientSentTime: {clientSentTime} - serverReceivedTime: {serverReceivedTime} - serverSentTime: {serverSentTime}"
                + $" - clientReceiveTime: {clientReceiveTime} - offset: {getOffset()} - rtt: {getRTT()}";
        }
    }
}