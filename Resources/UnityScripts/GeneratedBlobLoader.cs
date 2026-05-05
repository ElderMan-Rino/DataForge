using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public sealed class GeneratedBlobLoader : IGameDataLoader
	{
		public async UniTask LoadAllAsync(IDataSheetLoader sheetLoader)
		{
			await UniTask.WhenAll(
				sheetLoader.LoadSheetAsync<SceneInfoRoot>("SceneInfo"),
				sheetLoader.LoadSheetAsync<BootstrapLocale_KoRoot>("BootstrapLocale_Ko"),
				sheetLoader.LoadSheetAsync<BootstrapLocale_JpRoot>("BootstrapLocale_Jp"),
				sheetLoader.LoadSheetAsync<BootstrapLocale_EnRoot>("BootstrapLocale_En"),
				sheetLoader.LoadSheetAsync<ErrorCodeRoot>("ErrorCode"),
				sheetLoader.LoadSheetAsync<ErrorMsgLocale_KoRoot>("ErrorMsgLocale_Ko"),
				sheetLoader.LoadSheetAsync<ErrorMsgLocale_JpRoot>("ErrorMsgLocale_Jp"),
				sheetLoader.LoadSheetAsync<ErrorMsgLocale_EnRoot>("ErrorMsgLocale_En")
			);
		}

		public async UniTask LoadAsync<T>(IDataSheetLoader sheetLoader, string key) where T : unmanaged
		{
			await sheetLoader.LoadSheetAsync<T>(key);
		}
	}
}
