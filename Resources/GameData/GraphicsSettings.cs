using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct GraphicsSettings
	{
		[Key(0)] public readonly int Id;
		[Key(1)] public readonly FramerateType TargetFrameRate;
		[Key(2)] public readonly int VsyncCount;
		[Key(3)] public readonly QualityLevel QualityLevel;
		[Key(4)] public readonly int ScreenSleepTimeout;

		[SerializationConstructor]
		public GraphicsSettings(int id, FramerateType targetFrameRate, int vsyncCount, QualityLevel qualityLevel, int screenSleepTimeout)
		{
			this.Id = id;
			this.TargetFrameRate = targetFrameRate;
			this.VsyncCount = vsyncCount;
			this.QualityLevel = qualityLevel;
			this.ScreenSleepTimeout = screenSleepTimeout;
		}
	}
}
