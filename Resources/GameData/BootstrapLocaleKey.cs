using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct BootstrapLocaleKey
	{
		[Key(0)] public readonly string SheetName;
		[Key(1)] public readonly int Id;
		[Key(2)] public readonly LanguageType LocaleType;

		[SerializationConstructor]
		public BootstrapLocaleKey(string sheetName, int id, LanguageType localeType)
		{
			this.SheetName = sheetName;
			this.Id = id;
			this.LocaleType = localeType;
		}
	}
}
