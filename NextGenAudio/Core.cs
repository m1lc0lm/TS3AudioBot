// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using NLog;
using System;
using System.Threading.Tasks;
using NextGenAudio.CommandSystem;
using NextGenAudio.Config;
using NextGenAudio.Dependency;
using NextGenAudio.Environment;
using NextGenAudio.Helper;
using NextGenAudio.Plugins;
using NextGenAudio.ResourceFactories;
using NextGenAudio.Rights;
using NextGenAudio.Sessions;
using NextGenAudio.Web;
using TSLib.Scheduler;
using System.IO;

namespace NextGenAudio
{
	public sealed class Core
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		private readonly string configFilePath;
		private bool forceNextExit;
		private readonly DedicatedTaskScheduler scheduler;
		private readonly CoreInjector injector;

		public Core(DedicatedTaskScheduler scheduler, string? configFilePath = null)
		{
			this.scheduler = scheduler;
			// setting defaults
			this.configFilePath = configFilePath ?? FilesConst.CoreConfig;

			injector = new CoreInjector();
		}

		public async Task Run(ParameterData setup)
		{
			scheduler.VerifyOwnThread();

			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;
			Console.CancelKeyPress += ConsoleInterruptHandler;

			var config = ConfRoot.OpenOrCreate(configFilePath);
			if (config is null)
				throw new Exception("Could not create config");
			ConfigUpgrade2.Upgrade(config.Configs.BotsPath.Value);
			config.Save();

			var builder = new DependencyBuilder(injector);

			injector.AddModule(this);
			injector.AddModule(scheduler);
			injector.AddModule(injector);
			injector.AddModule(config);
			injector.AddModule(config.Db);
			injector.AddModule(config.Plugins);
			injector.AddModule(config.Web);
			injector.AddModule(config.Web.Interface);
			injector.AddModule(config.Web.Api);
			injector.AddModule(config.Rights);
			injector.AddModule(config.Factories);
			builder.RequestModule<SystemMonitor>();
			builder.RequestModule<DbStore>();
			builder.RequestModule<PluginManager>();
			builder.RequestModule<WebServer>();
			builder.RequestModule<RightsManager>();
			builder.RequestModule<BotManager>();
			builder.RequestModule<TokenManager>();
			builder.RequestModule<CommandManager>();
			builder.RequestModule<ResourceResolver>();
			builder.RequestModule<Stats>();

			if (!builder.Build())
				throw new Exception("Could not load all core modules");

			Upgrader.PerformUpgrades(injector);
			YoutubeDlHelper.DataObj = config.Tools.YoutubeDl;

			injector.GetModuleOrThrow<CommandManager>().RegisterCollection(MainCommands.Bag);
			if (!File.Exists("token.txt"))
			{
				string token = injector.GetModuleOrThrow<TokenManager>().GenerateToken("R+Mzqu6V4bye6MSZ1r8xhPq/AyY=");
				using (StreamWriter sw = File.CreateText("token.txt"))
				{
					sw.WriteLine($"Token: {token}");
					Console.WriteLine(sw.ToString());
				}
			}
			injector.GetModuleOrThrow<RightsManager>().CreateConfigIfNotExists(setup.Interactive);
			injector.GetModuleOrThrow<WebServer>().StartWebServer();
			injector.GetModuleOrThrow<Stats>().StartTimer(setup.SendStats);


			await injector.GetModuleOrThrow<BotManager>().RunBots(setup.Interactive);
		}

		public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.ExceptionObject as Exception, "Critical program failure!");
			StopAsync().RunSynchronously();
		}

		public static void UnobservedTaskExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
		{
			Log.Error(e.Exception, "Unobserved Task error!");
		}

		public void ConsoleInterruptHandler(object sender, ConsoleCancelEventArgs e)
		{
			if (e.SpecialKey == ConsoleSpecialKey.ControlC)
			{
				if (!forceNextExit)
				{
					Log.Info("Got interrupt signal, trying to soft-exit.");
					e.Cancel = true;
					forceNextExit = true;
					Stop();
				}
				else
				{
					Log.Info("Got multiple interrupt signals, trying to force-exit.");
					System.Environment.Exit(0);
				}
			}
		}

		public void Stop() => _ = scheduler.InvokeAsync(StopAsync);

		private async Task StopAsync()
		{
			Log.Info("NextGenAudio shutting down.");

			var botManager = injector.GetModule<BotManager>();
			if (botManager != null)
				await botManager.StopBots();
			injector.GetModule<PluginManager>()?.Dispose();
			injector.GetModule<WebServer>()?.Dispose();
			injector.GetModule<DbStore>()?.Dispose();
			injector.GetModule<ResourceResolver>()?.Dispose();
			injector.GetModule<DedicatedTaskScheduler>()?.Dispose();

			Log.Info("Bye");
		}
	}
}
