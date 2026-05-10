using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct GraphicsSettingsRow
	{
		public int Id;
		public FramerateType TargetFrameRate;
		public int VsyncCount;
		public QualityLevel QualityLevel;
		public int ScreenSleepTimeout;
	}
}
