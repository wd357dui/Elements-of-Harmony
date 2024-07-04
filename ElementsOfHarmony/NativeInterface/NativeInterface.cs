using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ElementsOfHarmony.NativeInterface
{
    /// <summary>
    /// you made me do this Unity, since you've banned native/managed mixed code,
    /// I'm writing my own managed wrapper for the C++ native IUnknown interface
    /// </summary>
    public class Unknown : IDisposable
    {
        protected IntPtr pInstance;

        public override bool Equals(object obj)
        {
            if (obj is Unknown other) return pInstance == other.pInstance;
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return pInstance.GetHashCode();
        }

        public IntPtr Instance => pInstance;

        public static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        public virtual Guid IID => IID_IUnknown;

        public Unknown()
        {
            pInstance = IntPtr.Zero;
        }
        public Unknown(IntPtr pInstance, bool NotOwningInstance_ShouldAddRef = true, int MethodCount = 3)
        {
            this.pInstance = pInstance;
            VTable.AddRange(Enumerable.Repeat<Delegate?>(null, MethodCount));
            if (NotOwningInstance_ShouldAddRef)
            {
                AddRef();
            }
        }

		public int As<T>(out T? PPV, Guid? IID = null) where T : Unknown, new()
		{
			PPV = new T();
			IID ??= PPV.IID;
			IntPtr Ptr;
			int result;
			unsafe
			{
				result = QueryInterface(IID.Value, &Ptr);
			}
			if (result == 0)
			{
				PPV.pInstance = Ptr;
			}
			else
			{
				PPV.Dispose();
				PPV = null;
			}
			return result;
		}
		public T? As<T>(Guid? IID = null) where T : Unknown, new()
		{
			As(out T? PPV, IID);
			return PPV;
		}
		public bool Is(Guid? IID = null)
		{
			if (IID != null)
			{
				using Unknown? Temp = As<Unknown>(IID);
				return Temp != null;
			}
			return false;
		}

		/// <summary>
		/// get function address from VTable
		/// </summary>
		protected IntPtr this[int Index] => Marshal.ReadIntPtr(Marshal.ReadIntPtr(pInstance), Index * IntPtr.Size);

        protected List<Delegate?> VTable = new List<Delegate?>();

        public static uint GetRefCount(IntPtr IUnknown)
        {
            using Unknown Temp = new Unknown(IUnknown, true);
            Temp.AddRef();
            uint RefCount = Temp.Release();
            Temp.pInstance = IntPtr.Zero;
            return RefCount;
        }

        /// <summary>
        /// invoke function with given VTable index and delegate type
        /// </summary>
        public object Invoke<T>(int Index, params object[] args) where T : Delegate
        {
            if (VTable.Count <= Index)
            {
                VTable.AddRange(Enumerable.Repeat<Delegate?>(null, Index - VTable.Count + 1));
            }
            Delegate Method = VTable[Index] ??= Marshal.GetDelegateForFunctionPointer<T>(this[Index]);
            return Method.DynamicInvoke(args.Prepend(pInstance).ToArray());
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int QueryInterfaceProc(IntPtr pInstance, Guid IID, IntPtr* PPV);
        public unsafe int QueryInterface(Guid IID, IntPtr* PPV) => (int)Invoke<QueryInterfaceProc>(0, IID, (IntPtr)PPV);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint AddRefProc(IntPtr pInstance);
        public uint AddRef() => (uint)Invoke<AddRefProc>(1);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint ReleaseProc(IntPtr pInstance);
        public uint Release() => (uint)Invoke<ReleaseProc>(2);



        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (pInstance != IntPtr.Zero)
                    {
                        Release();
                    }
                }
                disposedValue = true;
            }
        }

        ~Unknown()
        {
            Dispose(disposing: true);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

	public class NativeArrayAccess<T>
		where T : struct
	{
		public NativeArrayAccess(IntPtr Address, int ElementCount)
		{
			this.Address = Address;
			this.ElementCount = ElementCount;
		}
		protected readonly IntPtr Address;
		protected readonly int ElementCount;
		public T this[int index]
		{
			get
			{
				if (Address == IntPtr.Zero) throw new NullReferenceException();
				else if (index < 0 || index >= ElementCount) throw new IndexOutOfRangeException();
				else return Marshal.PtrToStructure<T>(IntPtr.Add(Address, index * Marshal.SizeOf<T>()));
			}
			set
			{
				if (Address == IntPtr.Zero) throw new NullReferenceException();
				else if (index < 0 || index >= ElementCount) throw new IndexOutOfRangeException();
				else Marshal.StructureToPtr(value, IntPtr.Add(Address, index * Marshal.SizeOf<T>()), false);
			}
		}
		public IntPtr FixedAddress => Address;
		public int Length => ElementCount;
		public void ReinterpretCopyToArray<PrimitiveType>(PrimitiveType[] Dst, long DstByteOffset, long SrcByteOffset, long SizeInBytes) where PrimitiveType : unmanaged
		{
			unsafe
			{
				fixed (void* Pinned = Dst)
				{
					Buffer.MemoryCopy((void*)(FixedAddress + (int)SrcByteOffset), (void*)(((IntPtr)Pinned) + (int)DstByteOffset),
						Marshal.SizeOf<PrimitiveType>() * Dst.Length - DstByteOffset,
						SizeInBytes);
				}
			}
		}
		public PrimitiveType[] ReinterpretToArray<PrimitiveType>() where PrimitiveType : unmanaged
		{
			if (Marshal.SizeOf<PrimitiveType>() != Marshal.SizeOf<T>()) throw new InvalidCastException();
			PrimitiveType[] Result = new PrimitiveType[ElementCount];
			ReinterpretCopyToArray(Result, 0, 0, Marshal.SizeOf<T>() * ElementCount);
			return Result;
		}
		public PrimitiveType[] ReinterpretToArray<PrimitiveType>(long Index, long Count) where PrimitiveType : unmanaged
		{
			if (Marshal.SizeOf<PrimitiveType>() != Marshal.SizeOf<T>()) throw new InvalidCastException();
			PrimitiveType[] Result = new PrimitiveType[Count];
			ReinterpretCopyToArray(Result, 0, Marshal.SizeOf<T>() * Index, Marshal.SizeOf<T>() * Count);
			return Result;
		}
	}
	public class AllocatedArrayAccess<T> : NativeArrayAccess<T>, IDisposable
		where T : struct
	{
		public AllocatedArrayAccess(int ElementCount) :
			base(Marshal.AllocHGlobal(Marshal.SizeOf<T>() * ElementCount), ElementCount)
		{ }

		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Marshal.FreeHGlobal(Address);
				}
				disposedValue = true;
			}
		}
		~AllocatedArrayAccess()
		{
			Dispose(disposing: true);
		}
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
