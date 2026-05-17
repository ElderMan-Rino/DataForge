using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct AudioSettingsRow
	{
		public int Id;
		public float MasterVolume;
		public float BgmVolume;
		public float SfxVolume;
		public float VoiceVolume;
		public float UiVolume;
		public bool MuteOnBackground;
	}
}
