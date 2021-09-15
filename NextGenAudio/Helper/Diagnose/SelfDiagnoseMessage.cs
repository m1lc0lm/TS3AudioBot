// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;

namespace NextGenAudio.Helper.Diagnose
{
	public class SelfDiagnoseMessage
	{
		public string Description { get; }
		public string Category { get; }
		public string Level => LevelValue.ToString();
		[JsonIgnore]
		public SelfDiagnoseLevel LevelValue { get; }

		public SelfDiagnoseMessage(string description, string category, SelfDiagnoseLevel levelValue)
		{
			Description = description;
			Category = category;
			LevelValue = levelValue;
		}
	}
}
