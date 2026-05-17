using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct GraphicsSettingsRoot
	{
		public BlobArray<GraphicsSettingsRow> Rows;
	}
}
