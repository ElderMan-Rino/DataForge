using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct BootstrapData
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string DataKey;
		[Key(2)] public readonly int Id;

		[SerializationConstructor]
		public BootstrapData(string key, string dataKey, int id)
		{
			this.Key = key;
			this.DataKey = dataKey;
			this.Id = id;
		}
	}
}
