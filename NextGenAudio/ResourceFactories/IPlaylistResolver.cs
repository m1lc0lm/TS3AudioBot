// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System.Threading.Tasks;
using NextGenAudio.Playlists;

namespace NextGenAudio.ResourceFactories
{
	public interface IPlaylistResolver : IResolver
	{
		MatchCertainty MatchPlaylist(ResolveContext ctx, string uri);

		Task<Playlist> GetPlaylist(ResolveContext ctx, string url);
	}
}
