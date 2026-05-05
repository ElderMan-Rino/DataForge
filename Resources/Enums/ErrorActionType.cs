
namespace Elder.SkillTrial.Resources.Data
{
	public enum ErrorActionType
	{
		/// <summary>에러 없음</summary>
		None = 0,
		/// <summary>로그만 남기고 사용자에게 알리지 않음</summary>
		Ignore = 1,
		/// <summary>화면 하단에 잠깐 띄우고 조작은 허용함</summary>
		Toast = 2,
		/// <summary>확인 버튼이 있는 팝업을 띄움 (조작 중단)</summary>
		Popup = 3,
		/// <summary>재시도 버튼이 포함된 팝업을 띄움 (네트워크 등)</summary>
		Retry = 4,
		/// <summary>확인 클릭 시 타이틀 화면으로 강제 이동</summary>
		BackToTitle = 5,
		/// <summary>확인 클릭 시 앱을 강제 종료</summary>
		Quit = 6,
	}
}
