// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageShowMessage : MessageBase
    {
        public string message;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.ShowMessage;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageShowMessage();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            message = reader.GetString(MAX_STRING_LENGTH);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put(SanitizeText(message));
        }

        public override string ToString() {
            return $"[MessageShowMessage] message: {message}";
        }
    }
}