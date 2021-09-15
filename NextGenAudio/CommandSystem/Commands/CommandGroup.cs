// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenAudio.Localization;

namespace NextGenAudio.CommandSystem.Commands
{
	public class CommandGroup : ICommand
	{
		private readonly IDictionary<string, ICommand> commands = new Dictionary<string, ICommand>();

		public void AddCommand(string name, ICommand command) => commands.Add(name, command ?? throw new ArgumentNullException(nameof(command)));
		public bool RemoveCommand(string name) => commands.Remove(name);
		public bool RemoveCommand(ICommand command)
		{
			var com = commands.FirstOrDefault(kvp => kvp.Value == command);
			if (com.Key is null || com.Value is null)
				return false;
			return commands.Remove(com.Key);
		}
		public bool ContainsCommand(string name) => commands.ContainsKey(name);
		public ICommand? GetCommand(string name) => commands.TryGetValue(name, out var com) ? com : null;
		public bool IsEmpty => commands.Count == 0;
		public IEnumerable<KeyValuePair<string, ICommand>> Commands => commands;

		public virtual async ValueTask<object?> Execute(ExecutionInformation info, IReadOnlyList<ICommand> arguments)
		{
			string result;
			if (arguments.Count == 0)
				result = string.Empty;
			else
				result = await arguments[0].ExecuteToString(info, Array.Empty<ICommand>());

			var filter = info.GetFilter();
			var commandResults = filter.Filter(commands, result).ToArray();

			// The special case when the command is empty and only might match because of fuzzy matching.
			// We only allow this if the command explicitly allows an empty overload.
			if (string.IsNullOrEmpty(result)
				&& (commandResults.Length == 0 || commandResults.Length > 1 || (commandResults.Length == 1 && !string.IsNullOrEmpty(commandResults[0].Key))))
			{
				throw new CommandException(string.Format(strings.cmd_help_info_contains_subfunctions, SuggestionsJoinTrim(commands.Keys)), CommandExceptionReason.AmbiguousCall);
			}

			// We found too many matching commands
			if (commandResults.Length > 1)
				throw new CommandException(string.Format(strings.cmd_help_error_ambiguous_command, SuggestionsJoinTrim(commandResults.Select(g => g.Key))), CommandExceptionReason.AmbiguousCall);
			// We either found no matching command
			if (commandResults.Length == 0)
				throw new CommandException(string.Format(strings.cmd_help_info_contains_subfunctions, SuggestionsJoinTrim(commands.Keys)), CommandExceptionReason.AmbiguousCall);

			var argSubList = arguments.TrySegment(1);
			return await commandResults[0].Value.Execute(info, argSubList);
		}

		private static string SuggestionsJoinTrim(IEnumerable<string> commands)
			=> string.Join(", ", commands.Where(x => !string.IsNullOrEmpty(x)));

		public override string ToString() => "<group>";
	}
}
