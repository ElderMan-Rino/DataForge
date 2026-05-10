
namespace Elder.SkillTrial.Resources.Data
{
	public enum FramerateType
	{
		/// <summary>저사양 / 배터리 절약</summary>
		Fps30 = 30,
		/// <summary>일반 (기본값)</summary>
		Fps60 = 60,
		/// <summary>고주사율 (Galaxy S 시리즈 등)</summary>
		Fps90 = 90,
		/// <summary>플래그십 모바일 / iPad Pro</summary>
		Fps120 = 120,
		/// <summary>PC 모니터 / ASUS ROG Phone 등</summary>
		Fps144 = 144,
		/// <summary>PC 고주사율 모니터</summary>
		Fps165 = 165,
		/// <summary>PC 고주사율 모니터</summary>
		Fps180 = 180,
		/// <summary>PC 경쟁용 모니터</summary>
		Fps240 = 240,
		/// <summary>제한 없음 (PC / 에디터)</summary>
		Unlimited = -1,
	}
}
