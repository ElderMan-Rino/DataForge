using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct LocaleSettings
	{
		[Key(0)] public readonly List<LanguageType> SupportedLanguages;
		[Key(1)] public readonly int Id;

		[SerializationConstructor]
		public LocaleSettings(List<LanguageType> supportedLanguages, int id)
		{
			this.SupportedLanguages = supportedLanguages;
			this.Id = id;
		}
	}
}
