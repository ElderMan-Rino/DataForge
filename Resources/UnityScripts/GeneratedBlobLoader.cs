using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public static class GeneratedBlobLoader
	{
		public static async UniTask LoadAllDataAsync(IDataSheetLoader sheetLoader)
		{
			await sheetLoader.LoadSheetAsync<SceneInfoRoot>("SceneInfo");
		}
	}
}
