// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Headjack.Cinema.Messages
{
    /// <summary>
    /// The message IDs of the default messages included with Headjack Cinema,
    /// for custom message IDs, start at DefaultMessages.HighestDefault
    /// </summary>
    public enum DefaultMessages
    {
        Connect = 0,
        Disconnect,
        DeviceNumber,
        Sync,
        Synced,
        Status,
        Log,
        DeviceInfo,
        Name,
        Playback,
        ViewDirection,
        DeviceHealth,
        DownloadProgress,
        // below are the default actions that can be sent to clients
        SetCinema,
        PrepareVideo,
        SeekTo,
        PlayAt,
        PauseAt,
        StopPlayback,
        DownloadProject,
        CancelDownload,
        DeleteProject,
        ShowMessage,
        ShowProject,
        SetSubtitle,
        // custom cinema messages should start with an ID higher than HighestDefault
        HighestDefault
    }

    public delegate void MessageCallback(NetPeer peer, MessageBase message);

    /// <summary>
    /// Base class that all Cinema message (client->server and server->client) derive from
    /// </summary>
    public abstract class MessageBase
    {
        public const int MAX_STRING_LENGTH = 500;

        public abstract int MessageType { get; }

        public abstract MessageBase NewInstance();
        public abstract void Deserialize(NetPacketReader reader);
        public abstract void Serialize(NetDataWriter writer);

        /// <summary>
        /// Ensures max text message length for cinema Messages
        /// </summary>
        public static string SanitizeText(string input) {
            if (input == null || input.Length <= MAX_STRING_LENGTH) {
                return input;
            } else {
                Debug.LogWarning("[MessageBase] Text is too long for network message. Max length: " + MAX_STRING_LENGTH);
                return input.Remove(MAX_STRING_LENGTH);
            }
        }
    }
}