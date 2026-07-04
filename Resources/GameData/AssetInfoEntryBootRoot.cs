using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct AssetInfoEntryBootRoot
	{
		public BlobArray<AssetInfoEntryBootRow> Rows;
	}
}
