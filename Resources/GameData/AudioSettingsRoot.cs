using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct AudioSettingsRoot
	{
		public BlobArray<AudioSettingsRow> Rows;
	}
}
