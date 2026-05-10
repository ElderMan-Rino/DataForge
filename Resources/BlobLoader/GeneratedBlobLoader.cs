using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public sealed class GeneratedBlobLoader : IGameDataLoader
	{
		private static readonly Dictionary<string, Func<IDataSheetLoader, UniTask>> _registry = new()
		{
			[SheetKey.SceneInfo] = l => l.LoadSheetAsync<SceneInfoRoot>(SheetKey.SceneInfo),
			[SheetKey.ErrorCode] = l => l.LoadSheetAsync<ErrorCodeRoot>(SheetKey.ErrorCode),
			[SheetKey.BootstrapData] = l => l.LoadSheetAsync<BootstrapDataRoot>(SheetKey.BootstrapData),
			[SheetKey.AudioSettings] = l => l.LoadSheetAsync<AudioSettingsRoot>(SheetKey.AudioSettings),
			[SheetKey.GraphicsSettings] = l => l.LoadSheetAsync<GraphicsSettingsRoot>(SheetKey.GraphicsSettings),
			[SheetKey.LocaleSettings] = l => l.LoadSheetAsync<LocaleSettingsRoot>(SheetKey.LocaleSettings),
		};

		public UniTask LoadAsync<T>(IDataSheetLoader sheetLoader, string key) where T : unmanaged
			=> sheetLoader.LoadSheetAsync<T>(key);

		public UniTask LoadByKeyAsync(IDataSheetLoader sheetLoader, string key)
			=> _registry.TryGetValue(key, out var load)
				? load(sheetLoader)
				: throw new KeyNotFoundException(key);
	}
}
