using System;
using System.Collections.Generic;
using MessagePack;

namespace 
{
	[MessagePackObject]
	public readonly struct ErrorCode
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string LocaleKey;
		[Key(2)] public readonly int Id;
		[Key(3)] public readonly ErrorCategory Category;
		[Key(4)] public readonly ErrorActionType Action;

		[SerializationConstructor]
		public ErrorCode(string key, string localeKey, int id, ErrorCategory category, ErrorActionType action)
		{
			this.Key = key;
			this.LocaleKey = localeKey;
			this.Id = id;
			this.Category = category;
			this.Action = action;
		}
	}
}
