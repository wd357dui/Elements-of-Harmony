using Character;
using HarmonyLib;
using Melbot;
using System.Collections.Generic;
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
			if (Time.frameCount != PreviousFrame)
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
							Axis *= Settings.Loyalty.KinectControl.StickSensitivity;
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

		#region Pipp Pipp Dance Parade
		#endregion

		#region Zipp's Flight Academy

		[HarmonyPatch(typeof(DashThroughTheSkyMiniGame))]
		[HarmonyPatch("UpdateControls")] // controls flying position
		public class DashThroughTheSkyUpdateControlsOverride
		{
			public static bool Prefix(DashThroughTheSkyMiniGame __instance)
			{
				FlyingLevelGenerator flyingLevelGenerator = Traverse.Create(__instance).Field<FlyingLevelGenerator>("flyingLevelGenerator").Value;
				List<Transform> character1MovePoints = Traverse.Create(flyingLevelGenerator).Field<List<Transform>>("character1MovePoints").Value;
				DashThroughTheSkyLevelProceduralGenerator? multiplayer = flyingLevelGenerator as DashThroughTheSkyLevelProceduralGenerator;
				List<Transform>? character2MovePoints = multiplayer != null ? Traverse.Create(multiplayer).Field<List<Transform>>("character2MovePoints").Value : null;
				float timerResumePlayerStop = Traverse.Create(__instance).Field<float>("timerResumePlayerStop").Value;

				// code copied from the original method
				MLPCharacter component = __instance.Characters[0].GetComponent<MLPCharacter>();

				if (!flyingLevelGenerator.GetPlayerStop(1, timerResumePlayerStop))
				{
					// my modification
					Vector3? player1pos = null;
					if (Player1?.HeadTiltAngle is float Angle)
					{
						float Progress = Mathf.Clamp01(((Angle / Settings.Loyalty.KinectControl.HeadTiltMaxAngle) + 1f) / 2f);
						player1pos = Vector3.Lerp(character1MovePoints[0].position, character1MovePoints[2].position, Progress);
					}
					if (player1pos.HasValue)
					{
						DashThroughTheSkyStates dashThroughTheSkyStates1 = (DashThroughTheSkyStates)component.bodyController.states;
						GoTo(dashThroughTheSkyStates1.flying, player1pos.Value);
					}
					else if (component.input.GetButtonDown(MLPAction.RIGHT))
					{
						DashThroughTheSkyStates dashThroughTheSkyStates = (DashThroughTheSkyStates)component.bodyController.states;
						flyingLevelGenerator.MoveCharacterFromTrigger(1, dashThroughTheSkyStates.flying, 1);
					}
					else if (component.input.GetButtonDown(MLPAction.LEFT))
					{
						DashThroughTheSkyStates dashThroughTheSkyStates2 = (DashThroughTheSkyStates)component.bodyController.states;
						flyingLevelGenerator.MoveCharacterFromTrigger(1, dashThroughTheSkyStates2.flying, -1);
					}
				}

				if (flyingLevelGenerator.GetPlayerStop(2, timerResumePlayerStop))
				{
					return false;
				}

				MLPCharacter component2 = __instance.Characters[0].GetComponent<MLPCharacter>();
				if (__instance.Characters.Count > 1)
				{
					component2 = __instance.Characters[1].GetComponent<MLPCharacter>();
				}

				if (__instance.Characters.Count > 1)
				{
					// my modification
					Vector3? player2pos = null;
					if (Player2?.HeadTiltAngle is float Angle && character2MovePoints != null)
					{
						float Progress = Mathf.Clamp01(((Angle / 30.0f) + 1f) / 2f);
						player2pos = Vector3.Lerp(character2MovePoints[0].position, character2MovePoints[2].position, Progress);
					}
					if (player2pos.HasValue)
					{
						DashThroughTheSkyStates dashThroughTheSkyStates2 = (DashThroughTheSkyStates)component2.bodyController.states;
						GoTo(dashThroughTheSkyStates2.flying, player2pos.Value);
					}
					else if (component2.input.GetButtonDown(MLPAction.RIGHT))
					{
						DashThroughTheSkyStates dashThroughTheSkyStates3 = (DashThroughTheSkyStates)component2.bodyController.states;
						flyingLevelGenerator.MoveCharacterFromTrigger(2, dashThroughTheSkyStates3.flying, 1);
					}
					else if (component2.input.GetButtonDown(MLPAction.LEFT))
					{
						DashThroughTheSkyStates dashThroughTheSkyStates4 = (DashThroughTheSkyStates)component2.bodyController.states;
						flyingLevelGenerator.MoveCharacterFromTrigger(2, dashThroughTheSkyStates4.flying, -1);
					}
				}
				return false;
			}
			public static void GoTo(DashThroughTheSkyFlying __instance, Vector3 positionToGo)
			{
				Traverse<Vector3> destinationPoint = Traverse.Create(__instance).Field<Vector3>("destinationPoint");
				if (destinationPoint.Value != positionToGo)
				{
					destinationPoint.Value = positionToGo;
					MLPCharacter character = __instance.GetCharacter();
					character.GetComponent<PositionInterpolator>().InterpolateTo(destinationPoint.Value);
					character.bodyController.characterModel.animator.SetTrigger("FlyingChangeLane");
					//audioSource.PlayOneShot(changeLaneClip);
				}
			}
		}

		#endregion

		#region Sprout's Roller Blading Chase

		public static RunnerStates?[]? Runners = null;

		[HarmonyPatch(typeof(Runner1MiniGame))]
		[HarmonyPatch("StartGame")]
		public class RunnerStartGameHook
		{
			public static void Postfix(Runner1MiniGame __instance)
			{
				Runners ??= new RunnerStates?[2] {
					(RunnerStates)__instance.Characters[0].bodyController.states,
					__instance.Characters.Count > 1 ? (RunnerStates)__instance.Characters[1].bodyController.states : null
				};
			}
		}

		[HarmonyPatch(typeof(Runner1MiniGame))]
		[HarmonyPatch("UpdateGame")]
		public class RunnerUpdateGamePatch
		{
			public static void Prefix()
			{
				EnsureFrameUpdate();
				if (Player1 is PlayerStatus Player1Status && Runners?[0] is RunnerStates Runner1State)
				{
					lock (Player1Status)
					{
						if (Player1Status.LeftAnkleGroundDistance > Settings.Loyalty.KinectControl.AnkleJumpThreshold ||
							Player1Status.RightAnkleGroundDistance > Settings.Loyalty.KinectControl.AnkleJumpThreshold)
						{
							Runner1State.SetCurrrent(Runner1State.jumping);
						}
					}
				}
				if (Player2 is PlayerStatus Player2Status && Runners?[1] is RunnerStates Runner2State)
				{
					lock (Player2Status)
					{
						if (Player2Status.LeftAnkleGroundDistance > Settings.Loyalty.KinectControl.AnkleJumpThreshold ||
							Player2Status.RightAnkleGroundDistance > Settings.Loyalty.KinectControl.AnkleJumpThreshold)
						{
							Runner2State.SetCurrrent(Runner2State.jumping);
						}
					}
				}
			}
		}

		#endregion
	}
}
