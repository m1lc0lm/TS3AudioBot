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
using NextGenAudio.CommandSystem.CommandResults;
using TSLib;

namespace NextGenAudio.CommandSystem
{
	public static class CommandSystemTypes
	{
		/// <summary>
		/// The order of types, the first item has the highest priority,
		/// items not in the list have higher priority as they are special types.
		/// </summary>
		public static readonly Type[] TypeOrder = {
			typeof(bool),
			typeof(sbyte), typeof(byte),
			typeof(short), typeof(ushort),
			typeof(int), typeof(uint),
			typeof(long), typeof(ulong),
			typeof(float), typeof(double),
			typeof(TimeSpan), typeof(DateTime),
			typeof(string) };
		public static readonly HashSet<Type> BasicTypes = new HashSet<Type>(TypeOrder);

		public static readonly HashSet<Type> AdvancedTypes = new HashSet<Type>(new Type[] {
			typeof(IAudioResourceResult),
			typeof(System.Collections.IEnumerable),
			typeof(ResourceFactories.AudioResource),
			typeof(History.AudioLogEntry),
			typeof(Playlists.PlaylistItem),
		}.Concat(TsTypes.All));
	}
}
