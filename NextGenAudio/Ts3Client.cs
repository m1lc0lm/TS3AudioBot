// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenAudio.Algorithm;
using NextGenAudio.CommandSystem;
using NextGenAudio.Config;
using NextGenAudio.Helper;
using NextGenAudio.Localization;
using TSLib;
using TSLib.Commands;
using TSLib.Full;
using TSLib.Helper;
using TSLib.Messages;
using CmdE = System.Threading.Tasks.Task<System.E<NextGenAudio.Localization.LocalStr>>;

namespace NextGenAudio
{
	public sealed class Ts3Client
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly Id id;

		public event AsyncEventHandler? OnBotConnected;
		public event AsyncEventHandler<DisconnectEventArgs>? OnBotDisconnected;
		public event AsyncEventHandler? OnBotStoppedReconnecting;
		public event AsyncEventHandler<TextMessage>? OnMessageReceived;
		public event AsyncEventHandler<AloneChanged>? OnAloneChanged;
		public event EventHandler? OnWhisperNoTarget;

		private static readonly string[] QuitMessages = {
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Requested via Web API.",
			"Gleich kommt Abdul mit Klappstuhl und Aladin mit Waschmaschin"
		};

		private bool closed = false;
		private int reconnectCounter;
		private ReconnectType? lastReconnect;

		private readonly ConfBot config;
		private readonly TsFullClient ts3FullClient;
		private IdentityData? identity;
		private List<ClientList> clientbuffer = new List<ClientList>();
		private bool clientbufferOutdated = true;
		private readonly TimedCache<ClientDbId, ClientDbInfo> clientDbNames = new TimedCache<ClientDbId, ClientDbInfo>();
		private readonly LruCache<Uid, ClientDbId> dbIdCache = new LruCache<Uid, ClientDbId>(128);
		private bool alone = true;
		private ChannelId? reconnectChannel = null;
		private ClientId[] ownChannelClients = Array.Empty<ClientId>();

		public bool Connected => ts3FullClient.Connected;
		public TsConst ServerConstants => ts3FullClient.ServerConstants;

		public Ts3Client(ConfBot config, TsFullClient ts3FullClient, Id id)
		{
			this.id = id;

			this.ts3FullClient = ts3FullClient;
			//ts3FullClient.OnEachTextMessage += ExtendedTextMessage;
			ts3FullClient.OnErrorEvent += TsFullClient_OnErrorEvent;
			ts3FullClient.OnDisconnected += TsFullClient_OnDisconnected;
			ts3FullClient.OnEachClientMoved += async (_, e) =>
			{
				UpdateReconnectChannel(e.ClientId, e.TargetChannelId);
				if (AloneRecheckRequired(e.ClientId, e.TargetChannelId)) await IsAloneRecheck();
			};
			ts3FullClient.OnEachClientEnterView += async (_, e) =>
			{
				UpdateReconnectChannel(e.ClientId, e.TargetChannelId);
				if (AloneRecheckRequired(e.ClientId, e.TargetChannelId)) await IsAloneRecheck();
				else if (AloneRecheckRequired(e.ClientId, e.SourceChannelId)) await IsAloneRecheck();
			};
			ts3FullClient.OnEachClientLeftView += async (_, e) =>
			{
				UpdateReconnectChannel(e.ClientId, e.TargetChannelId);
				if (AloneRecheckRequired(e.ClientId, e.TargetChannelId)) await IsAloneRecheck();
				else if (AloneRecheckRequired(e.ClientId, e.SourceChannelId)) await IsAloneRecheck();
			};

			this.config = config;
			identity = null;
		}

		public E<string> Connect()
		{
			// get or compute identity
			var identityConf = config.Connect.Identity;
			if (string.IsNullOrEmpty(identityConf.PrivateKey))
			{
				identity = TsCrypt.GenerateNewIdentity();
				identityConf.PrivateKey.Value = identity.PrivateKeyString;
				identityConf.Offset.Value = identity.ValidKeyOffset;
			}
			else
			{
				var identityResult = TsCrypt.LoadIdentityDynamic(identityConf.PrivateKey.Value, identityConf.Offset.Value);
				if (!identityResult.Ok)
				{
					Log.Error("The identity from the config file is corrupted. Remove it to generate a new one next start; or try to repair it.");
					return "Corrupted identity";
				}
				identity = identityResult.Value;
				identityConf.PrivateKey.Value = identity.PrivateKeyString;
				identityConf.Offset.Value = identity.ValidKeyOffset;
			}

			// check required security level
			if (identityConf.Level.Value >= 0 && identityConf.Level.Value <= 160)
				UpdateIndentityToSecurityLevel(identityConf.Level.Value);
			else if (identityConf.Level.Value != -1)
				Log.Warn("Invalid config value for 'Level', enter a number between '0' and '160' or '-1' to adapt automatically.");
			config.SaveWhenExists();

			reconnectCounter = 0;
			lastReconnect = null;
			reconnectChannel = null;
			ts3FullClient.QuitMessage = Tools.PickRandom(QuitMessages);
			ClearAllCaches();
			_ = ConnectClient();
			return R.Ok;
		}

