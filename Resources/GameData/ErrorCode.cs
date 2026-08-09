using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct ErrorCode
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string LocaleKey;
		[Key(2)] public readonly int Id;
		[Key(3)] public readonly ErrorCategory Category;
		[Key(4)] public readonly ErrorActionType OkAction;
		[Key(5)] public readonly ErrorActionType CancelAction;
		[Key(6)] public readonly ButtonType ButtonType;
		[Key(7)] public readonly DismissPolicy DismissPolicy;

		[SerializationConstructor]
		public ErrorCode(string key, string localeKey, int id, ErrorCategory category, ErrorActionType okAction, ErrorActionType cancelAction, ButtonType buttonType, DismissPolicy dismissPolicy)
		{
			this.Key = key;
			this.LocaleKey = localeKey;
			this.Id = id;
			this.Category = category;
			this.OkAction = okAction;
			this.CancelAction = cancelAction;
			this.ButtonType = buttonType;
			this.DismissPolicy = dismissPolicy;
		}
	}
}
