using System;
using System.Collections.Generic;
using MessagePack;

namespace 
{
	[MessagePackObject]
	public readonly struct LocaleEntry
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string Value;
		[Key(2)] public readonly int Id;

		[SerializationConstructor]
		public LocaleEntry(string key, string value, int id)
		{
			this.Key = key;
			this.Value = value;
			this.Id = id;
		}
	}
}
