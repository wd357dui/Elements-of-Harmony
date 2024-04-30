using HarmonyLib;
using Microsoft.Kinect;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static ElementsOfHarmony.DirectXHook;

namespace ElementsOfHarmony
{
	public static class KinectControl
	{
		private static KinectSensor sensor;
		private static BodyFrameReader reader;
		public static void Init()
		{
			sensor = KinectSensor.GetDefault();
			reader = sensor.BodyFrameSource.OpenReader();
			sensor.Open();

			// apply all of our patch procedures using Harmony API
			Harmony element = new Harmony($"{typeof(KinectControl).FullName}");
			if (ElementsOfHarmony.IsAMBA)
			{
				int Num = 0;
				foreach (var Patch in typeof(AMBA).GetNestedTypes())
				{
					element.CreateClassProcessor(Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {typeof(AMBA).FullName} successful - {Num} Patches");
				}
			}
			if (ElementsOfHarmony.IsAZHM)
			{
				int Num = 0;
				foreach (var Patch in typeof(AZHM).GetNestedTypes())
				{
					element.CreateClassProcessor(Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {typeof(AZHM).FullName} successful - {Num} Patches");
				}
			}

			reader.FrameArrived += Reader_FrameArrived;

			if (ElementsOfHarmony.IsAMBA)
			{
				OverlayDraw += DirectXHook_OverlayDraw;
			}

			Application.quitting += Application_quitting;
		}

		private static void Application_quitting()
		{
			reader?.Dispose();
		}

		public class PlayerStatus
		{
			public PlayerStatus(Body body) { Body = body; }
			public Body Body;

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
			public void UpdateHandStatus()
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

					bool CloserToHead(Vector2 Pos) =>
						(Pos - Body.Joints[JointType.Head].Position.ToVector2()).sqrMagnitude <
						(Pos - Body.Joints[JointType.FootLeft].Position.ToVector2()).sqrMagnitude;

					void UpdateHandStartPos(Hand Status, ref Vector2? StartPos, ref bool? StartPosCloserToHead)
					{
						switch (Status)
						{
							case Hand.EnterHolding:
								StartPos = Body.Joints[JointType.HandLeft].Position.ToVector2();
								StartPosCloserToHead = CloserToHead(StartPos.Value);
								break;
							case Hand.ExitHolding:
							case Hand.NotHolding:
								StartPos = null;
								StartPosCloserToHead = null;
								break;
						}
					}
					UpdateHandStartPos(LeftStatus, ref LeftStartPos, ref LeftStartPosCloserToHead);
					UpdateHandStartPos(RightStatus, ref RightStartPos, ref RightStartPosCloserToHead);

					LeftPos = Body.Joints[JointType.HandLeft].Position.ToVector2();
					RightPos = Body.Joints[JointType.HandRight].Position.ToVector2();
					LeftPosCloserToHead = CloserToHead(Body.Joints[JointType.HandLeft].Position.ToVector2());
					RightPosCloserToHead = CloserToHead(Body.Joints[JointType.HandRight].Position.ToVector2());
					LeftShoulderPos = Body.Joints[JointType.ShoulderLeft].Position.ToVector2();
					RightShoulderPos = Body.Joints[JointType.ShoulderRight].Position.ToVector2();
				}
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
						if (LeftStartPosCloserToHead == true && LeftPos != null && LeftStartPos != null)
						{
							return LeftPos - LeftStartPos.Value;
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
						if (RightStartPosCloserToHead == true && RightPos != null && RightStartPos != null)
						{
							return RightPos - RightStartPos.Value;
						}
						return null;
					}
				}
			}

			public bool? LeftThresholdCrossed => LeftStartPos == null ? (bool?)null : (LeftPos - LeftStartPos)?.sqrMagnitude >= Threshold;
			public bool? RightThresholdCrossed => RightStartPos == null ? (bool?)null : (RightPos - RightStartPos)?.sqrMagnitude >= Threshold;

