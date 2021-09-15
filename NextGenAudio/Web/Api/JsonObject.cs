// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
using NextGenAudio.CommandSystem.CommandResults;
using NextGenAudio.Helper;

namespace NextGenAudio.Web.Api
{
	public abstract class JsonObject : IWrappedResult
	{
		private static readonly JsonSerializerSettings DefaultSettigs = new JsonSerializerSettings();

		static JsonObject()
		{
			DefaultSettigs.Converters.Add(new IJsonSerializableConverter());
			DefaultSettigs.Converters.Add(new TimeSpanConverter());
		}

		protected JsonObject() { }

		object? IWrappedResult.Content => GetSerializeObject();
		public virtual object GetSerializeObject() => this;
		public virtual string Serialize() => JsonConvert.SerializeObject(GetSerializeObject(), DefaultSettigs);
		public override abstract string ToString();
	}
}
