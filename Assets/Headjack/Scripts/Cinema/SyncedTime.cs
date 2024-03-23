// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Headjack.Cinema.Messages;
using Headjack.Utils;
using System;

namespace Headjack.Cinema {

	/// <summary>
	/// Syncs time between this client and a server by analysing multiple sync events
	/// </summary>
	public class SyncedTime
	{ 
		private const int WAIT_FOR_SYNC_RESPONSES = 14;
		private const int USE_LOWEST_RTT_RESPONSES = 7;
		private const int MAX_SYNC_REQUESTS = 50;

		private Dictionary<int, MessageSync> _syncResponses;
		// this delegate will be called on sync success
		private MessageCallback _onSynced;

		public SyncedTime(MessageCallback onSynced) {
			_syncResponses = new Dictionary<int, MessageSync>(WAIT_FOR_SYNC_RESPONSES);
			_onSynced = onSynced;

			LocalClient.Instance.RegisterMessageCallback(OnMessage);
		}

		public IEnumerator SendSyncRequests(NetPeer server) 
		{
			NetDataWriter dataWriter = new NetDataWriter();
			MessageSync messageSync = new MessageSync();

			// send sync requests while sync is not established (up to maximum)
			int i = 0;

			while (i < MAX_SYNC_REQUESTS && _syncResponses.Count < WAIT_FOR_SYNC_RESPONSES) {
				dataWriter.Reset();
				messageSync.syncId = i;
				messageSync.clientSentTime = TimeStamp.GetMsLong();
				messageSync.Serialize(dataWriter);

				server.Send(dataWriter, DeliveryMethod.Unreliable);
				server.Flush();

				i++;
				yield return new WaitForSecondsRealtime(0.25f);
			}

			// TODO: deal with failure to sync

			yield break;
		}

		private void OnMessage(NetPeer peer, MessageBase message) 
		{
			if (message.MessageType == (int)DefaultMessages.Sync) {
				OnSyncResponse(peer, (MessageSync)message);
			}
		}

		private void OnSyncResponse(NetPeer peer, MessageSync message) {
			message.clientReceiveTime = TimeStamp.GetMsLong() - peer.TimeSinceLastPacket;

			if (_syncResponses.ContainsKey(message.syncId)) {
				// we have previously received this sync response already (duplicate packet),
				// so drop this data
				return;
			}

			_syncResponses[message.syncId] = message;

			if (_syncResponses.Count >= WAIT_FOR_SYNC_RESPONSES) {
				// we have enough sync data to finish syncing
				LocalClient.Instance.UnregisterMessageCallback(OnMessage);
				// sort sync responses by RTT to reject the highest
				List<int> orderedSyncs = new List<int>(USE_LOWEST_RTT_RESPONSES);
				int index;
				int maxIndex;
				long cRtt;
				foreach (KeyValuePair<int, MessageSync> syncPair in _syncResponses) {
					cRtt = syncPair.Value.getRTT();
					maxIndex = Math.Min(orderedSyncs.Count - 1, USE_LOWEST_RTT_RESPONSES - 1);
					if (orderedSyncs.Count < USE_LOWEST_RTT_RESPONSES || cRtt < _syncResponses[orderedSyncs[maxIndex]].getRTT()) {
						for (index = 0; index <= maxIndex; ++index) {
							if (cRtt < _syncResponses[orderedSyncs[index]].getRTT()) {
								break;
							}
						}
						if (index < USE_LOWEST_RTT_RESPONSES) {
							orderedSyncs.Insert(index, syncPair.Key);
						}
					}
				}

				// TODO: replace this average with outlier removal

				// calculate average offset
				long totalOffset = 0;
				int nrSyncs = Math.Min(orderedSyncs.Count, USE_LOWEST_RTT_RESPONSES);
				for (int i = 0; i < nrSyncs; ++i) {
					totalOffset += _syncResponses[orderedSyncs[i]].getOffset();
				}
				long averageOffset = totalOffset / nrSyncs;

				Debug.Log($"[SYNCEDTIME] Average offset: {averageOffset} (internal library calculated offset: {peer.RemoteTimeDelta / System.TimeSpan.TicksPerMillisecond})");

				// synchronized time offset is stored with NetPeer
				if (peer.Tag == null) {
					Debug.LogWarning("[SYNCEDTIME] Server instance does not have ServerMetadata instance, creating new one (device ID will be missing)");
					peer.Tag = new ServerMetadata(null);
				}
				ServerMetadata serverData = (ServerMetadata)peer.Tag;
				serverData.SetTimeOffset(averageOffset);

				_onSynced?.Invoke(peer, new MessageSynced());
			}
		}
	}
}