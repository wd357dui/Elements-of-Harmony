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

			OverlayDraw += OnOverlayDraw;

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
						Device.DrawPlainText("◀", new D2D1_RECT_F() { Left = LeftCenter.x - (Scale * 200.0f), Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▲", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y - (Scale * 200.0f), Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▶", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x + (Scale * 200.0f), Bottom = LeftCenter.y });
						Device.DrawPlainText("▼", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y + (Scale * 200.0f) });
					}

					// used for drawing a letter on a button
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

						if (Player.RightLasso)
						{
							// slightly larger right stick's circle
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
							Device.FillEllipse(RightCenter, Scale * 180.0f, Scale * 180.0f);

							DelayAction = () =>
							{
								// LB button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.NorthWest == true ? Highlight : NestedTransparent));
								Vector2 NorthWestButtonCenterPoint = RightCenter + new Vector2(Scale * -80.0f, Scale * -80.0f);
								D2D1_RECT_F NorthWestButtonRectangle = new D2D1_RECT_F
								{
									Left = NorthWestButtonCenterPoint.x - Scale * 60.0f,
									Top = NorthWestButtonCenterPoint.y - Scale * 30.0f,
									Right = NorthWestButtonCenterPoint.x + Scale * 60.0f,
									Bottom = NorthWestButtonCenterPoint.y + Scale * 30.0f,
								};
								Device.FillRectangle(NorthWestButtonRectangle, Scale * 8.0f, Scale * 8.0f);
								NorthWestButtonRectangle.Top -= Scale * 10.0f; // move a little bit more to the top
								DrawOutlinedText("LB", NorthWestButtonRectangle);

								// RB button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.NorthEast == true ? Highlight : NestedTransparent));
								Vector2 NorthEastButtonCenterPoint = RightCenter + new Vector2(Scale * 80.0f, Scale * -80.0f);
								D2D1_RECT_F NorthEastButtonRectangle = new D2D1_RECT_F
								{
									Left = NorthEastButtonCenterPoint.x - Scale * 60.0f,
									Top = NorthEastButtonCenterPoint.y - Scale * 30.0f,
									Right = NorthEastButtonCenterPoint.x + Scale * 60.0f,
									Bottom = NorthEastButtonCenterPoint.y + Scale * 30.0f,
								};
								Device.FillRectangle(NorthEastButtonRectangle, Scale * 8.0f, Scale * 8.0f);
								NorthEastButtonRectangle.Top -= Scale * 10.0f; // move a little bit more to the top
								DrawOutlinedText("RB", NorthEastButtonRectangle);

								// View button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.SouthWest == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(Scale * -80.0f, Scale * 80.0f), Scale * 60.0f, Scale * 60.0f);
								DrawOutlinedText("🗗", new D2D1_RECT_F()
								{
									Left = RightCenter.x - (Scale * 160.0f),
									Top = RightCenter.y + (Scale * 160.0f) - (Scale * 20.0f), // move a little bit more to the top
									Right = RightCenter.x,
									Bottom = RightCenter.y
								});

								// Menu button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.SouthEast == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(Scale * 80.0f, Scale * 80.0f), Scale * 60.0f, Scale * 60.0f);
								DrawOutlinedText("≡", new D2D1_RECT_F()
								{
									Left = RightCenter.x + (Scale * 160.0f),
									Top = RightCenter.y + (Scale * 160.0f) - (Scale * 20.0f), // move a little bit more to the top
									Right = RightCenter.x,
									Bottom = RightCenter.y
								});
							};
						}
						else
						{
							// right stick's circle
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
							Device.FillEllipse(RightCenter, Scale * 150.0f, Scale * 150.0f);

							DelayAction = () =>
							{
								// I just found that there are characters in unicode for 🅐🅑🅧🅨,
								// but I'm not going to use them because it's not worth the headache of
								// aligning positions and sizes all over again,
								// not to mention the struggle of adding outline to a character

								// A button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.green));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.South == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(0.0f, Scale * 100.0f), Scale * 50.0f, Scale * 50.0f);
								DrawOutlinedText("A", new D2D1_RECT_F()
								{
									Left = RightCenter.x,
									Top = RightCenter.y - (Scale * 10.0f),
									Right = RightCenter.x,
									Bottom = RightCenter.y + (Scale * 200.0f)
								});

								// B button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.red));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.East == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(Scale * 100.0f, 0.0f), Scale * 50.0f, Scale * 50.0f);
								DrawOutlinedText("B", new D2D1_RECT_F()
								{
									Left = RightCenter.x,
									Top = RightCenter.y - (Scale * 10.0f),
									Right = RightCenter.x + (Scale * 200.0f),
									Bottom = RightCenter.y
								}, PY: 2.0f); // outline of "B" need an additional Y offset to look right

								/* since X button is being used in AZHM, I've decided to not use the west button as menu button anymore
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
								*/

								// X button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.blue));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.West == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(Scale * -100.0f, 0.0f), Scale * 50.0f, Scale * 50.0f);
								DrawOutlinedText("X", new D2D1_RECT_F()
								{
									Left = RightCenter.x - (Scale * 200.0f),
									Top = RightCenter.y - (Scale * 10.0f),
									Right = RightCenter.x,
									Bottom = RightCenter.y
								});

								// Y button
								Marshal.ThrowExceptionForHR(Device.SetColor(Color.yellow));
								Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.North == true ? Highlight : NestedTransparent));
								Device.FillEllipse(RightCenter + new Vector2(0.0f, Scale * -100.0f), Scale * 50.0f, Scale * 50.0f);
								DrawOutlinedText("Y", new D2D1_RECT_F()
								{
									Left = RightCenter.x,
									Top = RightCenter.y - (Scale * 200.0f) - (Scale * 10.0f),
									Right = RightCenter.x,
									Bottom = RightCenter.y
								});
							};
						}
					}

					// left pointer
					if (Player.LeftPos is Vector2 Left &&
						Player.LeftShoulderPos is Vector2 LeftShoulder)
					{
						// convert to pixel coordinates
						Left += new Vector2(1.0f, 1.0f);
						Left *= new Vector2(0.5f, 0.5f);
						Left.y = 1.0f - Left.y;
						Left *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
						LeftShoulder += new Vector2(1.0f, 1.0f);
						LeftShoulder *= new Vector2(0.5f, 0.5f);
						LeftShoulder.y = 1.0f - LeftShoulder.y;
						LeftShoulder *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// draw a line from left shoulder to left hand,
						// and then draw a circle on the left hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(LeftShouldHighlight ? Highlight : Player.LeftPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(LeftShoulder, Left, Scale * 10.0f);
						Device.FillEllipse(Left, Scale * 40.0f, Scale * 40.0f);
					}

					// right pointer
					if (Player.RightPos is Vector2 Right &&
						Player.RightShoulderPos is Vector2 RightShoulder)
					{
						// convert to pixel coordinates
						Right += new Vector2(1.0f, 1.0f);
						Right *= new Vector2(0.5f, 0.5f);
						Right.y = 1.0f - Right.y;
						Right *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
						RightShoulder += new Vector2(1.0f, 1.0f);
						RightShoulder *= new Vector2(0.5f, 0.5f);
						RightShoulder.y = 1.0f - RightShoulder.y;
						RightShoulder *= new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

						// draw a line from right shoulder to right hand,
						// and then draw a circle on the right hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(RightShouldHighlight ? Highlight : Player.RightPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(RightShoulder, Right, Scale * 10.0f);
						Device.FillEllipse(Right, Scale * 40.0f, Scale * 40.0f);

						/* it's good practice while it lasted, but I've decided to use add another layer of buttons
						 * instead of just one button
						 * 
						if (Player.RightLassoProgress is decimal Progress)
						{
							// draw a menu button and a circle outline
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.RightLassoValidated ? Highlight : NestedTransparent));
							Device.FillEllipse(Right, Scale * 50.0f, Scale * 50.0f);
							DrawOutlinedText("≡", new D2D1_RECT_F()
							{
								Left = Right.x,
								Top = Right.y - Scale * 20.0f, // this symbol needs to be moved a little bit more to the up
								Right = Right.x,
								Bottom = Right.y
							});

							// draw a circle outline as progress bar
							Marshal.ThrowExceptionForHR(Device.BeginDrawBezier(Right + (Scale * new Vector2(0.0f, -50.0f))));

							float Sector = 0.0f;
							if (Progress > 0.25m)
							{
								Device.AddBezier(
									Right + (Scale * new Vector2(0.0f, -50.0f)),
									Right + (Scale * new Vector2(50.0f, -50.0f)),
									Right + (Scale * new Vector2(50.0f, 0.0f)));
								Sector = 90.0f;
							}
							if (Progress > 0.5m)
							{
								Device.AddBezier(
									Right + (Scale * new Vector2(50.0f, 0.0f)),
									Right + (Scale * new Vector2(50.0f, 50.0f)),
									Right + (Scale * new Vector2(0.0f, 50.0f)));
								Sector = 180.0f;
							}
							if (Progress > 0.75m)
							{
								Device.AddBezier(
									Right + (Scale * new Vector2(0.0f, 50.0f)),
									Right + (Scale * new Vector2(-50.0f, 50.0f)),
									Right + (Scale * new Vector2(-50.0f, 0.0f)));
								Sector = 270.0f;
							}

							decimal Degrees = (Progress == 1.0m ? 1.0m : (Progress % 0.25m / 0.25m)) * 90.0m;
							Matrix4x4 ReferenceRotation = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, (float)(Degrees / 2m)));
							Matrix4x4 EndRotation = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, (float)Degrees));

							Vector3 Start = new Vector3(0.0f, -1.0f, 0.0f);
							Vector3 Reference = ReferenceRotation.MultiplyPoint(Start);
							Vector3 End = EndRotation.MultiplyPoint(Start);

							float K = Reference.y / Reference.x;
							Reference.x = -1.0f / K;
							Reference.y = -1.0f;

							Matrix4x4 SectorRotation = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, Sector));
							D2D1_POINT_2F StartPoint = Right + SectorRotation.MultiplyPoint(Start).XY() * Scale * 50.0f;
							D2D1_POINT_2F ReferencePoint = Right + SectorRotation.MultiplyPoint(Reference).XY() * Scale * 50.0f;
							D2D1_POINT_2F EndPoint = Right + SectorRotation.MultiplyPoint(End).XY() * Scale * 50.0f;

							Device.AddBezier(StartPoint, ReferencePoint, EndPoint);

							Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Highlight));
							Marshal.ThrowExceptionForHR(Device.EndDrawBezier(EndPoint, Scale * 8.0f));
						}
						else
						{
							// draw a circle on the right hand
							Device.FillEllipse(Right, Scale * 40.0f, Scale * 40.0f);
						}
						*/
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

			private bool PreviousRightLasso = false; // check if right hand was lasso in the previous frame

			internal bool? LeftStartPosCloserToHead = null, RightStartPosCloserToHead = null; // hand start holding pos is closer to head
			internal Vector2? LeftStartPos = null, RightStartPos = null; // hand start holding pos

			internal bool RightLasso = false;

			internal bool LeftPosCloserToHead = false, RightPosCloserToHead = false; // hand current pos is closer to head
			internal Vector2? LeftPos = null, RightPos = null; // hand current pos
			internal Vector2? LeftShoulderPos = null, RightShoulderPos = null; // shoulder current pos

			/*
			internal double? RightLassoStartTime = null; // time when right hand entered lasso state
			internal bool RightLassoValidated = false; // right hand had a valid lasso frame after long press threshold passed
			*/

			public void UpdateHandStatus(Body Body)
			{
				lock (this)
				{
					// determine hand state change in current frame
					Hand HandStatus(HandState BodyHandState, ref bool PreviousStateHolding)
					{
						if (PreviousStateHolding)
						{
							if (BodyHandState == HandState.Open)
							{
								PreviousStateHolding = false;
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
								PreviousStateHolding = true;
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

					// determine whether if the hand is closer to the head than the torso
					bool CloserToHead(Vector2 Pos) =>
						(Pos - Joints[JointType.Head].Position.XY()).sqrMagnitude <
						(Pos - Joints[JointType.SpineBase].Position.XY()).sqrMagnitude;

					// set or reset hand start pos when hand state is being changed
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

					if (RightStatus == Hand.Holding && Body.HandRightState == HandState.Lasso && PreviousRightLasso)
					{
						RightLasso = true;
					}
					else if (RightStatus != Hand.Holding)
					{
						RightLasso = false;
					}
					PreviousRightLasso = Body.HandRightState == HandState.Lasso;
				}
			}

			/// <summary>
			/// convert coordinate from infrared (depth) sensor space to screen space
			/// </summary>
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
						if (LeftStartPos != null && LeftStartPosCloserToHead == true && LeftPos != null)
						{
							return AdjustStick(LeftPos.Value - LeftStartPos.Value);
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
						if (RightStartPos != null && RightStartPosCloserToHead == true && RightPos != null)
						{
							return AdjustStick(RightPos.Value - RightStartPos.Value);
						}
						return null;
					}
				}
			}

			/*
			/// <summary>
			/// progress of the right hand's lasso state
			/// </summary>
			public decimal? RightLassoProgress
			{
				get
				{
					lock (this)
					{
						if (RightLassoStartTime != null)
						{
							decimal Elapsed = (decimal)(Time.realtimeSinceStartupAsDouble - RightLassoStartTime.Value);
							return Elapsed > 1.0m ? 1.0m : Elapsed;
						}
						else return null;
					}
				}
			}
			*/

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
				RightThresholdCrossed == true && !RightLasso &&
				RightStick!.Value.y < 0.0f && Math.Abs(RightStick.Value.x) < Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox B button
			/// </summary>
			public bool? East => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && !RightLasso &&
				RightStick!.Value.x > 0.0f && Math.Abs(RightStick.Value.x) > Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox X button
			/// </summary>
			public bool? West => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && !RightLasso &&
				RightStick!.Value.x < 0.0f && Math.Abs(RightStick.Value.x) > Math.Abs(RightStick.Value.y);

			/// <summary>
			/// xbox Y button
			/// </summary>
			public bool? North => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && !RightLasso &&
				RightStick!.Value.y > 0.0f && Math.Abs(RightStick.Value.x) < Math.Abs(RightStick.Value.y);

			/*
			/// <summary>
			/// xbox right stick button (which will use as menu button)
			/// </summary>
			public bool RightStickButton => RightLassoStartTime != null && RightLassoValidated;
			*/

			/// <summary>
			/// xbox View button
			/// </summary>
			public bool? SouthWest => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightLasso &&
				RightStick!.Value.x < 0.0f && RightStick!.Value.y < 0.0f;

			/// <summary>
			/// xbox Menu button
			/// </summary>
			public bool? SouthEast => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightLasso &&
				RightStick!.Value.x > 0.0f && RightStick!.Value.y < 0.0f;

			/// <summary>
			/// xbox LB button
			/// </summary>
			public bool? NorthWest => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightLasso &&
				RightStick!.Value.x < 0.0f && RightStick!.Value.y > 0.0f;

			/// <summary>
			/// xbox RB button
			/// </summary>
			public bool? NorthEast => RightThresholdCrossed == null ? (bool?)null :
				RightThresholdCrossed == true && RightLasso &&
				RightStick!.Value.x > 0.0f && RightStick!.Value.y > 0.0f;

		}

		public static Vector2 XY(this Vector3 V3) => new Vector2(V3.x, V3.y);
	}
}
