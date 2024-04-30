using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ElementsOfHarmony
{
	/// <summary>
	/// you made me do this Unity, since you've banned native/managed mixed code,
	/// I'm writing my own managed wrapper for C++ native IUnknown interface
	/// </summary>
	public class Unknown : IDisposable
	{
		protected IntPtr pInstance;

		/// <summary>
		/// get function address from VTable
		/// </summary>
		protected IntPtr this[uint Index] => Marshal.ReadIntPtr(Marshal.ReadIntPtr(pInstance), (int)(Index * IntPtr.Size));

		protected Unknown(IntPtr pInstance)
		{
			this.pInstance = pInstance;
		}

		/// <summary>
		/// invoke function with given VTable index and delegate type
		/// </summary>
		protected object Invoke<T>(uint Index, params object[] args) where T : Delegate
		{
			if (!VTable.TryGetValue(Index, out Delegate Function))
			{
				VTable.Add(Index, Function = Marshal.GetDelegateForFunctionPointer<T>(this[Index]));
			}
			return Function.DynamicInvoke(args.Prepend(pInstance).ToArray());
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int QueryInterfaceProc(IntPtr pInstance, Guid IID, IntPtr* PPV);
		public const int QueryInterfaceProcIndex = 0;
		public unsafe int QueryInterface(Guid IID, IntPtr* PPV) => (int)Invoke<QueryInterfaceProc>(QueryInterfaceProcIndex, IID, (IntPtr)PPV);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate uint AddRefProc(IntPtr pInstance);
		public const int AddRefProcIndex = 1;
		public uint AddRef() => (uint)Invoke<AddRefProc>(AddRefProcIndex);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate uint ReleaseProc(IntPtr pInstance);
		public const int ReleaseProcIndex = 2;
		public uint Release() => (uint)Invoke<ReleaseProc>(ReleaseProcIndex);

		protected Dictionary<uint, Delegate> VTable = new Dictionary<uint, Delegate>();

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose instance
					Release();
				}

				disposedValue = true;
			}
		}

		~Unknown()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

}
