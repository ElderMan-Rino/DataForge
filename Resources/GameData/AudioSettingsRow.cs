using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
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
