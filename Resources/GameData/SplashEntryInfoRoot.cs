using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct SplashEntryInfoRoot
	{
		public BlobArray<SplashEntryInfoRow> Rows;
	}
}
