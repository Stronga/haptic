// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageSynced : MessageBase
    {
        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Synced;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageSynced();
        }

        public override void Deserialize(NetPacketReader reader)
        {

        }

        public override void Serialize(NetDataWriter writer)
        {

        }
    }
}