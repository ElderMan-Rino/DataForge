using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct SplashEntryInfo
	{
		[Key(0)] public readonly string SpriteName;
		[Key(1)] public readonly int Id;
		[Key(2)] public readonly float Interval;

		[SerializationConstructor]
		public SplashEntryInfo(string spriteName, int id, float interval)
		{
			this.SpriteName = spriteName;
			this.Id = id;
			this.Interval = interval;
		}
	}
}
