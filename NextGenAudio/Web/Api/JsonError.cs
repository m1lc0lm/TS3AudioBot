// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
using NextGenAudio.CommandSystem;

namespace NextGenAudio.Web.Api
{
	public class JsonError : JsonObject
	{
		private static readonly JsonSerializerSettings ErrorSerializeSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
		};

		private readonly CommandExceptionReason reason;
		public int ErrorCode => (int)reason;
		public string ErrorName => reason.ToString();
		public string ErrorMessage { get; }
		public string? HelpMessage { get; set; }
		public string? HelpLink { get; set; }

		public JsonError(string msg, CommandExceptionReason reason)
		{
			ErrorMessage = msg;
			this.reason = reason;
		}

		public override string Serialize() => JsonConvert.SerializeObject(GetSerializeObject(), ErrorSerializeSettings);
		public override string ToString() => ErrorMessage;
	}
}
