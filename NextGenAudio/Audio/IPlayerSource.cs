// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Threading.Tasks;
using TSLib.Audio;

namespace NextGenAudio.Audio
{
	public interface IPlayerSource : IAudioPassiveProducer
	{
		event EventHandler OnSongEnd;
		event EventHandler<SongInfoChanged> OnSongUpdated;

		TimeSpan? Length { get; }
		TimeSpan? Position { get; }

		Task Seek(TimeSpan position);
	}
}
