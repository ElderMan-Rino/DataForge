using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct LocaleSettingsRow
	{
		public Unity.Entities.BlobArray<LanguageType> SupportedLanguages;
		public int Id;
	}
}
