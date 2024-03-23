// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageName : MessageBase
    {
        public string name;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Name;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageName();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            name = reader.GetString(MAX_STRING_LENGTH);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(SanitizeText(name));
        }

        public override string ToString() {
            return $"[MessageName] name: {name}";
        }
    }
}