		private async Task ConnectClient()
		{
			if (identity is null) throw new InvalidOperationException();

			if (closed)
				return;

			TsVersionSigned? versionSign;
			if (!string.IsNullOrEmpty(config.Connect.ClientVersion.Build.Value))
			{
				var versionConf = config.Connect.ClientVersion;
				versionSign = TsVersionSigned.TryParse(versionConf.Build, versionConf.Platform.Value, versionConf.Sign);

				if (versionSign is null)
				{
					Log.Warn("Invalid version sign, falling back to unknown :P");
					versionSign = TsVersionSigned.VER_WIN_3_X_X;
				}
			}
			else if (Tools.IsLinux)
			{
				versionSign = TsVersionSigned.VER_LIN_3_X_X;
			}
			else
			{
				versionSign = TsVersionSigned.VER_WIN_3_X_X;
			}

			var connectionConfig = new ConnectionDataFull(config.Connect.Address, identity,
				versionSign: versionSign,
				username: config.Connect.Name,
				serverPassword: config.Connect.ServerPassword.Get(),
				defaultChannel: reconnectChannel?.ToPath() ?? config.Connect.Channel,
				defaultChannelPassword: config.Connect.ChannelPassword.Get(),
				logId: id);

			config.SaveWhenExists().UnwrapToLog(Log);

			if (!(await ts3FullClient.Connect(connectionConfig)).GetOk(out var error))
			{
				Log.Error("Could not connect: {0}", error.ErrorFormat());
				return;
			}

			Log.Info("Client connected.");
			reconnectCounter = 0;
			lastReconnect = null;

			await OnBotConnected.InvokeAsync(this);
		}

		public async Task Disconnect()
		{
			closed = true;
			await ts3FullClient.Disconnect();
			ts3FullClient.Dispose();
		}

		private void UpdateIndentityToSecurityLevel(int targetLevel)
		{
			if (identity is null) throw new InvalidOperationException();
			if (TsCrypt.GetSecurityLevel(identity) < targetLevel)
			{
				Log.Info("Calculating up to required security level: {0}", targetLevel);
				TsCrypt.ImproveSecurity(identity, targetLevel);
				config.Connect.Identity.Offset.Value = identity.ValidKeyOffset;
			}
		}

		#region TSLib functions wrapper

		public Task SendMessage(string message, ClientId clientId) => ts3FullClient.SendPrivateMessage(message, clientId).UnwrapThrow();
		public Task SendChannelMessage(string message) => ts3FullClient.SendChannelMessage(message).UnwrapThrow();
		public Task SendServerMessage(string message) => ts3FullClient.SendServerMessage(message, 1).UnwrapThrow();

		public Task KickClientFromServer(params ClientId[] clientId) => ts3FullClient.KickClientFromServer(clientId).UnwrapThrow();
		public Task KickClientFromChannel(params ClientId[] clientId) => ts3FullClient.KickClientFromChannel(clientId).UnwrapThrow();

		public Task ChangeDescription(string description)
			=> ts3FullClient.ChangeDescription(description).UnwrapThrow();

		public Task ChangeBadges(string badgesString)
		{
			if (!badgesString.StartsWith("overwolf=") && !badgesString.StartsWith("badges="))
				badgesString = "overwolf=0:badges=" + badgesString;
			return ts3FullClient.ChangeBadges(badgesString).UnwrapThrow();
		}

		public Task ChangeName(string name)
			=> ts3FullClient.ChangeName(name).UnwrapThrow(e =>
				(e == TsErrorCode.parameter_invalid_size ? strings.error_ts_invalid_name : null, false)
			);

		public Task<ClientList> GetCachedClientById(ClientId id) => ClientBufferRequest(client => client.ClientId == id);

