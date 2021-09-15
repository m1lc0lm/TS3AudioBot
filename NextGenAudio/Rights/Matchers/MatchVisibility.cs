// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System.Linq;
using TSLib;

namespace NextGenAudio.Rights.Matchers
{
	internal class MatchVisibility : Matcher
	{
		private readonly TextMessageTargetMode[] visibility;

		public MatchVisibility(TextMessageTargetMode[] visibility) => this.visibility = visibility;

		public override bool Matches(ExecuteContext ctx) => ctx.Visibiliy != null && visibility.Contains(ctx.Visibiliy.Value);
	}
}
