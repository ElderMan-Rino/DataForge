using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootstrapLocaleKeyRoot
	{
		public BlobArray<BootstrapLocaleKeyRow> Rows;
	}
}
