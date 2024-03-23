// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Headjack.Utils;
using Headjack.Cinema.Messages;

namespace Headjack.Cinema {
    /// <summary>
    /// Data class to hold client metadata including device ID and whether this device can receive commands
    /// </summary>
    public class ClientMetadata {
        public string DeviceId { get; private set; }
        public int DeviceNumber { get; private set; }
        public bool CanReceiveCommands { get; private set; }

        public ClientMetadata (string clientId, int deviceNr, bool receivesCommands) {
            DeviceId = clientId;
            DeviceNumber = deviceNr;
            CanReceiveCommands = receivesCommands;
        }
    }

    /// <summary>
    /// Local cinema remote control server, which handles the server discovery 
    /// and handles the device connections
    /// </summary>
    public class LocalServer : MonoBehaviour, INetEventListener
    {
        public const int SERVER_PORT = 49895;
        // the player preference key that stores the persistent device number across app sessions
        private const string DEVICE_NUMBER_KEY = "headjack_device_number";

        // Reference to this singleton
        private static LocalServer _instance;
        public static LocalServer Instance 
        {
            get {
                if (_instance == null) {
                    // create singleton
                    Debug.Log("Creating singleton instance of LocalServer");
                    GameObject clientObj = new GameObject("Headjack_LocalServer");
                    _instance = clientObj.AddComponent<LocalServer>();
                }
                return _instance;
            }
        }

        public static bool HasInstance
        {
            get {
                if (_instance == null) {
                    return false;
                }
                return true;
            }
        }

        private NetManager _netServer;
        // dictionary of message handlers
        private Dictionary<int, MessageBase> _messageHandlers;
        // reusable datawriter for sending messages
        private NetDataWriter _dataWriter;
        // this delegate will be called on any network event/message
        private MessageCallback _onMessage;
        // number of successful connections counter (for limiting device access)
        private int _nrConnections = 0;
        // persistent connection counter for assigning a device number to each new device
        // NOTE: persistent device number is only unique if the devices always connect to the same host
        private int _persistentDeviceCounter = 0;

        void Start()
        {
            if (_instance == null) {
                _instance = this;
            }
            // This object should survive all scene transitions.
            GameObject.DontDestroyOnLoad(_instance);

            _dataWriter = new NetDataWriter();
            _netServer = new NetManager(this);
            _messageHandlers = new Dictionary<int, MessageBase>();

            _persistentDeviceCounter = PlayerPrefs.GetInt(DEVICE_NUMBER_KEY, 1);

            // register all default message handlers for servers
            RegisterMessageHandler(new MessageSync());
            RegisterMessageHandler(new MessageStatus());
            RegisterMessageHandler(new MessageLog());
            RegisterMessageHandler(new MessageDeviceInfo());
            RegisterMessageHandler(new MessageName());
            RegisterMessageHandler(new MessagePlayback());
            RegisterMessageHandler(new MessageViewDirection());
            RegisterMessageHandler(new MessageDeviceHealth());
            RegisterMessageHandler(new MessageDownloadProgress());

            StartServer();
        }

        void FixedUpdate()
        {
            _netServer.PollEvents();
        }

        void OnDestroy()
        {
            if (_netServer != null)
            {
                StopServer(true);
            }

            if (this == _instance) {
                _instance = null;
            }
        }

