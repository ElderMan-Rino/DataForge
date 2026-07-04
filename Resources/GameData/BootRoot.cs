using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootRoot
	{
		public BlobArray<BootRow> Rows;
	}
}
