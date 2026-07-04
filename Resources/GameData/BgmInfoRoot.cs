using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BgmInfoRoot
	{
		public BlobArray<BgmInfoRow> Rows;
	}
}
