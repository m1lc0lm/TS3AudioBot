// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using NextGenAudio.Audio;
using NextGenAudio.CommandSystem.CommandResults;
using NextGenAudio.ResourceFactories;

namespace NextGenAudio.Playlists
{
	public class PlaylistItem : IAudioResourceResult, IMetaContainer
	{
		public PlayInfo? PlayInfo { get; set; }
		public AudioResource AudioResource { get; }

		public PlaylistItem(AudioResource resource, PlayInfo? meta = null)
		{
			AudioResource = resource ?? throw new ArgumentNullException(nameof(resource));
			PlayInfo = meta;
		}

		public static PlaylistItem From(PlayResource playResource)
		{
			return new PlaylistItem(playResource.AudioResource, playResource.PlayInfo);
		}

		public override string ToString() => AudioResource.ResourceTitle ?? $"{AudioResource.AudioType}: {AudioResource.ResourceId}";
	}
}
