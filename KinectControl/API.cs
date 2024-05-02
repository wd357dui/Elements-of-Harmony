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
		internal KinectSensor(IntPtr pInstance) : base(pInstance) { }

		[DllImport("Kinect20.dll", CallingConvention = CallingConvention.Winapi)]
		public unsafe static extern int GetDefaultKinectSensor(IntPtr* defaultKinectSensor);

		public static KinectSensor GetDefault()
		{
			IntPtr ptr;
			unsafe
			{
				Marshal.ThrowExceptionForHR(GetDefaultKinectSensor(&ptr));
			}
			return new KinectSensor(ptr);
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
				return new BodyFrameSource(ptr);
			}
		}
	}

	public class BodyFrameSource : Unknown
	{
		internal BodyFrameSource(IntPtr pInstance) : base(pInstance) { }

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
			return new BodyFrameReader(ptr);
		}
	}
	
	public class BodyFrameReader : Unknown
	{
		private readonly ManualResetEvent StopEvent;
		private readonly Thread WaitThread;

		internal BodyFrameReader(IntPtr pInstance) : base(pInstance)
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
							using (BodyFrameArrivedEventArgs EventArgs = new BodyFrameArrivedEventArgs(pEventArgs))
							{
								FrameArrived?.Invoke(this, EventArgs);
							}
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

		public event EventHandler<BodyFrameArrivedEventArgs> FrameArrived;
	}

	public class BodyFrameArrivedEventArgs : Unknown
	{
		internal BodyFrameArrivedEventArgs(IntPtr pInstance) : base(pInstance) { }

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
				return new BodyFrameReference(ptr);
			}
		}
	}

	public class BodyFrameReference : Unknown
	{
		internal BodyFrameReference(IntPtr pInstance) : base(pInstance) { }

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
			return new BodyFrame(ptr);
		}
	}

	public class BodyFrame : Unknown
	{
		internal BodyFrame(IntPtr pInstance) : base(pInstance) { }

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int GetAndRefreshBodyDataProc(IntPtr pInstance, uint capacity, IntPtr[] bodies);
		public int GetAndRefreshBodyData(uint capacity, IntPtr[] bodies) => (int)Invoke<GetAndRefreshBodyDataProc>(3, capacity, bodies);

		public void GetAndRefreshBodyData(IList<Body> bodies)
		{
			IntPtr[] pointers = new IntPtr[bodies.Count];
			Marshal.ThrowExceptionForHR(GetAndRefreshBodyData((uint)pointers.Length, pointers));
			for (int n = 0; n < pointers.Length; n++)
			{
				bodies[n] = new Body(pointers[n]);
			}
		}
	}

	public class Body : Unknown
	{
		internal Body(IntPtr pInstance) : base(pInstance) { }

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
