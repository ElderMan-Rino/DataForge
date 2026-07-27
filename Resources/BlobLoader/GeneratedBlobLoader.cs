using Cysharp.Threading.Tasks;
using Elder.Framework.Data.Interfaces;
using System.Collections.Generic;

namespace Elder.SkillTrial.Resources.Data
{
	public sealed class GeneratedBlobLoader : IGameDataLoader
	{
		public UniTask LoadAsync(IDataSheetLoader sheetLoader, int hash, int scope)
		{
			return GeneratedBlobRegistry.Registry.TryGetValue(hash, out var load)
				? load(sheetLoader, scope)
				: throw new KeyNotFoundException(hash.ToString()); // [HEAP] error path only
		}
	}
}
