using ElementsOfHarmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Microsoft.Kinect
{
	public class KinectSensor : Unknown
	{
		public static readonly Guid IKinectSensor_IID = new Guid("3C6EBA94-0DE1-4360-B6D4-653A10794C8B");
		public override Guid IID => IKinectSensor_IID;

		internal KinectSensor(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[DllImport("Kinect20.dll", CallingConvention = CallingConvention.Winapi)]
		public unsafe static extern int GetDefaultKinectSensor(IntPtr* defaultKinectSensor);

		public static KinectSensor GetDefault()
		{
			IntPtr ptr;
			unsafe
			{
				Marshal.ThrowExceptionForHR(GetDefaultKinectSensor(&ptr));
			}
			return new KinectSensor(ptr, true);
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int OpenProc(IntPtr pInstance);
		public int Open() => (int)Invoke<OpenProc>(6);
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int get_BodyFrameSourceProc(IntPtr pInstance, IntPtr* bodyFrameSource);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006: Naming Convention", Justification = "is original")]
		public unsafe int get_BodyFrameSource(IntPtr* bodyFrameSource) => (int)Invoke<get_BodyFrameSourceProc>(12, (IntPtr)bodyFrameSource);

		public BodyFrameSource BodyFrameSource
		{
			get
			{
				IntPtr ptr;
				unsafe
				{
					Marshal.ThrowExceptionForHR(get_BodyFrameSource(&ptr));
				}
				return new BodyFrameSource(ptr, true);
			}
		}
	}

	public class BodyFrameSource : Unknown
	{
		public static readonly Guid IBodyFrameSource_IID = new Guid("BB94A78A-458C-4608-AC69-34FEAD1E3BAE");
		public override Guid IID => IBodyFrameSource_IID;

		internal BodyFrameSource(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int GetBodyCountProc(IntPtr pInstance, IntPtr bodyCount);
		public unsafe int GetBodyCount(int* bodyCount) => (int)Invoke<GetBodyCountProc>(7, (IntPtr)bodyCount);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int OpenReaderProc(IntPtr pInstance, IntPtr* reader);
		public unsafe int OpenReader(IntPtr* reader) => (int)Invoke<OpenReaderProc>(8, (IntPtr)reader);

		public int BodyCount
		{
			get
			{
				int count = 0;
				unsafe
				{
					Marshal.ThrowExceptionForHR(GetBodyCount(&count));
				}
				return count;
			}
		}

		public BodyFrameReader OpenReader()
		{
			IntPtr ptr;
			unsafe
			{
				Marshal.ThrowExceptionForHR(OpenReader(&ptr));
			}
			return new BodyFrameReader(ptr, true);
		}
	}
	
	public class BodyFrameReader : Unknown
	{
		public static readonly Guid IBodyFrameReader_IID = new Guid("45532DF5-A63C-418F-A39F-C567936BC051");
		public override Guid IID => IBodyFrameReader_IID;

		private readonly ManualResetEvent StopEvent;
		private readonly Thread WaitThread;
		internal BodyFrameReader(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef)
		{
			StopEvent = new ManualResetEvent(false);
			IntPtr WaitableHandle;
			unsafe
			{
				Marshal.ThrowExceptionForHR(SubscribeFrameArrived(&WaitableHandle));
			}
			WaitThread = new Thread(Waitable =>
			{
				IntPtr[] Handles = new IntPtr[2]
				{
					(IntPtr)Waitable, StopEvent.SafeWaitHandle.DangerousGetHandle(),
				};
				loop:
				uint result = WaitForMultipleObjects(2, Handles, false, unchecked((uint)Timeout.Infinite));
				switch (result)
				{
					case 0:
						try
						{
							IntPtr pEventArgs;
							unsafe
							{
								Marshal.ThrowExceptionForHR(GetFrameArrivedEventData((IntPtr)Waitable, &pEventArgs));
							}
							using BodyFrameArrivedEventArgs EventArgs = new BodyFrameArrivedEventArgs(pEventArgs, true);
							FrameArrived?.Invoke(this, EventArgs);
						}
						catch (COMException)
						{
							// this happens occasionally for some frames for no reason...
							// doesn't cause errors for later frames though
							// so we can just ignore this faulty frame and move on
						}
						goto loop;
					case 1:
						break;
					default:
						Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
						break;
				}
			});
			WaitThread.Start(WaitableHandle);
		}

		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj);
		}
		public override int GetHashCode()
		{
			return WaitThread.GetHashCode();
		}

		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		private static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

		protected override void Dispose(bool disposing)
		{
			StopEvent.Set();
			WaitThread.Join();

			StopEvent.Dispose();
			base.Dispose(disposing);
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int SubscribeFrameArrivedProc(IntPtr pInstance, IntPtr* waitableHandle);
		public unsafe int SubscribeFrameArrived(IntPtr* waitableHandle) => (int)Invoke<SubscribeFrameArrivedProc>(3, (IntPtr)waitableHandle);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int UnsubscribeFrameArrivedProc(IntPtr pInstance, IntPtr waitableHandle);
		public int UnsubscribeFrameArrived(IntPtr waitableHandle) => (int)Invoke<UnsubscribeFrameArrivedProc>(4, waitableHandle);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int GetFrameArrivedEventDataProc(IntPtr pInstance, IntPtr waitableHandle, IntPtr* eventData);
		public unsafe int GetFrameArrivedEventData(IntPtr waitableHandle, IntPtr* eventData) => (int)Invoke<GetFrameArrivedEventDataProc>(5, waitableHandle, (IntPtr)eventData);

		public event EventHandler<BodyFrameArrivedEventArgs>? FrameArrived;
	}

	public class BodyFrameArrivedEventArgs : Unknown
	{
		public static readonly Guid IBodyFrameArrivedEventArgs_IID = new Guid("BF5CCA0E-00C1-4D48-837F-AB921E6AEE01");
		public override Guid IID => IBodyFrameArrivedEventArgs_IID;

		internal BodyFrameArrivedEventArgs(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int GetFrameReferenceProc(IntPtr pInstance, IntPtr* bodyFrameReference);
		public unsafe int GetFrameReference(IntPtr* bodyFrameReference) => (int)Invoke<GetFrameReferenceProc>(3, (IntPtr)bodyFrameReference);

		public BodyFrameReference FrameReference
		{
			get
			{
				IntPtr ptr;
				unsafe
				{
					Marshal.ThrowExceptionForHR(GetFrameReference(&ptr));
				}
				return new BodyFrameReference(ptr, true);
			}
		}
	}

	public class BodyFrameReference : Unknown
	{
		public static readonly Guid IBodyFrameReference_IID = new Guid("C3A1733C-5F84-443B-9659-2F2BE250C97D");
		public override Guid IID => IBodyFrameReference_IID;

		internal BodyFrameReference(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int AcquireFrameProc(IntPtr pInstance, IntPtr* bodyFrame);
		public unsafe int AcquireFrame(IntPtr* bodyFrame) => (int)Invoke<AcquireFrameProc>(3, (IntPtr)bodyFrame);

		public BodyFrame AcquireFrame()
		{
			IntPtr ptr;
			unsafe
			{
				Marshal.ThrowExceptionForHR(AcquireFrame(&ptr));
			}
			return new BodyFrame(ptr, true);
		}
	}

	public class BodyFrame : Unknown
	{
		public static readonly Guid IBodyFrame_IID = new Guid("52884F1F-94D7-4B57-BF87-9226950980D5");
		public override Guid IID => IBodyFrame_IID;

		internal BodyFrame(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int GetAndRefreshBodyDataProc(IntPtr pInstance, uint capacity, IntPtr[] bodies);
		public int GetAndRefreshBodyData(uint capacity, IntPtr[] bodies) => (int)Invoke<GetAndRefreshBodyDataProc>(3, capacity, bodies);

		public void GetAndRefreshBodyData(IList<Body> bodies)
		{
			IntPtr[] pointers = new IntPtr[bodies.Count];
			Marshal.ThrowExceptionForHR(GetAndRefreshBodyData((uint)pointers.Length, pointers));
			for (int n = 0; n < pointers.Length; n++)
			{
				bodies[n] = new Body(pointers[n], true);
			}
		}
	}

	public class Body : Unknown
	{
		public static readonly Guid IBody_IID = new Guid("46AEF731-98B0-4D18-827B-933758678F4A");
		public override Guid IID => IBody_IID;

		internal Body(IntPtr pInstance, bool OwnsInstance_DoNotAddRef = false) : base(pInstance, OwnsInstance_DoNotAddRef) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int GetJointsProc(IntPtr pInstance, uint capacity, Joint[] joints);
		public int GetJoints(uint capacity, Joint[] joints) => (int)Invoke<GetJointsProc>(3, capacity, joints);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int GetHandStateProc(IntPtr pInstance, HandState* handState);
		public unsafe int GetHandLeftState(HandState* handState) => (int)Invoke<GetHandStateProc>(9, (IntPtr)handState);
		public unsafe int GetHandRightState(HandState* handState) => (int)Invoke<GetHandStateProc>(11, (IntPtr)handState);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int GetIsTrackedProc(IntPtr pInstance, bool* tracked);
		public unsafe int GetIsTracked(bool* tracked) => (int)Invoke<GetIsTrackedProc>(15, (IntPtr)tracked);

		public IReadOnlyDictionary<JointType, Joint> Joints
		{
			get
			{
				Joint[] joints = new Joint[(int)(JointType.ThumbRight + 1)];
				Marshal.ThrowExceptionForHR(GetJoints((uint)joints.Length, joints));
				return new JointDictionary(joints);
			}
		}

		public HandState HandLeftState
		{
			get
			{
				HandState handState;
				unsafe
				{
					Marshal.ThrowExceptionForHR(GetHandLeftState(&handState));
				}
				return handState;
			}
		}

		public HandState HandRightState
		{
			get
			{
				HandState handState;
				unsafe
				{
					Marshal.ThrowExceptionForHR(GetHandRightState(&handState));
				}
				return handState;
			}
		}

		public bool IsTracked
		{
			get
			{
				bool tracked;
				unsafe
				{
					Marshal.ThrowExceptionForHR(GetIsTracked(&tracked));
				}
				return tracked;
			}
		}

		public class JointDictionary : IReadOnlyDictionary<JointType, Joint>
		{
			public JointDictionary(Joint[] values) { this.values = values; }

			public readonly Joint[] values;

			public Joint this[JointType key] => values[(int)key];

			public IEnumerable<JointType> Keys => Enumerable.Cast<JointType>(typeof(JointType).GetEnumValues());

			public IEnumerable<Joint> Values => values;

			public int Count => values.Length;

			public bool ContainsKey(JointType key)
			{
				return typeof(JointType).GetEnumName(key) != null;
			}

			public IEnumerator<KeyValuePair<JointType, Joint>> GetEnumerator()
			{
				for (JointType n = JointType.SpineBase; n < (JointType.ThumbRight + 1); n++)
				{
					yield return new KeyValuePair<JointType, Joint>(n, values[(int)n]);
				}
			}

			public bool TryGetValue(JointType key, out Joint value)
			{
				if ((int)key >= 0 && (int)key < values.Length)
				{
					value = this[key];
					return true;
				}
				else
				{
					value = default;
					return false;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Joint
	{
		public JointType JointType;
		public Vector3 Position;
		public TrackingState TrackingState;
	}

	public enum HandState
	{
		Unknown,
		NotTracked,
		Open,
		Closed,
		Lasso
	}

	public enum JointType
	{
		SpineBase,
		SpineMid,
		Neck,
		Head,
		ShoulderLeft,
		ElbowLeft,
		WristLeft,
		HandLeft,
		ShoulderRight,
		ElbowRight,
		WristRight,
		HandRight,
		HipLeft,
		KneeLeft,
		AnkleLeft,
		FootLeft,
		HipRight,
		KneeRight,
		AnkleRight,
		FootRight,
		SpineShoulder,
		HandTipLeft,
		ThumbLeft,
		HandTipRight,
		ThumbRight
	}

	public enum TrackingState
	{
		NotTracked,
		Inferred,
		Tracked
	}
}
