using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootstrapDataRoot
	{
		public BlobArray<BootstrapDataRow> Rows;
	}
}
