using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public static class GeneratedBlobRegistry
	{
		// [HEAP] 초기화 시 1회 할당
		public static readonly Dictionary<int, Func<IDataSheetLoader, int, UniTask>> Registry = new()
		{
			[SheetKey.BootstrapLocaleKeyHash] = static (l, scope) => l.LoadSheetAsync<BootstrapLocaleKeyRoot>(SheetKey.BootstrapLocaleKey, scope),
			[SheetKey.AudioSettingsHash] = static (l, scope) => l.LoadSheetAsync<AudioSettingsRoot>(SheetKey.AudioSettings, scope),
			[SheetKey.BootstrapDataHash] = static (l, scope) => l.LoadSheetAsync<BootstrapDataRoot>(SheetKey.BootstrapData, scope),
			[SheetKey.ErrorCodeHash] = static (l, scope) => l.LoadSheetAsync<ErrorCodeRoot>(SheetKey.ErrorCode, scope),
			[SheetKey.GraphicsSettingsHash] = static (l, scope) => l.LoadSheetAsync<GraphicsSettingsRoot>(SheetKey.GraphicsSettings, scope),
			[SheetKey.LocaleSettingsHash] = static (l, scope) => l.LoadSheetAsync<LocaleSettingsRoot>(SheetKey.LocaleSettings, scope),
			[SheetKey.SceneInfoHash] = static (l, scope) => l.LoadSheetAsync<SceneInfoRoot>(SheetKey.SceneInfo, scope),
			[SheetKey.SplashEntryInfoHash] = static (l, scope) => l.LoadSheetAsync<SplashEntryInfoRoot>(SheetKey.SplashEntryInfo, scope),
			[SheetKey.BootHash] = static (l, scope) => l.LoadSheetAsync<BootRoot>(SheetKey.Boot, scope),
			[SheetKey.BootstrapLocale_KoHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Ko, scope),
			[SheetKey.BootstrapLocale_JpHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Jp, scope),
			[SheetKey.BootstrapLocale_EnHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_En, scope),
			[SheetKey.ErrorMsgLocale_KoHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Ko, scope),
			[SheetKey.ErrorMsgLocale_JpHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Jp, scope),
			[SheetKey.ErrorMsgLocale_EnHash] = static (l, scope) => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_En, scope),
		};
	}
}
