using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct SceneInfoRoot
	{
		public BlobArray<SceneInfoRow> Rows;
	}
}
