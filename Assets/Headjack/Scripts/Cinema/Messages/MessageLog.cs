// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    ///
    /// </summary>
    public class MessageLog : MessageBase
    {
        public enum Level
        {
            Verbose = 0,
            Info,
            Warning,
            Error
        }

        public Level level;
        public string message;

        public override int MessageType
        {
            get
            {
                return (int)DefaultMessages.Log;
            }
        }

        public override MessageBase NewInstance()
        {
            return new MessageLog();
        }

        public override void Deserialize(NetPacketReader reader)
        {
            level = (Level)reader.GetInt();
            message = reader.GetString(MAX_STRING_LENGTH);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MessageType);

            writer.Put((int)level);
            writer.Put(SanitizeText(message));
        }

        public override string ToString() {
            return $"[MessageLog] level: {level} - message: {message}";
        }
    }
}