namespace PerfTap.Interop
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct PDH_FMT_COUNTERVALUE_DOUBLE
	{
		public uint CStatus;
		public double doubleValue;
	}
}