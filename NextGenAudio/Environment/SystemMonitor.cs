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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TSLib.Helper;
using TSLib.Scheduler;

namespace NextGenAudio.Environment
{
	public class SystemMonitor
	{
		private static readonly Process CurrentProcess = Process.GetCurrentProcess();
		private readonly ReaderWriterLockSlim historyLock = new ReaderWriterLockSlim();
		private readonly Queue<SystemMonitorSnapshot> history = new Queue<SystemMonitorSnapshot>();

		private bool historyChanged = true;
		private SystemMonitorReport? lastReport = null;
		private DateTime lastSnapshotTime = DateTime.MinValue;
		private TimeSpan lastCpuTime = TimeSpan.Zero;

		public DateTime StartTime { get; } = Tools.Now;

		public SystemMonitor(DedicatedTaskScheduler scheduler)
		{
			_ = scheduler.CreateTimer(CreateSnapshot, TimeSpan.FromSeconds(1), true);
		}

		public void CreateSnapshot()
		{
			CurrentProcess.Refresh();

			//TODO: foreach (ProcessThread thread in CurrentProcess.Threads)
			{
			}

			var currentSnapshotTime = Tools.Now;
			var currentCpuTime = CurrentProcess.TotalProcessorTime;

			var timeDiff = currentSnapshotTime - lastSnapshotTime;
			var cpuDiff = currentCpuTime - lastCpuTime;
			var cpu = cpuDiff.Ticks / (float)timeDiff.Ticks;

			lastSnapshotTime = currentSnapshotTime;
			lastCpuTime = currentCpuTime;

			historyLock.EnterWriteLock();
			try
			{
				history.Enqueue(new SystemMonitorSnapshot
				{
					Memory = CurrentProcess.WorkingSet64,
					Cpu = cpu,
				});

				while (history.Count > 60)
					history.Dequeue();

				historyChanged = true;
			}
			finally
			{
				historyLock.ExitWriteLock();
			}
		}

		public SystemMonitorReport GetReport()
		{
			try
			{
				historyLock.EnterReadLock();
				if (historyChanged || lastReport == null)
				{
					lastReport = new SystemMonitorReport
					(
						 memory: history.Select(x => x.Memory).ToArray(),
						 cpu: history.Select(x => x.Cpu).ToArray()
					);
					historyChanged = false;
				}
				return lastReport;
			}
			finally
			{
				historyLock.ExitReadLock();
			}
		}
	}

	public class SystemMonitorReport
	{
		public long[] Memory { get; }
		public float[] Cpu { get; }

		public SystemMonitorReport(long[] memory, float[] cpu)
		{
			Memory = memory;
			Cpu = cpu;
		}
	}

	public struct SystemMonitorSnapshot
	{
		public float Cpu { get; set; }
		public long Memory { get; set; }
	}
}
