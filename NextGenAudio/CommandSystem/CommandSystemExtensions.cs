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
using System.Threading.Tasks;
using NextGenAudio.Algorithm;
using NextGenAudio.CommandSystem.Commands;
using NextGenAudio.Dependency;

namespace NextGenAudio.CommandSystem
{
	public static class CommandSystemExtensions
	{
		public static IFilter GetFilter(this IInjector injector)
		{
			if (injector.TryGet<IFilter>(out var filter))
				return filter;
			return Filter.DefaultFilter;
		}

		public static Lazy<IFilter> GetFilterLazy(this IInjector injector)
			=> new Lazy<IFilter>(() => injector.GetFilter(), false);

		public static async ValueTask<string> ExecuteToString(this ICommand com, ExecutionInformation info, IReadOnlyList<ICommand> arguments)
		{
			var res = await com.Execute(info, arguments);
			return res?.ToString() ?? "";
		}
	}
}
