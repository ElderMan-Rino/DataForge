using System;
using Unity.Entities;

namespace 
{
	public struct ErrorCodeRow
	{
		public BlobString Key;
		public BlobString LocaleKey;
		public int Id;
		public ErrorCategory Category;
		public ErrorActionType Action;
	}
}
