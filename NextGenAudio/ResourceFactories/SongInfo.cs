using System;

namespace NextGenAudio.ResourceFactories
{
	public class SongInfo
	{
		public string? Title { get; set; }
		public string? Track { get; set; }
		public string? Artist { get; set; }
		public TimeSpan? Length { get; set; }
	}
}
