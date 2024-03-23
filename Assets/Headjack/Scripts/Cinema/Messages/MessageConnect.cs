// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageConnect : MessageBase
    {
        public string deviceId;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Connect;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageConnect();
        }

        public override void Deserialize(NetPacketReader reader)
        {

        }

        public override void Serialize(NetDataWriter writer)
        {

        }
    }
}