		public async Task<ClientList> GetFallbackedClientById(ClientId id)
		{
			try { return await ClientBufferRequest(client => client.ClientId == id); }
			catch (AudioBotException) { }
			Log.Warn("Slow double request due to missing or wrong permission configuration!");
			ClientList clientInfo = await ts3FullClient.Send<ClientList>("clientinfo", new CommandParameter("clid", id))
				.MapToSingle()
				.UnwrapThrow(_ => (strings.error_ts_no_client_found, true));
			clientInfo.ClientId = id;
			clientbuffer.Add(clientInfo);
			return clientInfo;
		}

		public async Task<ClientList> GetClientByName(string name)
		{
			await RefreshClientBuffer(false);
			var client = Filter.DefaultFilter.Filter(
				clientbuffer.Select(cb => new KeyValuePair<string, ClientList>(cb.Name, cb)), name).FirstOrDefault().Value;
			if (client is null)
				throw new CommandException(strings.error_ts_no_client_found);
			return client;
		}

		private async Task<ClientList> ClientBufferRequest(Predicate<ClientList> pred)
		{
			await RefreshClientBuffer(false);
			var clientData = clientbuffer.Find(pred);
			if (clientData is null)
				throw new CommandException(strings.error_ts_no_client_found);
			return clientData;
		}

		public async ValueTask RefreshClientBuffer(bool force)
		{
			if (clientbufferOutdated || force)
			{
				var result = await ts3FullClient.ClientList(ClientListOptions.uid);
				if (!result)
				{
					Log.Debug("Clientlist failed ({0})", result.Error.ErrorFormat());
					throw new TeamSpeakErrorCommandException(result.Error.FormatLocal().Str, result.Error);
				}
				clientbuffer = result.Value.ToList();
				clientbufferOutdated = false;
			}
		}

		public async Task<ServerGroupId[]> GetClientServerGroups(ClientDbId dbId)
		{
			var result = await ts3FullClient.ServerGroupsByClientDbId(dbId).UnwrapThrow(_ => (strings.error_ts_no_client_found, true));
			return result.Select(csg => csg.ServerGroupId).ToArray();
		}

		public async Task<ClientDbInfo> GetDbClientByDbId(ClientDbId clientDbId)
		{
			if (clientDbNames.TryGetValue(clientDbId, out var clientData))
				return clientData;

			clientData = await ts3FullClient.ClientDbInfo(clientDbId).UnwrapThrow(_ => (strings.error_ts_no_client_found, true));
			clientDbNames.Set(clientDbId, clientData);
			return clientData;
		}

		public Task<ClientInfo> GetClientInfoById(ClientId id) => ts3FullClient.ClientInfo(id).UnwrapThrow(_ => (strings.error_ts_no_client_found, true));

		public async Task<ClientDbId> GetClientDbIdByUid(Uid uid)
		{
			if (dbIdCache.TryGetValue(uid, out var dbid))
				return dbid;

			var client = await ts3FullClient.GetClientDbIdFromUid(uid).UnwrapThrow(_ => (strings.error_ts_no_client_found, true));

			dbIdCache.Set(client.ClientUid, client.ClientDbId);
			return client.ClientDbId;
		}

