using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public static class GeneratedBlobRegistry
	{
		// [HEAP] 초기화 시 1회 할당
		public static readonly Dictionary<int, Func<IDataSheetLoader, UniTask>> Registry = new()
		{
			[SheetKey.BootstrapLocaleKeyHash] = static l => l.LoadSheetAsync<BootstrapLocaleKeyRoot>(SheetKey.BootstrapLocaleKey),
			[SheetKey.AudioSettingsHash] = static l => l.LoadSheetAsync<AudioSettingsRoot>(SheetKey.AudioSettings),
			[SheetKey.BootstrapDataHash] = static l => l.LoadSheetAsync<BootstrapDataRoot>(SheetKey.BootstrapData),
			[SheetKey.ErrorCodeHash] = static l => l.LoadSheetAsync<ErrorCodeRoot>(SheetKey.ErrorCode),
			[SheetKey.GraphicsSettingsHash] = static l => l.LoadSheetAsync<GraphicsSettingsRoot>(SheetKey.GraphicsSettings),
			[SheetKey.LocaleSettingsHash] = static l => l.LoadSheetAsync<LocaleSettingsRoot>(SheetKey.LocaleSettings),
			[SheetKey.SceneInfoHash] = static l => l.LoadSheetAsync<SceneInfoRoot>(SheetKey.SceneInfo),
			[SheetKey.SplashEntryInfoHash] = static l => l.LoadSheetAsync<SplashEntryInfoRoot>(SheetKey.SplashEntryInfo),
			[SheetKey.BootHash] = static l => l.LoadSheetAsync<BootRoot>(SheetKey.Boot),
			[SheetKey.AssetInfoEntryBootHash] = static l => l.LoadSheetAsync<AssetInfoEntryBootRoot>(SheetKey.AssetInfoEntryBoot),
			[SheetKey.BgmInfoHash] = static l => l.LoadSheetAsync<BgmInfoRoot>(SheetKey.BgmInfo),
			[SheetKey.BootstrapLocale_KoHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Ko),
			[SheetKey.BootstrapLocale_JpHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_Jp),
			[SheetKey.BootstrapLocale_EnHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.BootstrapLocale_En),
			[SheetKey.ErrorMsgLocale_KoHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Ko),
			[SheetKey.ErrorMsgLocale_JpHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_Jp),
			[SheetKey.ErrorMsgLocale_EnHash] = static l => l.LoadSheetAsync<LocaleEntryRoot>(SheetKey.ErrorMsgLocale_En),
		};
	}
}
