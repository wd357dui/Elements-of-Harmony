using HarmonyLib;
using UnityEngine;
using static ElementsOfHarmony.KinectControl.KinectControl;

namespace ElementsOfHarmony.KinectControl.AMBA
{
	public static class KinectControl
	{
		public struct ButtonStatus
		{
			public bool? Left, Up, Right, Down,
				South, East, West, North, Menu;
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

				static ButtonStatus? GetStatus(PlayerStatus? Player)
				{
					if (Player == null) return null;
					lock (Player)
					{
						return new ButtonStatus()
						{
							Left = Player.Left,
							Up = Player.Up,
							Right = Player.Right,
							Down = Player.Down,
							South = Player.South,
							East = Player.East,
							West = Player.West,
							North = Player.North,
							Menu = Player.SouthEast,
						};
					}
				}

				CurrentPlayer1Status = GetStatus(Player1);
				CurrentPlayer2Status = GetStatus(Player2);

				PreviousFrame = Time.frameCount;
			}
		}

		#region Regular Controls

		[HarmonyPatch(typeof(GamePadInput), "GetAxis")]
		public static class GetAxisOverride
		{
			public static void Postfix(GamePadInput __instance, ref Vector3 __result)
			{
				EnsureFrameUpdate();
				if (__instance is XBOXGamePadInput &&
					(__instance.index == 0 ? Player1 : __instance.index == 1 ? Player2 : null) is PlayerStatus Player)
				{
					lock (Player)
					{
						if (Player?.LeftStick is Vector2 Axis)
						{
							Axis *= 2.5f; // adjust sensitivity
							__result.x = Axis.x;
							__result.z = Axis.y;
						}
					}
				}
			}
		}

