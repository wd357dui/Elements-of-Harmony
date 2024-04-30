using ElementsOfHarmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Kinect
{
	public class KinectSensor : Unknown
	{
		protected KinectSensor(IntPtr pInstance) : base(pInstance) { }
	}

	public class BodyFrameReader : Unknown
	{
		protected BodyFrameReader(IntPtr pInstance) : base(pInstance) { }
	}
	
	public class BodyFrame : Unknown
	{
		protected BodyFrame(IntPtr pInstance) : base(pInstance) { }
	}

	public class Body : Unknown
	{
		protected Body(IntPtr pInstance) : base(pInstance) { }
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
}