		public async Task SetupRights(string? key)
		{
			var self = ts3FullClient.Book.Self();
			if (self is null)
			{
				Log.Error("Getting self failed");
				throw new CommandException(strings.cmd_bot_setup_error);
			}
			var myDbId = self.DatabaseId;

			// Check all own server groups
			ServerGroupId[] groups;
			bool groupsOk;
			try { groups = await GetClientServerGroups(myDbId); groupsOk = true; }
			catch { groups = Array.Empty<ServerGroupId>(); groupsOk = false; }

			// Add self to master group (via token)
			if (!string.IsNullOrEmpty(key))
			{
				var privKeyUseResult = await ts3FullClient.PrivilegeKeyUse(key);
				if (!privKeyUseResult.Ok)
				{
					Log.Error("Using privilege key failed ({0})", privKeyUseResult.Error.ErrorFormat());
					throw new CommandException(strings.cmd_bot_setup_error);
				}
			}

			// Remember new group (or check if in new group at all)
			var groupDiff = Array.Empty<ServerGroupId>();
			if (groupsOk)
			{
				ServerGroupId[] groupsNew;
				try
				{
					groupsNew = await GetClientServerGroups(myDbId);
					groupDiff = groupsNew.Except(groups).ToArray();
				}
				catch { }
			}

			if (config.BotGroupId == 0)
			{
				// Create new Bot group
				var botGroup = await ts3FullClient.ServerGroupAdd("ServerBot");
				if (botGroup.Ok)
				{
					config.BotGroupId.Value = botGroup.Value.ServerGroupId.Value;

					// Add self to new group
					var grpresult = await ts3FullClient.ServerGroupAddClient(botGroup.Value.ServerGroupId, myDbId);
					if (!grpresult.Ok)
						Log.Error("Adding group failed ({0})", grpresult.Error.ErrorFormat());
				}
			}

			const int max = 75;
			const int ava = 500000; // max size in bytes for the avatar

			// Add various rights to the bot group
			var permresult = await ts3FullClient.ServerGroupAddPerm((ServerGroupId)config.BotGroupId.Value,
				new[] {
					TsPermission.i_client_whisper_power, // + Required for whisper channel playing
					TsPermission.i_client_private_textmessage_power, // + Communication
					TsPermission.b_client_server_textmessage_send, // + Communication
					TsPermission.b_client_channel_textmessage_send, // + Communication

					TsPermission.b_client_modify_dbproperties, // ? Dont know but seems also required for the next one
					TsPermission.b_client_modify_description, // + Used to change the description of our bot
					TsPermission.b_client_info_view, // (+) only used as fallback usually
					TsPermission.b_virtualserver_client_list, // ? Dont know but seems also required for the next one

					TsPermission.i_channel_subscribe_power, // + Required to find user to communicate
					TsPermission.b_virtualserver_client_dbinfo, // + Required to get basic user information for history, api, etc...
					TsPermission.i_client_talk_power, // + Required for normal channel playing
					TsPermission.b_client_modify_own_description, // ? not sure if this makes b_client_modify_description superfluous

					TsPermission.b_group_is_permanent, // + Group should stay even if bot disconnects
					TsPermission.i_client_kick_from_channel_power, // + Optional for kicking
					TsPermission.i_client_kick_from_server_power, // + Optional for kicking
					TsPermission.i_client_max_clones_uid, // + In case that bot times out and tries to join again

					TsPermission.b_client_ignore_antiflood, // + The bot should be resistent to forced spam attacks
					TsPermission.b_channel_join_ignore_password, // + The noble bot will not abuse this power
					TsPermission.b_channel_join_permanent, // + Allow joining to all channel even on strict servers
					TsPermission.b_channel_join_semi_permanent, // + Allow joining to all channel even on strict servers

					TsPermission.b_channel_join_temporary, // + Allow joining to all channel even on strict servers
					TsPermission.b_channel_join_ignore_maxclients, // + Allow joining full channels
					TsPermission.i_channel_join_power, // + Allow joining to all channel even on strict servers
					TsPermission.b_client_permissionoverview_view, // + Scanning through given perms for rights system

					TsPermission.i_client_max_avatar_filesize, // + Uploading thumbnails as avatar
					TsPermission.b_client_use_channel_commander, // + Enable channel commander
					TsPermission.b_client_ignore_bans, // + The bot should be resistent to bans
					TsPermission.b_client_ignore_sticky, // + Should skip weird movement restrictions

					TsPermission.i_client_max_channel_subscriptions, // + Required to find user to communicate
				},
				new[] {
					max, max,   1,   1,
					  1,   1,   1,   1,
					max,   1, max,   1,
					  1, max, max,   4,
					  1,   1,   1,   1,
					  1,   1, max,   1,
					ava,   1,   1,   1,
					 -1,
				},
				new[] {
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false,
				},
				new[] {
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false, false, false, false,
					false,
				});

			if (!permresult)
				Log.Error("Adding permissions failed ({0})", permresult.Error.ErrorFormat());

			// Leave master group again
			if (groupDiff.Length > 0)
			{
				foreach (var grp in groupDiff)
				{
					var grpresult = await ts3FullClient.ServerGroupDelClient(grp, myDbId);
					if (!grpresult.Ok)
						Log.Error("Removing group failed ({0})", grpresult.Error.ErrorFormat());
				}
			}
		}

		public Task UploadAvatar(System.IO.Stream stream)
			=> ts3FullClient.UploadAvatar(stream).UnwrapThrow(e =>
				(e == TsErrorCode.permission_invalid_size ? strings.error_ts_file_too_big : null, false)
			);

		public Task DeleteAvatar() => ts3FullClient.DeleteAvatar().UnwrapThrow();

