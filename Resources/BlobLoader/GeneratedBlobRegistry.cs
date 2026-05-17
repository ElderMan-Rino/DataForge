using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public static class GeneratedBlobRegistry
	{
		public static readonly Dictionary<string, Func<IDataSheetLoader, UniTask>> Registry = new()
		{
			[SheetKey.SceneInfo] = static l => l.LoadSheetAsync<SceneInfoRoot>(SheetKey.SceneInfo),
			[SheetKey.ErrorCode] = static l => l.LoadSheetAsync<ErrorCodeRoot>(SheetKey.ErrorCode),
			[SheetKey.BootstrapData] = static l => l.LoadSheetAsync<BootstrapDataRoot>(SheetKey.BootstrapData),
			[SheetKey.AudioSettings] = static l => l.LoadSheetAsync<AudioSettingsRoot>(SheetKey.AudioSettings),
			[SheetKey.GraphicsSettings] = static l => l.LoadSheetAsync<GraphicsSettingsRoot>(SheetKey.GraphicsSettings),
			[SheetKey.LocaleSettings] = static l => l.LoadSheetAsync<LocaleSettingsRoot>(SheetKey.LocaleSettings),
			[SheetKey.BootstrapLocale_Ko] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Ko),
			[SheetKey.BootstrapLocale_Jp] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Jp),
			[SheetKey.BootstrapLocale_En] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_En),
			[SheetKey.ErrorMsgLocale_Ko] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Ko),
			[SheetKey.ErrorMsgLocale_Jp] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Jp),
			[SheetKey.ErrorMsgLocale_En] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_En),
		};
	}
}
