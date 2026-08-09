using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct ErrorCodeRow
	{
		public BlobString Key;
		public BlobString LocaleKey;
		public int Id;
		public ErrorCategory Category;
		public ErrorActionType OkAction;
		public ErrorActionType CancelAction;
		public ButtonType ButtonType;
		public DismissPolicy DismissPolicy;
	}
}
