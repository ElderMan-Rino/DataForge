using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct GraphicsSettingsRow
	{
		public int Id;
		public FramerateType TargetFrameRate;
		public int VsyncCount;
		public QualityLevel QualityLevel;
		public int ScreenSleepTimeout;
	}
}
