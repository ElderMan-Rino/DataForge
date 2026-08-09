using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct BootstrapLocaleKeyRow
	{
		public BlobString SheetName;
		public int Id;
		public LanguageType LocaleType;
	}
}