		public Task MoveTo(ChannelId channelId, string? password = null)
			=> ts3FullClient.ClientMove(ts3FullClient.ClientId, channelId, password).UnwrapThrow(_ => (strings.error_ts_cannot_move, true));

		public Task SetChannelCommander(bool isCommander)
			=> ts3FullClient.ChangeIsChannelCommander(isCommander).UnwrapThrow(_ => (strings.error_ts_cannot_set_commander, true));

		public async Task<bool> IsChannelCommander()
			=> (await GetClientInfoById(ts3FullClient.ClientId)).IsChannelCommander;

		public void InvalidateClientBuffer() => clientbufferOutdated = true;

		private void ClearAllCaches()
		{
			InvalidateClientBuffer();
			dbIdCache.Clear();
			clientDbNames.Clear();
			alone = true;
			ownChannelClients = Array.Empty<ClientId>();
		}

		#endregion

		#region Events

		private void TsFullClient_OnErrorEvent(object? sender, CommandError error)
		{
			switch (error.Id)
			{
			case TsErrorCode.whisper_no_targets:
				OnWhisperNoTarget?.Invoke(this, EventArgs.Empty);
				break;

			default:
				Log.Debug("Got ts3 error event: {0}", error.ErrorFormat());
				break;
			}
		}

		private async void TsFullClient_OnDisconnected(object? sender, DisconnectEventArgs e)
		{
			await OnBotDisconnected.InvokeAsync(this, e);

			if (e.Error != null)
			{
				var error = e.Error;
				switch (error.Id)
				{
				case TsErrorCode.client_could_not_validate_identity:
					if (config.Connect.Identity.Level.Value == -1 && !string.IsNullOrEmpty(error.ExtraMessage))
					{
						int targetSecLevel = int.Parse(error.ExtraMessage);
						UpdateIndentityToSecurityLevel(targetSecLevel); // TODO Async
						await ConnectClient();
						return; // skip triggering event, we want to reconnect
					}
					else
					{
						Log.Warn("The server reported that the security level you set is not high enough." +
							"Increase the value to '{0}' or set it to '-1' to generate it on demand when connecting.", error.ExtraMessage);
					}
					break;

				case TsErrorCode.client_too_many_clones_connected:
					Log.Warn("Another client with the same identity is already connected.");
					if (await TryReconnect(ReconnectType.Error))
						return;
					break;

				case TsErrorCode.connect_failed_banned:
					Log.Warn("This bot is banned.");
					if (await TryReconnect(ReconnectType.Ban))
						return;
					break;

				default:
					Log.Warn("Could not connect: {0}", error.ErrorFormat());
					if (await TryReconnect(ReconnectType.Error))
						return;
					break;
				}
			}
			else
			{
				Log.Debug("Bot disconnected. Reason: {0}", e.ExitReason);

				if (await TryReconnect(e.ExitReason switch
				{
					Reason.Timeout => ReconnectType.Timeout,
					Reason.SocketError => ReconnectType.Timeout,
					Reason.KickedFromServer => ReconnectType.Kick,
					Reason.ServerShutdown => ReconnectType.ServerShutdown,
					Reason.ServerStopped => ReconnectType.ServerShutdown,
					Reason.Banned => ReconnectType.Ban,
					_ => ReconnectType.None
				})) return;
			}

			await OnBotStoppedReconnecting.InvokeAsync(this);
		}

		private async Task<bool> TryReconnect(ReconnectType type)
		{
			if (closed)
				return false;

			// Check if we want to keep the last disconnect type
			if (type == ReconnectType.Timeout && lastReconnect == ReconnectType.ServerShutdown)
			{
				type = lastReconnect.Value;
			}
			else
			{
				if (lastReconnect != type)
					reconnectCounter = 0;
				lastReconnect = type;
			}

			TimeSpan? delay;
			switch (type)
			{
			case ReconnectType.Timeout: delay = config.Reconnect.OnTimeout.GetValueAsTime(reconnectCounter); break;
			case ReconnectType.Kick: delay = config.Reconnect.OnKick.GetValueAsTime(reconnectCounter); break;
			case ReconnectType.Ban: delay = config.Reconnect.OnBan.GetValueAsTime(reconnectCounter); break;
			case ReconnectType.ServerShutdown: delay = config.Reconnect.OnShutdown.GetValueAsTime(reconnectCounter); break;
			case ReconnectType.Error: delay = config.Reconnect.OnError.GetValueAsTime(reconnectCounter); break;
			case ReconnectType.None:
				return false;
			default: throw Tools.UnhandledDefault(type);
			}
			reconnectCounter++;

			if (delay == null)
			{
				Log.Info("Reconnect strategy for '{0}' has reached the end. Closing instance.", type);
				return false;
			}

			Log.Info("Trying to reconnect because of {0}. Delaying reconnect for {1:0} seconds", type, delay.Value.TotalSeconds);
			await Task.Delay(delay.Value); // TODO: Async add cancellation token ?
			await ConnectClient();
			return true;
		}

