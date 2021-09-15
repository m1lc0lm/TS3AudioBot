// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;

namespace NextGenAudio.Dependency
{
	/// <summary>
	/// This provides the base contract for 'injector' classes.
	/// An injector is basically a dictionary to look up objects by type.
	/// </summary>
	public interface IInjector
	{
		object? GetModule(Type type);
		void AddModule(Type type, object obj);
	}
}
