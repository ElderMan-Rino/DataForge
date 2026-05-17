using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct LocaleSettingsRow
	{
		public Unity.Entities.BlobArray<LanguageType> SupportedLanguages;
		public int Id;
	}
}
