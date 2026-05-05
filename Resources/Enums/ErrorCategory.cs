
namespace Elder.SkillTrial.Resources.Data
{
	public enum ErrorCategory
	{
		/// <summary>에러 없음</summary>
		None = 0,
		/// <summary>클라이언트 기기 메모리, OS 권한, 알 수 없는 크래시 등 기본 시스템</summary>
		System = 1,
		/// <summary>소켓 연결 끊김, HTTP 타임아웃 등 통신 계층</summary>
		Network = 2,
		/// <summary>리소스 다운로드, 에셋 번들 압축 해제, 파일 무결성(체크섬) 검증</summary>
		Patch = 3,
		/// <summary>로그인 실패, 토큰 만료, 중복 접속, 계정 제재(블럭)</summary>
		Auth = 4,
		/// <summary>인게임 로직 예외, 데이터 동기화 실패, 비정상적인 조작 감지</summary>
		Game = 5,
		/// <summary>인앱 결제 실패, 영수증 검증 오류, 스토어 통신 장애</summary>
		Billing = 6,
	}
}
