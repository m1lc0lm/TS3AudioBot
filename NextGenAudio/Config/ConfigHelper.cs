// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
using System;
using System.Linq;
using System.Xml;
using NextGenAudio.CommandSystem;

namespace NextGenAudio.Config
{
	public static class ConfigHelper
	{
		public const string DefaultBotName = "default";

		public static ConfigPart[] ByPathAsArray(this ConfigPart config, string path)
		{
			try
			{
				return config.ByPath(path).ToArray();
			}
			catch (Exception ex)
			{
				throw new CommandException("Invalid TomlPath expression", ex, CommandExceptionReason.CommandError);
			}
		}

		public static E<string> TryReadValue<T>(this JsonReader reader, out T value)
		{
			if (reader.Read()
				&& (reader.TokenType == JsonToken.Boolean
				|| reader.TokenType == JsonToken.Date
				|| reader.TokenType == JsonToken.Float
				|| reader.TokenType == JsonToken.Integer
				|| reader.TokenType == JsonToken.String))
			{
				var jValue = reader.Value;
				if (jValue is null)
				{
					value = default!;
					return "Read null";
				}

				try
				{
					if (typeof(T) == typeof(TimeSpan))
					{
						if (reader.TokenType == JsonToken.String)
						{
							var timeStr = ((string)jValue).ToUpperInvariant();
							if (!timeStr.StartsWith("P"))
							{
								if (!timeStr.Contains("T"))
									timeStr = "PT" + timeStr;
								else
									timeStr = "P" + timeStr;
							}
							value = (T)(object)XmlConvert.ToTimeSpan(timeStr);
							return R.Ok;
						}
					}
					else if (typeof(T).IsEnum)
					{
						if (reader.TokenType == JsonToken.String)
						{
							value = (T)Enum.Parse(typeof(T), (string)jValue, true);
							return R.Ok;
						}
					}
					else
					{
						value = (T)Convert.ChangeType(jValue, typeof(T));
						return R.Ok;
					}
				}
				catch (Exception ex) when
					(ex is InvalidCastException
					|| ex is OverflowException
					|| ex is FormatException)
				{ }
			}
			value = default!;
			return $"Wrong type, expected {typeof(T).Name}, got {reader.TokenType}";
		}
	}
}