		private async void ExtendedTextMessage(object? sender, TextMessage textMessage)
		{
			// Prevent loopback of own textmessages
			if (textMessage.InvokerId == ts3FullClient.ClientId)
				return;
			await OnMessageReceived.InvokeAsync(sender, textMessage);
		}

		private void UpdateReconnectChannel(ClientId clientId, ChannelId channelId)
		{
			if (clientId == ts3FullClient.ClientId && channelId != ChannelId.Null)
				reconnectChannel = channelId;
		}

		private bool AloneRecheckRequired(ClientId clientId, ChannelId channelId)
			=> ownChannelClients.Contains(clientId) || channelId == ts3FullClient.Book.Self()?.Channel;

		private async ValueTask IsAloneRecheck()
		{
			var self = ts3FullClient.Book.Self();
			if (self is null)
				return;
			var ownChannel = self.Channel;
			ownChannelClients = ts3FullClient.Book.Clients.Values.Where(c => c.Channel == ownChannel && c != self).Select(c => c.Id).ToArray();
			var newAlone = ownChannelClients.Length == 0;
			if (newAlone != alone)
			{
				alone = newAlone;
				await OnAloneChanged.InvokeAsync(this, new AloneChanged(newAlone));
			}
		}

		#endregion

		private enum ReconnectType
		{
			None,
			Timeout,
			Kick,
			Ban,
			ServerShutdown,
			Error
		}
	}

	public class AloneChanged : EventArgs
	{
		public bool Alone { get; }

		public AloneChanged(bool alone)
		{
			Alone = alone;
		}
	}

	internal static class CommandErrorExtentions
	{
		public static async Task<T> UnwrapThrow<T>(this Task<R<T, CommandError>> task, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null) where T : notnull
		{
			var result = await task;
			if (result.Ok)
				return result.Value;
			else
				throw new TeamSpeakErrorCommandException(result.Error.FormatLocal(prefix).Str, result.Error);
		}

		public static async Task UnwrapThrow(this Task<E<CommandError>> task, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null)
		{
			var result = await task;
			if (!result.Ok)
				throw new TeamSpeakErrorCommandException(result.Error.FormatLocal(prefix).Str, result.Error);
		}

		public static async Task<R<T, LocalStr>> FormatLocal<T>(this Task<R<T, CommandError>> task, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null) where T : notnull
			=> (await task).FormatLocal(prefix);

		public static R<T, LocalStr> FormatLocal<T>(this R<T, CommandError> cmdErr, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null) where T : notnull
		{
			if (cmdErr.Ok)
				return cmdErr.Value;
			return cmdErr.Error.FormatLocal(prefix);
		}

		public static async CmdE FormatLocal(this Task<E<CommandError>> task, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null)
			=> (await task).FormatLocal(prefix);

		public static E<LocalStr> FormatLocal(this E<CommandError> cmdErr, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null)
		{
			if (cmdErr.Ok)
				return R.Ok;
			return cmdErr.Error.FormatLocal(prefix);
		}

		public static LocalStr FormatLocal(this CommandError err, Func<TsErrorCode, (string? loc, bool msg)>? prefix = null)
		{
			var strb = new StringBuilder();
			bool msg = true;

			if (prefix != null)
			{
				string? prefixStr;
				(prefixStr, msg) = prefix(err.Id);
				if (prefixStr != null)
				{
					strb.Append(prefixStr);
				}
			}

			if (strb.Length == 0)
			{
				strb.Append(strings.error_ts_unknown_error);
			}

			if (msg)
			{
				if (strb.Length > 0)
					strb.Append(" (");
				var localStr = LocalizationManager.GetString("error_ts_code_" + (uint)err.Id);
				if (localStr != null)
					strb.Append(localStr);
				else
					strb.Append(err.Message);
				strb.Append(')');
			}

			if (err.MissingPermissionId != TsPermission.undefined)
				strb.Append(" (").Append(err.MissingPermissionId).Append(')');

			return new LocalStr(strb.ToString());
		}
	}
}
