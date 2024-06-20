using HarmonyLib;
using Microsoft.Kinect;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using static ElementsOfHarmony.DirectXHook;

namespace ElementsOfHarmony.KinectControl
{
	public static class KinectControl
	{
		private static KinectSensor? sensor;
		private static BodyFrameSource? source;
		private static BodyFrameReader? reader;
		public static void Init()
		{
			sensor = KinectSensor.GetDefault();
			source = sensor.BodyFrameSource;
			reader = source.OpenReader();
			Marshal.ThrowExceptionForHR(sensor.Open());

			// apply all of our patch procedures using Harmony API
			Harmony element = new Harmony($"{typeof(KinectControl).FullName}");
			if (ElementsOfHarmony.IsAMBA)
			{
				Assembly KinectControl_AMBA =
					AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.KinectControl.AMBA") ??
					Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.KinectControl.AMBA.dll"));
				Type AMBA = KinectControl_AMBA.GetType("ElementsOfHarmony.KinectControl.AMBA.KinectControl");
				int Num = 0;
				foreach (var Patch in AMBA.GetNestedTypes())
				{
					new PatchClassProcessor(element, Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {AMBA.FullName} successful - {Num} Patches");
				}
				OverlayDraw += OnOverlayDraw;
			}
			if (ElementsOfHarmony.IsAZHM)
			{
				Assembly KinectControl_AZHM =
					AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.KinectControl.AZHM") ??
					Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.KinectControl.AZHM.dll"));
				Type AZHM = KinectControl_AZHM.GetType("ElementsOfHarmony.KinectControl.AZHM.KinectControl");
				int Num = 0;
				foreach (var Patch in AZHM.GetNestedTypes())
				{
					new PatchClassProcessor(element, Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {AZHM.FullName} successful - {Num} Patches");
				}
			}

			reader.FrameArrived += Reader_FrameArrived;

			Application.quitting += Application_quitting;
		}

		private static void Application_quitting()
		{
			sensor?.Dispose();
			source?.Dispose();
			reader?.Dispose();
		}

		private static Body[]? Bodies;

		public static PlayerStatus? Player1, Player2;
		private static readonly Color Player1Color = new Color(0.92f, 0.0f, 0.57f, 1.0f);
		private static readonly Color Player2Color = new Color(0.19f, 0.66f, 0.92f, 1.0f);
		private const float Highlight = 0.9f;
		private const float Transparent = 0.4f;
		private const float NestedTransparent = 0.3f;
		private const float FadedTransparent = 0.1f;
		private const float Threshold = 0.01f;

		private static void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			using BodyFrameReference reference = e.FrameReference;
			using BodyFrame frame = reference.AcquireFrame();
			if (frame == null) return;
			if (Bodies == null || Bodies.Length != source!.BodyCount)
			{
				Bodies = new Body[source!.BodyCount];
				Player1 = Player2 = null;
			}
			frame.GetAndRefreshBodyData(Bodies);

			// invalidate player status if lost track
			if (Player1?.BodyIndex is int PreviousIndex1 && !Bodies[PreviousIndex1].IsTracked)
			{
				Player1 = null;
			}
			if (Player2?.BodyIndex is int PreviousIndex2 && !Bodies[PreviousIndex2].IsTracked)
			{
				Player2 = null;
			}

			// select new tracked body
			if (Player1 == null)
			{
				for (int n = 0; n < Bodies.Length; n++)
				{
					if (Bodies[n].IsTracked && n != Player2?.BodyIndex)
					{
						Player1 = new PlayerStatus(n);
						break;
					}
				}
			}
			if (Player2 == null)
			{
				for (int n = 0; n < Bodies.Length; n++)
				{
					if (Bodies[n].IsTracked && n != Player1?.BodyIndex)
					{
						Player2 = new PlayerStatus(n);
						break;
					}
				}
			}

			if (Player1?.BodyIndex is int Index1) Player1.UpdateHandStatus(Bodies[Index1]);
			if (Player2?.BodyIndex is int Index2) Player2.UpdateHandStatus(Bodies[Index2]);

			for (int n = 0; n < Bodies.Length; n++)
			{
				Bodies[n]?.Dispose();
			}
		}
		private static void OnOverlayDraw(object sender, EventArgs _)
		{
			IntPtr Device = (IntPtr)sender;
			void DrawPlayerControls(PlayerStatus? Player, Color PlayerColor)
			{
				if (Player == null) return;
				lock (Player)
				{
					Action? DelayAction = null;

					float Scale = (Screen.currentResolution.width / 1920.0f + Screen.currentResolution.height / 1080.0f) / 2;
					Scale *= Screen.dpi / 96.0f;

					Marshal.ThrowExceptionForHR(Device.SetFont("Segoe UI",
						DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
						DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
						DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
						Scale * 96.0f * 0.75f, "en-US"));
					Marshal.ThrowExceptionForHR(Device.SetFontParams(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_CENTER,
						DWRITE_PARAGRAPH_ALIGNMENT.DWRITE_PARAGRAPH_ALIGNMENT_CENTER));

					bool LeftShouldHighlight = false;
					if (Player.LeftStartPosCloserToHead == true &&
						Player.LeftStartPos is Vector2 LeftCenter)
					{
						// left starting pos is legal
						LeftShouldHighlight = true;

						// convert to screen space
						LeftCenter += new Vector2(1.0f, 1.0f);
						LeftCenter *= new Vector2(0.5f, 0.5f);
						LeftCenter.y = 1.0f - LeftCenter.y;
						LeftCenter *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// left stick's circle
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.FillEllipse(LeftCenter, Scale * 150.0f, Scale * 150.0f);

						// left stick's arrows
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.white));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.DrawPlainText("◀", new D2D1_RECT_F() { Left = LeftCenter.x - Scale * 200.0f, Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▲", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y - Scale * 200.0f, Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▶", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x + Scale * 200.0f, Bottom = LeftCenter.y });
						Device.DrawPlainText("▼", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y + Scale * 200.0f });
					}

					bool RightShouldHighlight = false;
					if (Player.RightStartPosCloserToHead == true &&
						Player.RightStartPos is Vector2 RightCenter)
					{
						// right starting pos is legal
						RightShouldHighlight = true;

						// convert to screen space
						RightCenter += new Vector2(1.0f, 1.0f);
						RightCenter *= new Vector2(0.5f, 0.5f);
						RightCenter.y = 1.0f - RightCenter.y;
						RightCenter *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// right stick's circle
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.FillEllipse(RightCenter, Scale * 150.0f, Scale * 150.0f);

						DelayAction += () =>
						{

							void DrawOutlinedText(string Text, D2D1_RECT_F Rect, float PX = 1.0f, float PY = 1.0f, float NX = -1.0f, float NY = -1.0f)
							{
								PX = Scale * PX;
								PY = Scale * PY;
								NX = Scale * NX;
								NY = Scale * NY;
								Marshal.ThrowExceptionForHR(Device.SetOpacity(1.0f));
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + NX, Top = Rect.Top + NY, Right = Rect.Right + NX, Bottom = Rect.Bottom + NY });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + NX, Top = Rect.Top + 0, Right = Rect.Right + NX, Bottom = Rect.Bottom + 0 });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + NX, Top = Rect.Top + PY, Right = Rect.Right + NX, Bottom = Rect.Bottom + PY });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + 0, Top = Rect.Top + PY, Right = Rect.Right + 0, Bottom = Rect.Bottom + PY });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + 0, Top = Rect.Top + NY, Right = Rect.Right + 0, Bottom = Rect.Bottom + NY });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + PX, Top = Rect.Top + NY, Right = Rect.Right + PX, Bottom = Rect.Bottom + NY });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + PX, Top = Rect.Top + 0, Right = Rect.Right + PX, Bottom = Rect.Bottom + 0 });
								Device.DrawPlainText(Text, new D2D1_RECT_F() { Left = Rect.Left + PX, Top = Rect.Top + PY, Right = Rect.Right + PX, Bottom = Rect.Bottom + PY });
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.white));
								Device.DrawPlainText(Text, Rect);
							}

							// A button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.green));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.South == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(0.0f, Scale * 100.0f), Scale * 50.0f, Scale * 50.0f);
							DrawOutlinedText("A", new D2D1_RECT_F()
							{
								Left = RightCenter.x,
								Top = RightCenter.y - Scale * 10.0f,
								Right = RightCenter.x,
								Bottom = RightCenter.y + Scale * 200.0f
							});

							// B button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.red));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.East == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(Scale * 100.0f, 0.0f), Scale * 50.0f, Scale * 50.0f);
							DrawOutlinedText("B", new D2D1_RECT_F()
							{
								Left = RightCenter.x,
								Top = RightCenter.y - Scale * 10.0f,
								Right = RightCenter.x + Scale * 200.0f,
								Bottom = RightCenter.y
							}, PY: 2.0f); // outline of "B" need an additional Y offset to look right

							// Y button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.yellow));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.North == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(0.0f, Scale * -100.0f), Scale * 50.0f, Scale * 50.0f);
							DrawOutlinedText("Y", new D2D1_RECT_F()
							{
								Left = RightCenter.x,
								Top = RightCenter.y - Scale * 200.0f - Scale * 10.0f,
								Right = RightCenter.x,
								Bottom = RightCenter.y
							});

							// Menu button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.West == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(Scale * -100.0f, 0.0f), Scale * 50.0f, Scale * 50.0f);
							DrawOutlinedText("≡", new D2D1_RECT_F()
							{
								Left = RightCenter.x - Scale * 200.0f,
								Top = RightCenter.y - Scale * 20.0f, // this symbol needs to be moved a little bit more to the up
								Right = RightCenter.x,
								Bottom = RightCenter.y
							});
						};
					}

					if (Player.LeftPos is Vector2 Left &&
						Player.LeftShoulderPos is Vector2 LeftShoulder)
					{
						// convert to screen space
						Left += new Vector2(1.0f, 1.0f);
						Left *= new Vector2(0.5f, 0.5f);
						Left.y = 1.0f - Left.y;
						Left *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
						LeftShoulder += new Vector2(1.0f, 1.0f);
						LeftShoulder *= new Vector2(0.5f, 0.5f);
						LeftShoulder.y = 1.0f - LeftShoulder.y;
						LeftShoulder *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// draw a line from left shoulder to left hand, and draw a circle on the left hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(LeftShouldHighlight ? Highlight : Player.LeftPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(LeftShoulder, Left, Scale * 10.0f);
						Device.FillEllipse(Left, Scale * 40.0f, Scale * 40.0f);
					}
					if (Player.RightPos is Vector2 Right &&
						Player.RightShoulderPos is Vector2 RightShoulder)
					{
						// convert to screen space
						Right += new Vector2(1.0f, 1.0f);
						Right *= new Vector2(0.5f, 0.5f);
						Right.y = 1.0f - Right.y;
						Right *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
						RightShoulder += new Vector2(1.0f, 1.0f);
						RightShoulder *= new Vector2(0.5f, 0.5f);
						RightShoulder.y = 1.0f - RightShoulder.y;
						RightShoulder *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// draw a line from right shoulder to right hand, and draw a circle on the right hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(RightShouldHighlight ? Highlight : Player.RightPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(RightShoulder, Right, Scale * 10.0f);
						Device.FillEllipse(Right, Scale * 40.0f, Scale * 40.0f);
					}

					DelayAction?.Invoke();
				}
			}
			DrawPlayerControls(Player1, Player1Color);
			DrawPlayerControls(Player2, Player2Color);
		}

		public class PlayerStatus
		{
			public PlayerStatus(int BodyIndex)
			{
				this.BodyIndex = BodyIndex;
			}
			public readonly int BodyIndex;

			public enum Hand
			{
				NotHolding,
				EnterHolding,
				Holding,
				ExitHolding,
			}

			private bool PreviousLeftHolding = false, PreviousRightHolding = false; // check if hand was holding in the previous frame
			private Hand LeftStatus = Hand.NotHolding, RightStatus = Hand.NotHolding; // converted to current frame holding status

			public bool? LeftStartPosCloserToHead = null, RightStartPosCloserToHead = null; // hand start holding pos is closer to head
			public Vector2? LeftStartPos = null, RightStartPos = null; // hand start holding pos

			public bool LeftPosCloserToHead = false, RightPosCloserToHead = false; // hand current pos is closer to head
			public Vector2 LeftPos = Vector2.negativeInfinity, RightPos = Vector2.negativeInfinity; // hand current pos
			public Vector2 LeftShoulderPos = Vector2.negativeInfinity, RightShoulderPos = Vector2.negativeInfinity; // shoulder current pos
			public void UpdateHandStatus(Body Body)
			{
				lock (this)
				{
					Hand HandStatus(HandState BodyHandState, ref bool PreviousStatus)
					{
						if (PreviousStatus)
						{
							if (BodyHandState == HandState.Open)
							{
								PreviousStatus = false;
								return Hand.ExitHolding;
							}
							else
							{
								return Hand.Holding;
							}
						}
						else
						{
							if (BodyHandState == HandState.Closed)
							{
								PreviousStatus = true;
								return Hand.EnterHolding;
							}
							else
							{
								return Hand.NotHolding;
							}
						}
					}
					LeftStatus = HandStatus(Body.HandLeftState, ref PreviousLeftHolding);
					RightStatus = HandStatus(Body.HandRightState, ref PreviousRightHolding);

					var Joints = Body.Joints;

					bool CloserToHead(Vector2 Pos) =>
						(Pos - Joints[JointType.Head].Position.XY()).sqrMagnitude <
						(Pos - Joints[JointType.SpineBase].Position.XY()).sqrMagnitude;

					void UpdateHandStartPos(Hand Status, ref Vector2? StartPos, ref bool? StartPosCloserToHead, Vector2 HandPos)
					{
						switch (Status)
						{
							case Hand.EnterHolding:
								StartPos = HandPos;
								StartPosCloserToHead = CloserToHead(StartPos.Value);
								break;
							case Hand.ExitHolding:
							case Hand.NotHolding:
								StartPos = null;
								StartPosCloserToHead = null;
								break;
						}
					}
					UpdateHandStartPos(LeftStatus, ref LeftStartPos, ref LeftStartPosCloserToHead, Joints[JointType.HandLeft].Position.XY());
					UpdateHandStartPos(RightStatus, ref RightStartPos, ref RightStartPosCloserToHead, Joints[JointType.HandRight].Position.XY());

					LeftPos = Joints[JointType.HandLeft].Position.XY();
					RightPos = Joints[JointType.HandRight].Position.XY();
					LeftPosCloserToHead = CloserToHead(Joints[JointType.HandLeft].Position.XY());
					RightPosCloserToHead = CloserToHead(Joints[JointType.HandRight].Position.XY());
					LeftShoulderPos = Joints[JointType.ShoulderLeft].Position.XY();
					RightShoulderPos = Joints[JointType.ShoulderRight].Position.XY();
				}
			}

			private Vector2 AdjustStick(Vector2 V2)
			{
				V2.x *= 5.0f / 6.0f; // aspect ratio of depth sensor is 6:5, convert to 1:1
				V2.x *= (float)Screen.currentResolution.width / Screen.currentResolution.height; // and then convert to screen ratio
				return V2;
			}

			/// <summary>
			/// if start holding pos is legal, return left stick coordinates
			/// </summary>
			public Vector2? LeftStick
			{
				get
				{
					lock (this)
					{
						if (LeftStartPos != null && LeftStartPosCloserToHead == true)
						{
							return AdjustStick(LeftPos - LeftStartPos.Value);
						}
						return null;
					}
				}
			}

			/// <summary>
			/// if start holding pos is legal, return right stick coordinates
			/// </summary>
			public Vector2? RightStick
			{
				get
				{
					lock (this)
					{
						if (RightStartPos != null && RightStartPosCloserToHead == true)
						{
							return AdjustStick(RightPos - RightStartPos.Value);
						}
						return null;
					}
				}
			}

			public bool? LeftThresholdCrossed => LeftStick is Vector2 S ? S.sqrMagnitude >= Threshold : (bool?)null;
			public bool? RightThresholdCrossed => RightStick is Vector2 S ? S.sqrMagnitude >= Threshold : (bool?)null;

			/// <summary>
			/// dpad left
			/// </summary>
			public bool? Left => LeftThresholdCrossed == null ? (bool?)null :
				LeftThresholdCrossed == true && LeftStick!.Value.x < 0.0f && Math.Abs(LeftStick.Value.x) > Math.Abs(LeftStick.Value.y);

			/// <summary>
			/// dpad up
			/// </summary>
			public bool? Up => LeftThresholdCrossed == null ? (bool?)null :
				LeftThresholdCrossed == true && LeftStick!.Value.y > 0.0f && Math.Abs(LeftStick.Value.x) < Math.Abs(LeftStick.Value.y);

			/// <summary>
			/// dpad right
			/// </summary>
			public bool? Right => LeftThresholdCrossed == null ? (bool?)null :
				LeftThresholdCrossed == true && LeftStick!.Value.x > 0.0f && Math.Abs(LeftStick.Value.x) > Math.Abs(LeftStick.Value.y);

			/// <summary>
			/// dpad down
			/// </summary>
			public bool? Down => LeftThresholdCrossed == null ? (bool?)null :
				LeftThresholdCrossed == true && LeftStick!.Value.y < 0.0f && Math.Abs(LeftStick.Value.x) < Math.Abs(LeftStick.Value.y);

			/// <summary>
			/// xbox A button
			/// </summary>
			public bool? South => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightStick!.Value.y < 0.0f && Math.Abs(RightStick.Value.x) < Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox B button
			/// </summary>
			public bool? East => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightStick!.Value.x > 0.0f && Math.Abs(RightStick.Value.x) > Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox X button (but we're using this as menu button, because the game didn't use the X button and we need a menu button)
			/// </summary>
			public bool? West => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightStick!.Value.x < 0.0f && Math.Abs(RightStick.Value.x) > Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox Y button
			/// </summary>
			public bool? North => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightStick!.Value.y > 0.0f && Math.Abs(RightStick.Value.x) < Math.Abs(RightStick.Value.y);
		}

		public static Vector2 XY(this Vector3 V3) => new Vector2(V3.x, V3.y);
	}
}
