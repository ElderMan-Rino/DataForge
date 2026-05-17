using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public sealed class GeneratedBlobLoader : IGameDataLoader
	{
		public UniTask LoadAsync(IDataSheetLoader sheetLoader, string key)
			=> GeneratedBlobRegistry.Registry.TryGetValue(key, out var load)
				? load(sheetLoader)
				: throw new KeyNotFoundException(key);
	}
}
