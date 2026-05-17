using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct LocaleEntryRoot
	{
		public BlobArray<LocaleEntryRow> Rows;
	}
}
