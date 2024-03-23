// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageDisconnect : MessageBase
    {
        public DisconnectInfo disconnectInfo;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Disconnect;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageDisconnect();
        }

        public override void Deserialize(NetPacketReader reader)
        {

        }

        public override void Serialize(NetDataWriter writer)
        {

        }
    }
}