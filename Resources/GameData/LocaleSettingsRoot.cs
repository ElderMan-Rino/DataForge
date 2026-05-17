using Unity.Burst;
using Unity.Entities;
namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct LocaleSettingsRoot
	{
		public BlobArray<LocaleSettingsRow> Rows;
	}
}