        void OnApplicationPause(bool pauseStatus) {
            if (_netServer != null) {
                if (pauseStatus && _netServer.IsRunning) {
                    // pausing app
                    StopServer();
                } else if (!pauseStatus && !_netServer.IsRunning) {
                    // resuming app
                    StartServer();
                }
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[SERVER] We have new peer " + peer.EndPoint);
            if (peer.Tag == null)
            {
                Debug.LogError("[SERVER] Peer's device ID was not stored on connection request");
                return;
            }

            ClientMetadata clientMeta = (ClientMetadata)peer.Tag;

            // send back device number for client to store
            MessageDeviceNumber numberMessage = new MessageDeviceNumber() {
                deviceNumber = clientMeta.DeviceNumber
            };
            // NOTE: use peer.Send instead of SendMessage here as this should be sent even when the peer
            // cannot receive commands
            _dataWriter.Reset();
            numberMessage.Serialize(_dataWriter);
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);

            // callback with connect message
            MessageConnect messageConnect = new MessageConnect() {
                deviceId = clientMeta.DeviceId
            };
            _onMessage?.Invoke(peer, messageConnect);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            Debug.Log("[SERVER] error " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.DiscoveryRequest && App.AppId != null && App.Data != null)
            {
                //Debug.Log("[SERVER] Received discovery request.");
                string discoverAppId = reader.GetString(App.AppId.Length);
                if (discoverAppId != App.AppId)
                {
                    //Debug.Log("[SERVER] Discovering peer is looking for another app (by ID), not sending response");
                    return;
                }
                // discovering client's app ID matches this server's app ID
                // responding to discovery request with this devices unique ID
                _dataWriter.Reset();
                _dataWriter.Put(App.Guid);

                _netServer.SendDiscoveryResponse(_dataWriter, remoteEndPoint);
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (App.Authkey == null) {
                Debug.LogWarning("[LocalServer] Local AUTH key not set, cannot verify new connection");
                return;
            }

            NetDataReader reader = request.Data;
            string key = reader.GetString(App.Authkey.Length);
            string deviceId = reader.GetString(MessageBase.MAX_STRING_LENGTH);
            int deviceNr = reader.GetInt();
            // Only allow connection if app auth key matches
            if (key == App.Authkey)
            {
                ClientMetadata clientMeta = null;
                foreach (NetPeer peer in _netServer) {
                    clientMeta = (ClientMetadata)peer.Tag;
                    if (clientMeta != null && clientMeta.DeviceId == deviceId) {
                        // client is already in the process of connecting, no need to setup another connection
                        Debug.Log($"[LocalServer] Device ({deviceId}) is already connecting, ignore this request");
                        request.Reject();
                        return;
                    }
                }

                NetPeer newPeer = request.Accept();
                // (calculate &) store device number
                if (deviceNr < 0) {
                    deviceNr = _persistentDeviceCounter;
                    _persistentDeviceCounter++;
                } else if (deviceNr >= _persistentDeviceCounter) {
                    _persistentDeviceCounter = deviceNr + 1;
                }
                PlayerPrefs.SetInt(DEVICE_NUMBER_KEY, _persistentDeviceCounter);
                // determine if this device can receive commands (based on app's Cinema device limit)
                bool receivesCommands = true;
                _nrConnections++;
                if (App.Data.App.CinemaLimit >= 0 && _nrConnections > App.Data.App.CinemaLimit) {
                    receivesCommands = false;
                }

                ClientMetadata newClientMeta = new ClientMetadata(deviceId, deviceNr, receivesCommands);
                // match unique device ID to peer connection
                newPeer.Tag = newClientMeta;
            }
            else
            {
                Debug.Log("[SERVER] Connecting device did not provide correct app auth key");
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[SERVER] peer disconnected " + peer.EndPoint + ", info: " + disconnectInfo.Reason);

            // callback with disconnect message
            MessageDisconnect messageDisconnect = new MessageDisconnect()
            {
                disconnectInfo = disconnectInfo
            };
            _onMessage?.Invoke(peer, messageDisconnect);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            // handle packet contents based on message type
            int messageType = -1;
            if(!reader.TryGetInt(out messageType) || messageType < 0)
            {
                Debug.LogError("[SERVER] Peer message does not start with a valid type: " + messageType);
                return;
            }
            
            if (!_messageHandlers.ContainsKey(messageType))
            {
                Debug.LogError("[SERVER] No message handler registered for new message with type " + messageType);
                return;
            }

            MessageBase newMessage = _messageHandlers[messageType].NewInstance();
            newMessage.Deserialize(reader);

            // sync messages are directly handled by this local server
            if (messageType == (int)DefaultMessages.Sync)
            {
                SendSyncResponse(peer, (MessageSync)newMessage);
            }

            _onMessage?.Invoke(peer, newMessage);
        }

        /// <summary>
        /// Register a new message handler that will serialize/deserialize network messages with a particular type number
        /// </summary>
        /// <param name="handler">New message handler to register with this server</param>
        public void RegisterMessageHandler(MessageBase handler)
        {
            if (_messageHandlers.ContainsKey(handler.MessageType))
            {
                Debug.LogWarning("[SERVER] A Message handler for messages of type " + handler.MessageType + " is already registered and will be overwritten.");
            }

            _messageHandlers[handler.MessageType] = handler;
        }

        /// <summary>
        /// Register a callback function that will be executed when a new message arrives
        /// </summary>
        public void RegisterMessageCallback(MessageCallback callback)
        {
            _onMessage += callback;
        }

        /// <summary>
        /// Unregister a registered callback function
        /// </summary>
        public void UnregisterMessageCallback(MessageCallback callback)
        {
            _onMessage -= callback;
        }

        /// <summary>
        /// Send message to all connected clients/peers (reliable ordered messaging)
        /// </summary>
        public void SendMessage(MessageBase message) {
            _dataWriter.Reset();
            message.Serialize(_dataWriter);
            foreach (NetPeer peer in _netServer) {
                ClientMetadata clientMeta = (ClientMetadata)peer.Tag;
                if (clientMeta != null && clientMeta.CanReceiveCommands) {
                    peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        /// <summary>
        /// Send message to specific client/peer (reliable ordered messaging)
        /// </summary>
        public void SendMessage(NetPeer peer, MessageBase message) {
            if (peer == null) {
                Debug.LogWarning("[LocalServer] peer is null, cannot send message");
                return;
            }
            ClientMetadata clientMeta = (ClientMetadata)peer.Tag;
            if (clientMeta != null && clientMeta.CanReceiveCommands) {
                _dataWriter.Reset();
                message.Serialize(_dataWriter);
                peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
            }
        }

        /// <summary>
        /// Get maximum latency of all connected clients (max RTT/2 in ms)
        /// </summary>
        public long GetMaxPing() {
            long maxPing = 0;
            foreach (NetPeer peer in _netServer) {
                maxPing = System.Math.Max(maxPing, peer.Ping);
            }
            return maxPing;
        }

        /// <summary>
        /// stop all connections and stop discovery responses
        /// </summary>
        public void StopServer(bool resetConnections = false) {
            _netServer.DisconnectAll();
            _netServer.Flush();
            _netServer.PollEvents();
            _netServer.Stop();

            if (resetConnections) {
                _nrConnections = 0;
            }
        }

        /// <summary>
        /// Start server and network discovery
        /// </summary>
        public void StartServer() {
            _netServer.Start(SERVER_PORT);
            _netServer.DiscoveryEnabled = true;
            _netServer.UpdateTime = 10;
        }

        /// <summary>
        /// Sends back a response to a sync request by including the currently timestamp (in ms) of this server
        /// </summary>
        /// <param name="peer">peer/client that requested the sync message</param>
        /// <param name="messageSync">the data in the sync request</param>
        private void SendSyncResponse(NetPeer peer, MessageSync messageSync)
        {
            messageSync.serverSentTime = TimeStamp.GetMsLong();
            messageSync.serverReceivedTime = messageSync.serverSentTime - peer.TimeSinceLastPacket;

            _dataWriter.Reset();
            messageSync.Serialize(_dataWriter);
            // Unreliable delivery will ensure no additional delays in responding to sync request,
            // to reduce the latency as much as possible and improve the synchronization
            peer.Send(_dataWriter, DeliveryMethod.Unreliable);
            peer.Flush();
        }
    }
}