using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using Unity.Entities;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public class GeneratedBlobLoader
	{
		public async UniTask LoadAllDataAsync(IDataSheetLoader sheetLoader)
		{
			var tasks = new List<UniTask>();

			tasks.Add(sheetLoader.LoadSheetAsync<TestSheetRoot>("TestSheet"));

			await UniTask.WhenAll(tasks);
		}
	}
}
