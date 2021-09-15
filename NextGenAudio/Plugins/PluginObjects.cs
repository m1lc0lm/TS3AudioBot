// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using NextGenAudio.CommandSystem;

namespace NextGenAudio.Plugins
{
	internal class PluginObjects
	{
		public PluginCommandBag Bag { get; set; }
		public ITabPlugin Plugin { get; set; }
		public CommandManager CommandManager { get; set; }

		public PluginObjects(ITabPlugin plugin, PluginCommandBag bag, CommandManager commandManager)
		{
			Bag = bag;
			Plugin = plugin;
			CommandManager = commandManager;
		}
	}
}
