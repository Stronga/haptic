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
    /// Data class to hold server metadata including server device ID and server time offset for synchronization
    /// </summary>
    public class ServerMetadata {
        public string DeviceId { get; private set; }
        public long TimeOffset { get; private set; }

        public bool IsSynced { get; private set; }

        public ServerMetadata (string serverId) {
            DeviceId = serverId;
            TimeOffset = 0;
            IsSynced = false;
        }

        public long GetServerTime() {
            return TimeStamp.GetMsLong() + TimeOffset;
        }

        public void SetTimeOffset(long offset) {
            TimeOffset = offset;
            IsSynced = true;
        }
    }

    /// <summary>
    /// Local cinema client, which can discover local cinema servers
    /// and handles the connection(s) and messages
    /// </summary>
    public class LocalClient : MonoBehaviour, INetEventListener {
        // player prefs name storing the persistent device number
        private const string DEVICE_NUMBER_PREFERENCE_KEY = "headjack_cinema_device_number";

        // Reference to this singleton
        private static LocalClient _instance;
        /// <summary>
        /// Singleton instance of cinema client
        /// </summary>
        public static LocalClient Instance 
        {
            get {
                if (_instance == null) {
                    // create singleton
                    Debug.Log("Creating singleton instance of LocalClient");
                    GameObject clientObj = new GameObject("Headjack_LocalClient");
                    _instance = clientObj.AddComponent<LocalClient>();
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

        private NetManager _netClient;
        /// <summary>
        /// Returns first peer/server in connection list (otherwise null), useful for looping through connections
        /// </summary>
        public IEnumerable<NetPeer> Servers {
            get {
                return _netClient;
            }
        }
        // dictionary of message handlers
        private Dictionary<int, MessageBase> _messageHandlers;
        // reusable datawriter for sending messages
        private NetDataWriter _dataWriter;
        // this delegate will be called on any network event/message
        private MessageCallback _onMessage;

        private NetDataWriter _appIdWriter;
        private string _currentAppId;

        void Start()
        {
            if (_instance == null) {
                _instance = this;
            }
            // This object should survive all scene transitions.
            GameObject.DontDestroyOnLoad(_instance);

            _dataWriter = new NetDataWriter();
            _netClient = new NetManager(this);
            _messageHandlers = new Dictionary<int, MessageBase>();

            // register all default message handlers for clients
            RegisterMessageHandler(new MessageSync());
            RegisterMessageHandler(new MessageName());
            RegisterMessageHandler(new MessageDeviceNumber());
            // register default actions
            RegisterMessageHandler(new MessageSetCinema());
            RegisterMessageHandler(new MessagePrepareVideo());
            RegisterMessageHandler(new MessageSeekTo());
            RegisterMessageHandler(new MessagePlayAt());
            RegisterMessageHandler(new MessagePauseAt());
            RegisterMessageHandler(new MessageStopPlayback());
            RegisterMessageHandler(new MessageDownloadProject());
            RegisterMessageHandler(new MessageCancelDownload());
            RegisterMessageHandler(new MessageDeleteProject());
            RegisterMessageHandler(new MessageShowMessage());
            RegisterMessageHandler(new MessageShowProject());
            RegisterMessageHandler(new MessageSetSubtitle());

            _netClient.Start();
            _netClient.UpdateTime = 10;
        }

        void FixedUpdate()
        {
            _netClient.PollEvents();

            if (Time.frameCount % 30 == 0 && _netClient.PeersCount == 0 && !string.IsNullOrEmpty(App.AppId)) {
                if (_appIdWriter == null || _currentAppId != App.AppId) {
                    _currentAppId = App.AppId;
                    _appIdWriter = new NetDataWriter();
                    _appIdWriter.Put(_currentAppId);
                }
                _netClient.SendDiscoveryRequest(_appIdWriter, LocalServer.SERVER_PORT);
            }
        }

        void OnDestroy()
        {
            if (_netClient != null)
            {
                _netClient.DisconnectAll();
                _netClient.Flush();
                _netClient.PollEvents();
                _netClient.Stop();
            }

            if (this == _instance) {
                _instance = null;
            }
        }

        void OnApplicationPause(bool pauseStatus) {
            if (_netClient != null) {
                if (pauseStatus && _netClient.IsRunning) {
                    // pausing app
                    _netClient.DisconnectAll();
                    _netClient.Flush();
                    _netClient.PollEvents();
                    _netClient.Stop();
                } else if (!pauseStatus && !_netClient.IsRunning) {
                    // resuming app
                    _netClient.Start();
                }
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[CLIENT] We connected to " + peer.EndPoint);

            if (peer.Tag == null)
            {
                Debug.LogError("[CLIENT] Server's metadata (e.g. device ID) was not stored on discovery response");
                return;
            }
            ServerMetadata serverData = (ServerMetadata)peer.Tag;

            // callback with connect message
            MessageConnect messageConnect = new MessageConnect() {
                deviceId = serverData.DeviceId
            };

            _onMessage?.Invoke(peer, messageConnect);

            // start server time sync
            SyncedTime serverSync = new SyncedTime(delegate (NetPeer syncedPeer, MessageBase syncedMessage) {
                _onMessage?.Invoke(syncedPeer, syncedMessage);
            });
            StartCoroutine(serverSync.SendSyncRequests(peer));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            Debug.Log("[CLIENT] We received error " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            // handle packet contents based on message type
            int messageType = -1;
            if(!reader.TryGetInt(out messageType) || messageType < 0)
            {
                Debug.LogError("[CLIENT] Server message does not start with a valid type: " + messageType);
                return;
            }

            if (!_messageHandlers.ContainsKey(messageType))
            {
                Debug.LogError("[CLIENT] No message handler registered for new message with type " + messageType);
                return;
            }

            MessageBase newMessage = _messageHandlers[messageType].NewInstance();
            newMessage.Deserialize(reader);

            // special case: deal with receiving device number from server
            if (newMessage.MessageType == (int)DefaultMessages.DeviceNumber) {
                OnNewDeviceNumber(peer, (MessageDeviceNumber)newMessage);
            }

            _onMessage?.Invoke(peer, newMessage);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.DiscoveryResponse && _netClient.PeersCount == 0 && App.Authkey != null)
            {
                Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
                // if this client was previously assigned a persistent device number, send that to server
                // NOTE: the server will assign a new number of it receives -1
                int deviceNr = PlayerPrefs.GetInt(DEVICE_NUMBER_PREFERENCE_KEY, -1);
                // server should have sent its device ID along, storing that for successful connection
                string serverId = reader.GetString();
                // send unique device ID and auth key in connection request
                _dataWriter.Reset();
                _dataWriter.Put(App.Authkey);
                _dataWriter.Put(App.Guid);
                _dataWriter.Put(deviceNr);
                NetPeer newServer = _netClient.Connect(remoteEndPoint, _dataWriter);
                newServer.Tag = new ServerMetadata(serverId);
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        public void OnConnectionRequest(ConnectionRequest request)
        {

        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[CLIENT] We disconnected because " + disconnectInfo.Reason);

            // callback with disconnect message
            MessageDisconnect messageDisconnect = new MessageDisconnect()
            {
                disconnectInfo = disconnectInfo
            };
            _onMessage?.Invoke(peer, messageDisconnect);
        }

        /// <summary>
        /// Register a new message handler that will serialize/deserialize network messages with a particular type number
        /// </summary>
        /// <param name="handler">New message handler to register with this client</param>
        public void RegisterMessageHandler(MessageBase handler)
        {
            if (_messageHandlers.ContainsKey(handler.MessageType))
            {
                Debug.LogWarning("[CLIENT] A Message handler for messages of type " + handler.MessageType + " is already registered and will be overwritten.");
            }

            _messageHandlers[handler.MessageType] = handler;
        }

        public void RegisterMessageCallback(MessageCallback onMessage)
        {
            _onMessage += onMessage;
        }

        public void UnregisterMessageCallback(MessageCallback onMessage)
        {
            _onMessage -= onMessage;
        }

        /// <summary>
        /// Send message to all connected servers/peers (reliable ordered messaging)
        /// </summary>
        public void SendMessage(MessageBase message) {
            _dataWriter.Reset();
            message.Serialize(_dataWriter);
            _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Send message to specific server/peer (reliable ordered messaging)
        /// </summary>
        public void SendMessage(NetPeer peer, MessageBase message) {
            _dataWriter.Reset();
            message.Serialize(_dataWriter);
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
        }

        private void OnNewDeviceNumber(NetPeer server, MessageDeviceNumber numberMessage) {
            int currentDeviceNr = PlayerPrefs.GetInt(DEVICE_NUMBER_PREFERENCE_KEY, -1);

            if (currentDeviceNr < 0) {
                PlayerPrefs.SetInt(DEVICE_NUMBER_PREFERENCE_KEY, numberMessage.deviceNumber);
            } else if (currentDeviceNr != numberMessage.deviceNumber) {
                Debug.LogError($"[LocalClient] Stored device number does not match number given by server. Stored: {currentDeviceNr}, Server: {numberMessage.deviceNumber}");
            }
        }
    }
}