			/// <summary>
			/// dpad left
			/// </summary>
			public bool? Left => LeftThresholdCrossed == null ? (bool?)null : LeftPos.x < 0.0f && Math.Abs(LeftPos.x) > Math.Abs(LeftPos.y);

			/// <summary>
			/// dpad up
			/// </summary>
			public bool? Up => LeftThresholdCrossed == null ? (bool?)null : LeftPos.y > 0.0f && Math.Abs(LeftPos.x) < Math.Abs(LeftPos.y);

			/// <summary>
			/// dpad right
			/// </summary>
			public bool? Right => LeftThresholdCrossed == null ? (bool?)null : LeftPos.x > 0.0f && Math.Abs(LeftPos.x) > Math.Abs(LeftPos.y);

			/// <summary>
			/// dpad down
			/// </summary>
			public bool? Down => LeftThresholdCrossed == null ? (bool?)null : LeftPos.y < 0.0f && Math.Abs(LeftPos.x) < Math.Abs(LeftPos.y);

			/// <summary>
			/// xbox A button
			/// </summary>
			public bool? South => RightThresholdCrossed == null ? (bool?)null : RightPos.y < 0.0f && Math.Abs(RightPos.x) < Math.Abs(RightPos.y);

			/// <summary>
			/// xbox B button
			/// </summary>
			public bool? East => RightThresholdCrossed == null ? (bool?)null : RightPos.x > 0.0f && Math.Abs(RightPos.x) > Math.Abs(RightPos.y);

			/// <summary>
			/// xbox X button (but we're using this as menu button, because the game didn't use the X button and we need a menu button)
			/// </summary>
			public bool? West => RightThresholdCrossed == null ? (bool?)null : RightPos.x < 0.0f && Math.Abs(RightPos.x) > Math.Abs(RightPos.y);

			/// <summary>
			/// xbox Y button
			/// </summary>
			public bool? North => RightThresholdCrossed == null ? (bool?)null : RightPos.y > 0.0f && Math.Abs(RightPos.x) < Math.Abs(RightPos.y);
		}

		private static Body[] Bodies;
		private static PlayerStatus Player1, Player2;
		private static readonly Color Player1Color = new Color(0.92f, 0.0f, 0.57f, 1.0f);
		private static readonly Color Player2Color = new Color(0.19f, 0.66f, 0.92f, 1.0f);
		private const float Highlight = 0.9f;
		private const float Transparent = 0.3f;
		private const float NestedTransparent = 0.2f;
		private const float FadedTransparent = 0.1f;
		private const float ExtraTransparent = 0.02f;
		private const float Threshold = 0.3f;

