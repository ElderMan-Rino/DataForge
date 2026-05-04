using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public sealed class GeneratedBlobLoader : IGameDataLoader
	{
		public async UniTask LoadAllAsync(IDataSheetLoader sheetLoader)
		{
			await sheetLoader.LoadSheetAsync<SceneInfoRoot>("SceneInfo");
		}

		public async UniTask LoadAsync<T>(IDataSheetLoader sheetLoader, string key) where T : unmanaged
		{
			await sheetLoader.LoadSheetAsync<T>(key);
		}
	}
}
