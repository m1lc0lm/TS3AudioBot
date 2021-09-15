// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.IO;
using System.Text.RegularExpressions;
using NextGenAudio.Helper;

namespace NextGenAudio.Config
{
	/// <summary>
	/// Upgrades the /bots/ folder structure from each Bot being a 'bot_(name).toml'
	/// file to each bot having its own folder with '/(name)/bot.toml'.
	/// </summary>
	internal static class ConfigUpgrade2
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private static readonly Regex BotFileMatcher = new Regex(@"^bot_(.+)\.toml$", Util.DefaultRegexConfig);
		private const string NewBotConfigFileName = "bot.toml";

		public static void Upgrade(string path)
		{
			string[] files;
			try
			{
				files = Directory.GetFiles(path);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to get 'Bots' directory. Your bots might not be available. Refer to our GitHub for upgrade actions.");
				return;
			}

			foreach (var file in files)
			{
				try
				{
					var fi = new FileInfo(file);
					var match = BotFileMatcher.Match(fi.Name);
					if (match.Success)
					{
						var name = match.Groups[1].Value;
						Directory.CreateDirectory(Path.Combine(path, name));
						fi.MoveTo(Path.Combine(path, name, NewBotConfigFileName));
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to move Bot '{0}' to the new folder structure.", file);
				}
			}
		}
	}
}
