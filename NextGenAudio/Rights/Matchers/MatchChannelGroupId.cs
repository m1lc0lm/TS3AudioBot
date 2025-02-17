// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System.Collections.Generic;
using TSLib;

namespace NextGenAudio.Rights.Matchers
{
	internal class MatchChannelGroupId : Matcher
	{
		private readonly HashSet<ChannelGroupId> channelGroupIds;

		public MatchChannelGroupId(IEnumerable<ChannelGroupId> channelGroupIds) => this.channelGroupIds = new HashSet<ChannelGroupId>(channelGroupIds);

		public override bool Matches(ExecuteContext ctx) => ctx.ChannelGroupId != null && channelGroupIds.Contains(ctx.ChannelGroupId.Value);
	}
}
