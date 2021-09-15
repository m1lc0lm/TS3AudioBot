// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using NextGenAudio.Audio;
using NextGenAudio.CommandSystem.CommandResults;

namespace NextGenAudio.ResourceFactories
{
	public class PlayResource : IAudioResourceResult, IMetaContainer
	{
		public AudioResource AudioResource { get; }
		public string PlayUri { get; }
		public PlayInfo? PlayInfo { get; set; }
		public SongInfo? SongInfo { get; set; }

		public PlayResource(string uri, AudioResource baseData, PlayInfo? playInfo = null, SongInfo? songInfo = null)
		{
			AudioResource = baseData;
			PlayUri = uri;
			PlayInfo = playInfo;
			SongInfo = songInfo;
		}

		public override string ToString() => AudioResource.ToString();
	}
}
