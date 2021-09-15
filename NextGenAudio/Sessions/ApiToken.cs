// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using TSLib.Helper;

namespace NextGenAudio.Sessions
{
	internal class ApiToken
	{
		public const int TokenLen = 32;
		public static readonly TimeSpan DefaultTokenTimeout = TimeSpan.MaxValue;

		public string Value { get; }
		public DateTime Timeout { get; }
		public bool ApiTokenActive => Tools.Now <= Timeout;

		public ApiToken(string value, DateTime timeout)
		{
			Value = value;
			Timeout = timeout;
		}
	}
}
