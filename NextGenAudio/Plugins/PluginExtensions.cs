// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using NLog;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NextGenAudio.Plugins
{
	public static class PluginExtensions
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Logger GetLogger()
		{
			var cls = new StackTrace()?.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
			return LogManager.GetLogger($"NextGenAudio.Plugins.{cls}");
		}
	}
}
