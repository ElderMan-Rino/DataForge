using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct ErrorCodeRoot
	{
		public BlobArray<ErrorCodeRow> Rows;
	}
}