		private static void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			using (BodyFrame frame = e.FrameReference.AcquireFrame())
			{
				if (frame == null) return;
				if (Bodies == null || Bodies.Length != frame.BodyCount)
				{
					Bodies = new Body[frame.BodyCount];
					Player1 = Player2 = null;
				}
				frame.GetAndRefreshBodyData(Bodies);

				// invalidate player status if lost track
				if (Player1?.Body.IsTracked == false) Player1 = null;
				if (Player2?.Body.IsTracked == false) Player2 = null;

				// select new tracked body
				if (Player1 == null && Bodies.FirstOrDefault(B => B.IsTracked && B != Player2?.Body) is Body body1) Player1 = new PlayerStatus(body1);
				if (Player2 == null && Bodies.FirstOrDefault(B => B.IsTracked && B != Player1?.Body) is Body body2) Player2 = new PlayerStatus(body2);

				Player1?.UpdateHandStatus();
				Player2?.UpdateHandStatus();
			}
		}
		private static void DirectXHook_OverlayDraw(IntPtr Device)
		{
			void DrawPlayerControls(PlayerStatus Player, Color PlayerColor)
			{
				if (Player == null) return;
				lock (Player)
				{
					Action DelayAction = null;

					Marshal.ThrowExceptionForHR(Device.SetFont("Segoe UI",
						DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
						DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
						DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
						96.0f, "en-US"));
					Marshal.ThrowExceptionForHR(Device.SetFontParams(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_CENTER,
						DWRITE_PARAGRAPH_ALIGNMENT.DWRITE_PARAGRAPH_ALIGNMENT_CENTER));

					bool LeftShouldHighlight = false;
					if (Player.LeftStartPosCloserToHead == true &&
						Player.LeftStartPos is Vector2 LeftCenter)
					{
						// left starting pos is legal
						LeftShouldHighlight = true;

						// left stick's circle
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.FillEllipse(LeftCenter, 150.0f, 150.0f);

						// left stick's arrows
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.white));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.DrawPlainText("◀", new D2D1_RECT_F() { Left = LeftCenter.x - 100.0f, Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▲", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y - 100.0f, Right = LeftCenter.x, Bottom = LeftCenter.y });
						Device.DrawPlainText("▶", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x + 100.0f, Bottom = LeftCenter.y });
						Device.DrawPlainText("▼", new D2D1_RECT_F() { Left = LeftCenter.x, Top = LeftCenter.y, Right = LeftCenter.x, Bottom = LeftCenter.y + 100.0f });
					}

					bool RightShouldHighlight = false;
					if (Player.RightStartPosCloserToHead == true &&
						Player.RightStartPos is Vector2 RightCenter)
					{
						// right starting pos is legal
						RightShouldHighlight = true;

						// right stick's circle
						Marshal.ThrowExceptionForHR(Device.SetColor(Color.gray));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(Transparent));
						Device.FillEllipse(RightCenter, 150.0f, 150.0f);

						DelayAction += () => {
							// A button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.green));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.South == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(0.0f, 100.0f), 50.0f, 50.0f);
							Device.DrawPlainText("A", new D2D1_RECT_F() { Left = RightCenter.x, Top = RightCenter.y, Right = RightCenter.x, Bottom = RightCenter.y + 100.0f });

							// B button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.red));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.East == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(100.0f, 0.0f), 50.0f, 50.0f);
							Device.DrawPlainText("B", new D2D1_RECT_F() { Left = RightCenter.x, Top = RightCenter.y, Right = RightCenter.x + 100.0f, Bottom = RightCenter.y });

							// Y button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.yellow));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.North == true ? Highlight : NestedTransparent));
							Device.FillEllipse(RightCenter + new Vector2(0.0f, -100.0f), 50.0f, 50.0f);
							Device.DrawPlainText("Y", new D2D1_RECT_F() { Left = RightCenter.x, Top = RightCenter.y - 100.0f, Right = RightCenter.x, Bottom = RightCenter.y });

							// Menu button
							Marshal.ThrowExceptionForHR(Device.SetColor(Color.black));
							Marshal.ThrowExceptionForHR(Device.SetOpacity(Player.West == true ? Highlight : ExtraTransparent));
							Device.FillEllipse(RightCenter + new Vector2(-100.0f, 0.0f), 50.0f, 50.0f);
							Device.DrawPlainText("≡", new D2D1_RECT_F() { Left = RightCenter.x - 100.0f, Top = RightCenter.y, Right = RightCenter.x, Bottom = RightCenter.y });
						};
					}

					if (Player.LeftPos is Vector2 Left &&
						Player.LeftShoulderPos is Vector2 LeftShoulder)
					{
						// draw a line from left shoulder to left hand, and draw a circle on the left hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(LeftShouldHighlight ? Highlight : Player.LeftPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(LeftShoulder, Left, 10.0f);
						Device.FillEllipse(Left, 40.0f, 40.0f);
					}
					if (Player.RightPos is Vector2 Right &&
						Player.RightShoulderPos is Vector2 RightShoulder)
					{
						// draw a line from right shoulder to right hand, and draw a circle on the right hand
						Marshal.ThrowExceptionForHR(Device.SetColor(PlayerColor));
						Marshal.ThrowExceptionForHR(Device.SetOpacity(RightShouldHighlight ? Highlight : Player.RightPosCloserToHead ? Transparent : FadedTransparent));
						Device.DrawLine(RightShoulder, Right, 10.0f);
						Device.FillEllipse(Right, 40.0f, 40.0f);
					}

					DelayAction?.Invoke();
				}
			}
			DrawPlayerControls(Player1, Player1Color);
			DrawPlayerControls(Player2, Player2Color);
		}

		public static class AMBA
		{
			public struct ButtonStatus
			{
				public bool? LeftThresholdCrossed, RightThresholdCrossed,
					Left, Up, Right, Down,
					South, East, West, North;
			}
			public static ButtonStatus? PreviousPlayer1Status = null, PreviousPlayer2Status = null,
				CurrentPlayer1Status = null, CurrentPlayer2Status = null;
			public static int PreviousFrame = -1;

			public static void EnsureFrameUpdate()
			{
				if (PreviousFrame != Time.frameCount)
				{
					PreviousPlayer1Status = CurrentPlayer1Status;
					PreviousPlayer2Status = CurrentPlayer2Status;

					ButtonStatus GetStatus(PlayerStatus Player)
					{
						lock (Player)
						{
							return new ButtonStatus()
							{
								LeftThresholdCrossed = Player.LeftThresholdCrossed,
								RightThresholdCrossed = Player.RightThresholdCrossed,
								Left = Player.Left,
								Up = Player.Up,
								Right = Player.Right,
								Down = Player.Down,
								South = Player.South,
								East = Player.East,
								West = Player.West,
								North = Player.North,
							};
						}
					}

					if (Player1 != null)
					{
						CurrentPlayer1Status = GetStatus(Player1);
					}
					else CurrentPlayer1Status = null;

					if (Player2 != null)
					{
						CurrentPlayer2Status = GetStatus(Player2);
					}
					else CurrentPlayer2Status = null;

					PreviousFrame = Time.frameCount;
				}
			}

			[HarmonyPatch(typeof(GamePadInput))]
			[HarmonyPatch("GetAxis")]
			public static class GetAxisOverride
			{
				public static bool Prefix(GamePadInput __instance, ref Vector3 __result)
				{
					EnsureFrameUpdate();
					if (__instance is XBOXGamePadInput && (__instance.index == 1 ? Player1 : __instance.index == 2 ? Player2 : null) is PlayerStatus Player)
					{
						lock (Player)
						{
							if (Player?.LeftStick is Vector2 Axis)
							{
								__result.x = Axis.x;
								__result.z = Axis.y;
								return false;
							}
						}
					}
					return true;
				}
			}

			[HarmonyPatch(typeof(GamePadInput))]
			[HarmonyPatch("GetButtonDown")]
			public static class GetButtonDownOverride
			{
				public static bool Prefix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
				{
					EnsureFrameUpdate();
					if (__instance is XBOXGamePadInput &&
						(__instance.index == 1 ? CurrentPlayer1Status : __instance.index == 2 ? CurrentPlayer1Status : null) is ButtonStatus Current &&
						(__instance.index == 1 ? PreviousPlayer1Status : __instance.index == 2 ? PreviousPlayer1Status : null) is ButtonStatus Previous)
					{
						switch (mlpAction)
						{
							case MLPAction.RIGHT:
								if (Current.Right == true && Previous.Right == false)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.LEFT:
								if (Current.Left == true && Previous.Left == false)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.UP:
								if (Current.Up == true && Previous.Up == false)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.DOWN:
								if (Current.Down == true && Previous.Down == false)
								{
									__result = true;
									return false;
								}
								return false;

							case MLPAction.JUMP:
							case MLPAction.SELECT:
								if (Current.South == true && Previous.South == false)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.INTERACT:
							case MLPAction.BACK:
								if (Current.East == true && Previous.East == false)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.PAUSE:
							case MLPAction.CHANGE_ACCOUNT:
								if (Current.West == true && Previous.West == false)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.EQUIPMENT:
							case MLPAction.DELETE_ITEM:
								if (Current.North == true && Previous.North == false)
								{
									__result = true;
									return false;
								}
								break;

							case MLPAction.ANY:
								if (Current.LeftThresholdCrossed == true && Previous.LeftThresholdCrossed == false ||
									Current.RightThresholdCrossed == true && Previous.RightThresholdCrossed == false)
								{
									__result = true;
									return false;
								}
								break;
						}
					}
					return true;
				}
			}

			[HarmonyPatch(typeof(GamePadInput))]
			[HarmonyPatch("GetButtonUp")]
			public static class GetButtonUpOverride
			{
				public static bool Prefix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
				{
					EnsureFrameUpdate();
					if (__instance is XBOXGamePadInput &&
						(__instance.index == 1 ? CurrentPlayer1Status : __instance.index == 2 ? CurrentPlayer1Status : null) is ButtonStatus Current &&
						(__instance.index == 1 ? PreviousPlayer1Status : __instance.index == 2 ? PreviousPlayer1Status : null) is ButtonStatus Previous)
					{
						switch (mlpAction)
						{
							case MLPAction.RIGHT:
								if (Current.Right == false && Previous.Right == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.LEFT:
								if (Current.Left == false && Previous.Left == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.UP:
								if (Current.Up == false && Previous.Up == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.DOWN:
								if (Current.Down == false && Previous.Down == true)
								{
									__result = true;
									return false;
								}
								return false;

							case MLPAction.JUMP:
							case MLPAction.SELECT:
								if (Current.South == false && Previous.South == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.INTERACT:
							case MLPAction.BACK:
								if (Current.East == false && Previous.East == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.PAUSE:
							case MLPAction.CHANGE_ACCOUNT:
								if (Current.West == false && Previous.West == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.EQUIPMENT:
							case MLPAction.DELETE_ITEM:
								if (Current.North == false && Previous.North == true)
								{
									__result = true;
									return false;
								}
								break;

							case MLPAction.ANY:
								if (Current.LeftThresholdCrossed == false && Previous.LeftThresholdCrossed == true ||
									Current.RightThresholdCrossed == false && Previous.RightThresholdCrossed == true)
								{
									__result = true;
									return false;
								}
								break;
						}
					}
					return true;
				}
			}

			[HarmonyPatch(typeof(GamePadInput))]
			[HarmonyPatch("GetButton")]
			public static class GetButtonOverride
			{
				public static bool Prefix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
				{
					EnsureFrameUpdate();
					if (__instance is XBOXGamePadInput && (__instance.index == 1 ? CurrentPlayer1Status : __instance.index == 2 ? CurrentPlayer1Status : null) is ButtonStatus Player)
					{
						switch (mlpAction)
						{
							case MLPAction.RIGHT:
								if (Player.Right == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.LEFT:
								if (Player.Left == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.UP:
								if (Player.Up == true)
								{
									__result = true;
									return false;
								}
								return false;
							case MLPAction.DOWN:
								if (Player.Down == true)
								{
									__result = true;
									return false;
								}
								return false;

							case MLPAction.JUMP:
							case MLPAction.SELECT:
								if (Player.South == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.INTERACT:
							case MLPAction.BACK:
								if (Player.East == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.PAUSE:
							case MLPAction.CHANGE_ACCOUNT:
								if (Player.West == true)
								{
									__result = true;
									return false;
								}
								break;
							case MLPAction.EQUIPMENT:
							case MLPAction.DELETE_ITEM:
								if (Player.North == true)
								{
									__result = true;
									return false;
								}
								break;

							case MLPAction.ANY:
								if (Player.LeftThresholdCrossed == true ||
									Player.RightThresholdCrossed == true)
								{
									__result = true;
									return false;
								}
								break;
						}
					}
					return true;
				}
			}
		}

		public static class AZHM
		{
		}

		public static Vector2 ToVector2(this CameraSpacePoint P) => new Vector2(P.X, P.Y);
	}
}
