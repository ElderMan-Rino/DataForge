using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct AudioSettings
	{
		[Key(0)] public readonly int Id;
		[Key(1)] public readonly float MasterVolume;
		[Key(2)] public readonly float BgmVolume;
		[Key(3)] public readonly float SfxVolume;
		[Key(4)] public readonly float VoiceVolume;
		[Key(5)] public readonly float UiVolume;
		[Key(6)] public readonly bool MuteOnBackground;

		[SerializationConstructor]
		public AudioSettings(int id, float masterVolume, float bgmVolume, float sfxVolume, float voiceVolume, float uiVolume, bool muteOnBackground)
		{
			this.Id = id;
			this.MasterVolume = masterVolume;
			this.BgmVolume = bgmVolume;
			this.SfxVolume = sfxVolume;
			this.VoiceVolume = voiceVolume;
			this.UiVolume = uiVolume;
			this.MuteOnBackground = muteOnBackground;
		}
	}
}
