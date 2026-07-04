using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootstrapLocaleKeyRow
	{
		public BlobString SheetName;
		public int Id;
		public LanguageType LocaleType;
	}
}