		[HarmonyPatch(typeof(GamePadInput), "GetButtonDown")]
		public static class GetButtonDownOverride
		{
			public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
			{
				EnsureFrameUpdate();
				if (__instance is XBOXGamePadInput &&
					(__instance.index == 0 ? CurrentPlayer1Status : __instance.index == 1 ? CurrentPlayer2Status : null) is ButtonStatus Current &&
					(__instance.index == 0 ? PreviousPlayer1Status : __instance.index == 1 ? PreviousPlayer2Status : null) is ButtonStatus Previous)
				{
					switch (mlpAction)
					{
						case MLPAction.RIGHT:
							if (Current.Right == true && Previous.Right == false)
							{
								__result = true;
							}
							break;
						case MLPAction.LEFT:
							if (Current.Left == true && Previous.Left == false)
							{
								__result = true;
							}
							break;
						case MLPAction.UP:
							if (Current.Up == true && Previous.Up == false)
							{
								__result = true;
							}
							break;
						case MLPAction.DOWN:
							if (Current.Down == true && Previous.Down == false)
							{
								__result = true;
							}
							break;

						case MLPAction.JUMP:
						case MLPAction.SELECT:
							if (Current.South == true && Previous.South == false)
							{
								__result = true;
							}
							break;
						case MLPAction.INTERACT:
						case MLPAction.BACK:
							if (Current.East == true && Previous.East == false)
							{
								__result = true;
							}
							break;
						case MLPAction.PAUSE:
						case MLPAction.CHANGE_ACCOUNT:
							if (Current.West == true && Previous.West == false)
							{
								__result = true;
							}
							if (Current.Menu == true && Previous.Menu == false)
							{
								__result = true;
							}
							break;
						case MLPAction.EQUIPMENT:
						case MLPAction.DELETE_ITEM:
							if (Current.North == true && Previous.North == false)
							{
								__result = true;
							}
							break;

						case MLPAction.ANY:
							if (Current.Right == true && Previous.Right == false ||
								Current.Left == true && Previous.Left == false ||
								Current.Up == true && Previous.Up == false ||
								Current.Down == true && Previous.Down == false ||
								Current.South == true && Previous.South == false ||
								Current.East == true && Previous.East == false ||
								Current.West == true && Previous.West == false ||
								Current.North == true && Previous.North == false ||
								Current.Menu == true && Previous.Menu == false)
							{
								__result = true;
							}
							break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(GamePadInput), "GetButtonUp")]
		public static class GetButtonUpOverride
		{
			public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
			{
				EnsureFrameUpdate();
				if (__instance is XBOXGamePadInput &&
					(__instance.index == 0 ? CurrentPlayer1Status : __instance.index == 1 ? CurrentPlayer2Status : null) is ButtonStatus Current &&
					(__instance.index == 0 ? PreviousPlayer1Status : __instance.index == 1 ? PreviousPlayer2Status : null) is ButtonStatus Previous)
				{
					switch (mlpAction)
					{
						case MLPAction.RIGHT:
							if (Current.Right == false && Previous.Right == true)
							{
								__result = true;
							}
							break;
						case MLPAction.LEFT:
							if (Current.Left == false && Previous.Left == true)
							{
								__result = true;
							}
							break;
						case MLPAction.UP:
							if (Current.Up == false && Previous.Up == true)
							{
								__result = true;
							}
							break;
						case MLPAction.DOWN:
							if (Current.Down == false && Previous.Down == true)
							{
								__result = true;
							}
							break;

						case MLPAction.JUMP:
						case MLPAction.SELECT:
							if (Current.South == false && Previous.South == true)
							{
								__result = true;
							}
							break;
						case MLPAction.INTERACT:
						case MLPAction.BACK:
							if (Current.East == false && Previous.East == true)
							{
								__result = true;
							}
							break;
						case MLPAction.PAUSE:
						case MLPAction.CHANGE_ACCOUNT:
							if (Current.West == false && Previous.West == true)
							{
								__result = true;
							}
							if (Current.Menu == false && Previous.Menu == true)
							{
								__result = true;
							}
							break;
						case MLPAction.EQUIPMENT:
						case MLPAction.DELETE_ITEM:
							if (Current.North == false && Previous.North == true)
							{
								__result = true;
							}
							break;

						case MLPAction.ANY:
							if (Current.Right == false && Previous.Right == true ||
								Current.Left == false && Previous.Left == true ||
								Current.Up == false && Previous.Up == true ||
								Current.Down == false && Previous.Down == true ||
								Current.South == false && Previous.South == true ||
								Current.East == false && Previous.East == true ||
								Current.West == false && Previous.West == true ||
								Current.North == false && Previous.North == true ||
								Current.Menu == false && Previous.Menu == true)
							{
								__result = true;
							}
							break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(GamePadInput), "GetButton")]
		public static class GetButtonOverride
		{
			public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
			{
				EnsureFrameUpdate();
				if (__instance is XBOXGamePadInput &&
					(__instance.index == 0 ? CurrentPlayer1Status : __instance.index == 1 ? CurrentPlayer2Status : null) is ButtonStatus Player)
				{
					switch (mlpAction)
					{
						case MLPAction.RIGHT:
							if (Player.Right == true)
							{
								__result = true;
							}
							break;
						case MLPAction.LEFT:
							if (Player.Left == true)
							{
								__result = true;
							}
							break;
						case MLPAction.UP:
							if (Player.Up == true)
							{
								__result = true;
							}
							break;
						case MLPAction.DOWN:
							if (Player.Down == true)
							{
								__result = true;
							}
							break;

						case MLPAction.JUMP:
						case MLPAction.SELECT:
							if (Player.South == true)
							{
								__result = true;
							}
							break;
						case MLPAction.INTERACT:
						case MLPAction.BACK:
							if (Player.East == true)
							{
								__result = true;
							}
							break;
						case MLPAction.PAUSE:
						case MLPAction.CHANGE_ACCOUNT:
							if (Player.West == true)
							{
								__result = true;
							}
							if (Player.Menu == true)
							{
								__result = true;
							}
							break;
						case MLPAction.EQUIPMENT:
						case MLPAction.DELETE_ITEM:
							if (Player.North == true)
							{
								__result = true;
							}
							break;

						case MLPAction.ANY:
							if (Player.Right == true ||
								Player.Left == true ||
								Player.Up == true ||
								Player.Down == true ||
								Player.South == true ||
								Player.East == true ||
								Player.West == true ||
								Player.North == true ||
								Player.Menu == true)
							{
								__result = true;
							}
							break;
					}
				}
			}
		}

		#endregion
	}
}
