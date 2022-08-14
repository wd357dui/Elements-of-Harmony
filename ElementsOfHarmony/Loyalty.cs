using Character;
using HarmonyLib;
using Melbot;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace ElementsOfHarmony
{
    // this is a motion control sub-mod using Microsoft Kinect V2
    // I may be planning on adding more sub-mods in the future, the sub-mods shouldn't have dependencies on each other if they're not related
    // which is going to be problematic because I don't have an idea on how to properly seperate/combine them
    // I'm a little unwilling to create new DLLs (because that would require extra work to setup the mod) (but this is prbably what I'm going to end up with in the end)
    // I don't want to create new github repos either, I want to keep sub-mods as branches if possible
    // also I'm still struggling to understand github branches & stuff
    public class Loyalty
    {
        public static void LogMessage(string Message)
        {
            ElementsOfHarmony.LogMessage(Message);
        }

        // I didn't have the time to add comments right now
        public class KinectUpdate
        {
            public const int BodyCount = 6;
            public const int JointCount = 25;
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
            public enum HandState
            {
                Unknown,
                NotTracked,
                Open,
                Closed,
                Lasso
            }
            public enum TrackingConfidence
            {
                Low,
                High,
            }

            [DllImport("KinectProxy.dll")]
            public extern static int KinectInit();

            [DllImport("KinectProxy.dll")]
            public extern static int AcquireLatestFrame();

            [DllImport("KinectProxy.dll")]
            public extern static long GetFrameTime();

            [DllImport("KinectProxy.dll")]
            public extern static bool GetBodyTracked(int body);

            [DllImport("KinectProxy.dll")]
            public extern static float GetJointPosX(int body, int joint);

            [DllImport("KinectProxy.dll")]
            public extern static float GetJointPosY(int body, int joint);

            [DllImport("KinectProxy.dll")]
            public extern static float GetJointPosZ(int body, int joint);

            [DllImport("KinectProxy.dll")]
            public extern static int GetHandStateLeft(int body);

            [DllImport("KinectProxy.dll")]
            public extern static int GetHandStateRight(int body);

            [DllImport("KinectProxy.dll")]
            public extern static int GetHandConfidenceLeft(int body);

            [DllImport("KinectProxy.dll")]
            public extern static int GetHandConfidenceRight(int body);

            [DllImport("KinectProxy.dll")]
            public extern static float GetLeftFootOnTheGround(int body, float floorElevation);

            [DllImport("KinectProxy.dll")]
            public extern static float GetRightFootOnTheGround(int body, float floorElevation);

            [DllImport("KinectProxy.dll")]
            public extern static float GetLeftAnkleOnTheGround(int body, float floorElevation);

            [DllImport("KinectProxy.dll")]
            public extern static float GetRightAnkleOnTheGround(int body, float floorElevation);

            [DllImport("KinectProxy.dll")]
            public extern static int GetJointState(int body, int joint);
            public const uint E_PENDING = 0x8000000A;

            public static int?[] Players = new int?[2];
            public static Thread UpdateThread;
            public static Mutex UpdateMutex = new Mutex();
            public static long FrameTime = 0;

            public static void Begin()
            {
                UpdateThread = new Thread(() =>
                {
                    int hresult = KinectInit();
                    if (hresult != 0) Marshal.GetExceptionForHR(hresult);
                    else while (true)
                        {
                            Update();
                            Thread.Sleep(1);
                        }
                });
                UpdateThread.Start();
            }
            public static void Update()
            {
                int hresult = AcquireLatestFrame();
                if (hresult == -1) return;
                else if (hresult != 0 && (uint)hresult != E_PENDING) LogMessage("error - hresult: 0x" + hresult.ToString("X8"));
                else
                {
                    long NewFrameTime = GetFrameTime();
                    if (NewFrameTime != FrameTime)
                    {
                        for (int body = 0; body < BodyCount; body++)
                        {
                            if (GetBodyTracked(body))
                            {
                                if (Players[1] != body && !Players[0].HasValue)
                                {
                                    Players[0] = body;
                                }
                                else if (Players[0] != body && !Players[1].HasValue)
                                {
                                    Players[1] = body;
                                }
                            }
                            else
                            {
                                if (Players[0] == body)
                                {
                                    PlayerLeftHandHolding[0] = PlayerRightHandHolding[0] = false;
                                    Players[0] = null;
                                }
                                if (Players[1] == body)
                                {
                                    PlayerLeftHandHolding[1] = PlayerRightHandHolding[1] = false;
                                    Players[1] = null;
                                }
                            }
                        }
                        UpdateMutex.WaitOne();
                        ProcessPlayer(0);
                        ProcessPlayer(1);
                        FrameTime = NewFrameTime;
                        UpdateMutex.ReleaseMutex();
                    }
                }
            }
            public static bool[] PlayerLeftHandHolding = new bool[2];
            public static bool[] PlayerRightHandHolding = new bool[2];
            public static bool IsLeftHandHolding(int index)
            {
                HandState State = (HandState)GetHandStateLeft(Players[index].Value);
                TrackingConfidence Confidence = (TrackingConfidence)GetHandConfidenceLeft(Players[index].Value);
                if (PlayerLeftHandHolding[index])
                {
                    if (State == HandState.Open)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return State == HandState.Closed;
                }
            }
            public static bool IsRightHandHolding(int index)
            {
                HandState State = (HandState)GetHandStateRight(Players[index].Value);
                TrackingConfidence Confidence = (TrackingConfidence)GetHandConfidenceRight(Players[index].Value);
                if (PlayerRightHandHolding[index])
                {
                    if (State == HandState.Open)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return State == HandState.Closed;
                }
            }
            public static bool IsLeftHandCloserToUpperBody(int body)
            {
                Vector2 LeftHand = new Vector2(GetJointPosX(body, (int)JointType.HandLeft), GetJointPosY(body, (int)JointType.HandLeft));
                Vector2 SpineBase = new Vector2(GetJointPosX(body, (int)JointType.SpineBase), GetJointPosY(body, (int)JointType.SpineBase));
                Vector2 SpineShoulder = new Vector2(GetJointPosX(body, (int)JointType.SpineShoulder), GetJointPosY(body, (int)JointType.SpineShoulder));
                return (LeftHand - SpineShoulder).magnitude < (LeftHand - SpineBase).magnitude;
            }
            public static bool IsRightHandCloserToUpperBody(int body)
            {
                Vector2 RightHand = new Vector2(GetJointPosX(body, (int)JointType.HandRight), GetJointPosY(body, (int)JointType.HandRight));
                Vector2 SpineBase = new Vector2(GetJointPosX(body, (int)JointType.SpineBase), GetJointPosY(body, (int)JointType.SpineBase));
                Vector2 SpineShoulder = new Vector2(GetJointPosX(body, (int)JointType.SpineShoulder), GetJointPosY(body, (int)JointType.SpineShoulder));
                return (RightHand - SpineShoulder).magnitude < (RightHand - SpineBase).magnitude;
            }
            public static bool[] FrameAcknowledged = new bool[2];
            public static void AcknowledgeFrame(int index)
            {
                FrameAcknowledged[index] = true;
            }
            public static void ProcessPlayer(int index)
            {
                if (FrameAcknowledged[index])
                {
                    Right_[index] = Right[index];
                    Left_[index] = Left[index];
                    Up_[index] = Up[index];
                    Down_[index] = Down[index];

                    A_[index] = A[index];
                    B_[index] = B[index];
                    Y_[index] = Y[index];
                    Start_[index] = Start[index];
                    FrameAcknowledged[index] = false;
                }

                if (Players[index].HasValue)
                {
                    if (IsLeftHandHolding(index))
                    {
                        if (!PlayerLeftHandHolding[index])
                        {
                            if (IsLeftHandCloserToUpperBody(Players[index].Value))
                            {
                                RecordLeftStartPos(index);
                                MoveLeftAxis(index);
                                PlayerLeftHandHolding[index] = true;
                            }
                        }
                        else
                        {
                            MoveLeftAxis(index);
                            PlayerLeftHandHolding[index] = true;
                        }
                    }
                    else
                    {
                        if (PlayerLeftHandHolding[index])
                        {
                            ReleaseLeftAxis(index);
                        }
                        PlayerLeftHandHolding[index] = false;
                    }
                    if (IsRightHandHolding(index))
                    {
                        if (!PlayerRightHandHolding[index])
                        {
                            if (IsRightHandCloserToUpperBody(Players[index].Value))
                            {
                                RecordRightStartPos(index);
                                MoveRightAxis(index);
                                PlayerRightHandHolding[index] = true;
                            }
                        }
                        else
                        {
                            MoveRightAxis(index);
                            PlayerRightHandHolding[index] = true;
                        }
                    }
                    else
                    {
                        if (PlayerRightHandHolding[index])
                        {
                            ReleaseRightAxis(index);
                        }
                        PlayerRightHandHolding[index] = false;
                    }
                }
                else
                {
                    Right[index] = Left[index] = Up[index] = Down[index] = false;
                    A[index] = B[index] = Y[index] = Start[index] = false;
                }
                OnProcessPlayer?.Invoke(index);
            }
            public static event Action<int> OnProcessPlayer;

            public static Vector2[] LeftAxisStartPos = new Vector2[2], RightAxisStartPos = new Vector2[2];
            public static Vector2[] LeftAxisPos = new Vector2[2], RightAxisPos = new Vector2[2];
            public static Vector2[] LeftAxisCoord = new Vector2[2], RightAxisCoord = new Vector2[2];
            public static bool[] Right = new bool[2], Left = new bool[2], Up = new bool[2], Down = new bool[2];
            public static bool[] Right_ = new bool[2], Left_ = new bool[2], Up_ = new bool[2], Down_ = new bool[2];
            public static void RecordLeftStartPos(int index)
            {
                LeftAxisStartPos[index].x = GetJointPosX(Players[index].Value, (int)JointType.HandLeft);
                LeftAxisStartPos[index].y = GetJointPosY(Players[index].Value, (int)JointType.HandLeft);
                LogMessage("RecordLeftStartPos X=" + LeftAxisStartPos[index].x + " Y=" + LeftAxisStartPos[index].y);
            }
            public static void MoveLeftAxis(int index)
            {
                LeftAxisPos[index].x = GetJointPosX(Players[index].Value, (int)JointType.HandLeft);
                LeftAxisPos[index].y = GetJointPosY(Players[index].Value, (int)JointType.HandLeft);
                LeftAxisCoord[index] = (LeftAxisPos[index] - LeftAxisStartPos[index]) / 0.2f;

                Right[index] = LeftAxisCoord[index].x > 0.3f;
                Left[index] = LeftAxisCoord[index].x < -0.3f;
                Up[index] = LeftAxisCoord[index].y > 0.3f;
                Down[index] = LeftAxisCoord[index].y < -0.3f;
            }
            public static void ReleaseLeftAxis(int index)
            {
                LeftAxisCoord[index].x = 0.0f;
                LeftAxisCoord[index].y = 0.0f;
                Right[index] = Left[index] = Up[index] = Down[index] = false;
                LogMessage("ReleaseLeftAxis");
            }
            public static bool[] A = new bool[2], B = new bool[2], Y = new bool[2], Start = new bool[2];
            public static bool[] A_ = new bool[2], B_ = new bool[2], Y_ = new bool[2], Start_ = new bool[2];
            public static void RecordRightStartPos(int index)
            {
                RightAxisStartPos[index].x = GetJointPosX(Players[index].Value, (int)JointType.HandRight);
                RightAxisStartPos[index].y = GetJointPosY(Players[index].Value, (int)JointType.HandRight);
                LogMessage("RecordRightStartPos X=" + RightAxisStartPos[index].x + " Y=" + RightAxisStartPos[index].y);
            }
            public static void MoveRightAxis(int index)
            {
                RightAxisPos[index].x = GetJointPosX(Players[index].Value, (int)JointType.HandRight);
                RightAxisPos[index].y = GetJointPosY(Players[index].Value, (int)JointType.HandRight);
                RightAxisCoord[index] = (RightAxisPos[index] - RightAxisStartPos[index]) / 0.2f;

                bool Threshold = RightAxisCoord[index].magnitude >= 0.3f;
                A[index] = Threshold && RightAxisCoord[index].y < 0.0f && RightAxisCoord[index].y < -Math.Abs(RightAxisCoord[index].x);
                B[index] = Threshold && RightAxisCoord[index].x > 0.0f && RightAxisCoord[index].x > Math.Abs(RightAxisCoord[index].y);
                Y[index] = Threshold && RightAxisCoord[index].y > 0.0f && RightAxisCoord[index].y > Math.Abs(RightAxisCoord[index].x);
                Start[index] = Threshold && RightAxisCoord[index].x < 0.0f && RightAxisCoord[index].x < -Math.Abs(RightAxisCoord[index].y);
            }
            public static void ReleaseRightAxis(int index)
            {
                RightAxisCoord[index].x = 0.0f;
                RightAxisCoord[index].y = 0.0f;
                A[index] = B[index] = Y[index] = Start[index] = false;
                LogMessage("ReleaseRightAxis");
            }
            public static bool IsPressedPreviousFrame(int index, MLPAction mlpAction)
            {
                bool result = false;
                switch (mlpAction)
                {
                    case MLPAction.RIGHT:
                        return Right_[index];
                    case MLPAction.LEFT:
                        return Left_[index];
                    case MLPAction.UP:
                        return Up_[index];
                    case MLPAction.DOWN:
                        return Down_[index];
                    case MLPAction.JUMP:
                    case MLPAction.SELECT:
                        return A_[index];
                    case MLPAction.INTERACT:
                    case MLPAction.BACK:
                        return B_[index];
                    case MLPAction.PAUSE:
                    case MLPAction.CHANGE_ACCOUNT:
                        return Start_[index];
                    case MLPAction.MENULEFT:
                        return false;
                    case MLPAction.MENURIGHT:
                        return false;
                    case MLPAction.EQUIPMENT:
                    case MLPAction.DELETE_ITEM:
                        return Y_[index];
                    case MLPAction.ANY:
                        return A_[index] || B_[index] || Y_[index] || Start_[index] || Right_[index] || Left_[index] || Up_[index] || Down_[index];
                }
                return result;
            }
            public static bool IsPressedCurrentFrame(int index, MLPAction mlpAction)
            {
                bool result = false;
                switch (mlpAction)
                {
                    case MLPAction.RIGHT:
                        result = Right[index];
                        break;
                    case MLPAction.LEFT:
                        result = Left[index];
                        break;
                    case MLPAction.UP:
                        result = Up[index];
                        break;
                    case MLPAction.DOWN:
                        result = Down[index];
                        break;
                    case MLPAction.JUMP:
                    case MLPAction.SELECT:
                        result = A[index];
                        break;
                    case MLPAction.INTERACT:
                    case MLPAction.BACK:
                        result = B[index];
                        break;
                    case MLPAction.PAUSE:
                    case MLPAction.CHANGE_ACCOUNT:
                        result = Start[index];
                        break;
                    case MLPAction.MENULEFT:
                        result = false;
                        break;
                    case MLPAction.MENURIGHT:
                        result = false;
                        break;
                    case MLPAction.EQUIPMENT:
                    case MLPAction.DELETE_ITEM:
                        result = Y[index];
                        break;
                    case MLPAction.ANY:
                        result = A[index] || B[index] || Y[index] || Start[index] || Right[index] || Left[index] || Up[index] || Down[index];
                        break;
                }
                return result;
            }
            public static Vector2 GetAxis(int index)
            {
                return LeftAxisCoord[index];
            }
        }

        [HarmonyPatch(typeof(PlayerInput))]
        [HarmonyPatch("Awake")]
        public class PlayerInputAwakeHook
        {
            public static bool HasInit = false;
            public static void Postfix()
            {
                if (!HasInit)
                {
                    KinectUpdate.Begin();
                    HasInit = true;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerInput))]
        [HarmonyPatch("Update")]
        public class PlayerInputUpdateHook
        {
            public static bool HasUpdateThisFrame = false;
            public static long PreviousFrameTime = 0;
            public static int PreviousFrame = 0;
            public static void Prefix(PlayerInput __instance, ref int __state)
            {
                __state = GetCurrentPlayer(__instance);
                if (__state != -1)
                {
                    KinectUpdate.UpdateMutex.WaitOne();
                    int NewFrameCount = Time.frameCount;
                    if (PreviousFrame != NewFrameCount)
                    {
                        PreviousFrame = NewFrameCount;
                        if (PreviousFrameTime != KinectUpdate.FrameTime)
                        {
                            PreviousFrameTime = KinectUpdate.FrameTime;
                            HasUpdateThisFrame = true;
                        }
                        else
                        {
                            HasUpdateThisFrame = false;
                        }
                    }
                    KinectUpdate.AcknowledgeFrame(__state);
                    OnUpdate?.Invoke(__state, HasUpdateThisFrame);
                }
            }
            public static void Postfix(ref int __state)
            {
                if (__state != -1)
                {
                    KinectUpdate.UpdateMutex.ReleaseMutex();
                }
            }
            public static int GetCurrentPlayer(PlayerInput __instance)
            {
                foreach (MLPInput input in __instance.inputs)
                {
                    GamePadInput gamepad = input as GamePadInput;
                    if (gamepad != null) return gamepad.index;
                }
                return -1;
            }
            public static event Action<int, bool> OnUpdate;
        }

        [HarmonyPatch(typeof(GamePadInput))]
        [HarmonyPatch("GetAxis")]
        public class GetAxisOverride
        {
            public static void Postfix(GamePadInput __instance, ref Vector3 __result)
            {
                if (__instance is XBOXGamePadInput && KinectUpdate.Players[__instance.index].HasValue)
                {
                    Vector2 XZ = KinectUpdate.GetAxis(__instance.index);
                    __result.x = XZ.x;
                    __result.z = XZ.y;
                }
            }
        }

        [HarmonyPatch(typeof(GamePadInput))]
        [HarmonyPatch("GetButtonDown")]
        public class GetButtonDownOverride
        {
            public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
            {
                if (__instance is XBOXGamePadInput && PlayerInputUpdateHook.HasUpdateThisFrame)
                {
                    if (!KinectUpdate.IsPressedPreviousFrame(__instance.index, mlpAction) && KinectUpdate.IsPressedCurrentFrame(__instance.index, mlpAction))
                    {
                        LogMessage("GetButtonDown" + " player " + (__instance.index + 1) + " action " + Enum.GetName(typeof(MLPAction), mlpAction));
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GamePadInput))]
        [HarmonyPatch("GetButtonUp")]
        public class GetButtonUpOverride
        {
            public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
            {
                if (__instance is XBOXGamePadInput && PlayerInputUpdateHook.HasUpdateThisFrame)
                {
                    if (KinectUpdate.IsPressedPreviousFrame(__instance.index, mlpAction) && !KinectUpdate.IsPressedCurrentFrame(__instance.index, mlpAction))
                    {
                        LogMessage("GetButtonUp" + " player " + (__instance.index + 1) + " action " + Enum.GetName(typeof(MLPAction), mlpAction));
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GamePadInput))]
        [HarmonyPatch("GetButton")]
        public class GetButtonOverride
        {
            public static void Postfix(ref bool __result, GamePadInput __instance, MLPAction mlpAction)
            {
                if (__instance is XBOXGamePadInput && KinectUpdate.IsPressedCurrentFrame(__instance.index, mlpAction))
                {
                    __result = true;
                }
            }
        }

        #region Pipp Pipp Dance Parade

        [HarmonyPatch(typeof(FashionShowCombo))]
        [HarmonyPatch("Start")]
        class FashionShowComboStartHook
        {
            public static void Postfix(FashionShowCombo __instance)
            {
                KinectUpdate.OnProcessPlayer += KinectUpdate_OnProcessPlayer;
                PlayerInputUpdateHook.OnUpdate += PlayerInputUpdateHook_OnUpdate;
            }

            public static void KinectUpdate_OnProcessPlayer(int index)
            {
                if (KinectUpdate.Players[index] == null)
                {
                    PlayerTracked[index] = false;
                }
                else
                {
                    PlayerTracked[index] = true;
                    int body = KinectUpdate.Players[index].Value;
                    LeftFootOnTheGround[index] = KinectUpdate.GetLeftFootOnTheGround(body, 0.3f) > 0.0f;
                    RightFootOnTheGround[index] = KinectUpdate.GetRightFootOnTheGround(body, 0.3f) > 0.0f;
                    LeftAnkleOnTheGround[index] = KinectUpdate.GetLeftAnkleOnTheGround(body, 0.3f) > 0.0f;
                    RightAnkleOnTheGround[index] = KinectUpdate.GetRightAnkleOnTheGround(body, 0.3f) > 0.0f;
                    SpineBase[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.SpineBase),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.SpineBase),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.SpineBase));
                    LeftHip[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.HipLeft),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.HipLeft),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.HipLeft));
                    RightHip[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.HipRight),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.HipRight),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.HipRight));
                    LeftKnee[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.KneeLeft),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.KneeLeft),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.KneeLeft));
                    RightKnee[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.KneeRight),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.KneeRight),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.KneeRight));
                    LeftAnkle[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.AnkleLeft),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.AnkleLeft),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.AnkleLeft));
                    RightAnkle[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.AnkleRight),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.AnkleRight),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.AnkleRight));
                    LeftFootPos[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.FootLeft),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.FootLeft),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.FootLeft));
                    RightFootPos[index] = new Vector3(
                        KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.FootRight),
                        KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.FootRight),
                        KinectUpdate.GetJointPosZ(body, (int)KinectUpdate.JointType.FootRight));
                }
            }
            public static bool[] PlayerTracked = new bool[2];
            public static bool[] LeftFootOnTheGround = new bool[2], RightFootOnTheGround = new bool[2];
            public static bool[] LeftAnkleOnTheGround = new bool[2], RightAnkleOnTheGround = new bool[2];
            public static Vector3[] SpineBase = new Vector3[2];
            public static Vector3[] LeftHip = new Vector3[2], RightHip = new Vector3[2];
            public static Vector3[] LeftKnee = new Vector3[2], RightKnee = new Vector3[2];
            public static Vector3[] LeftAnkle = new Vector3[2], RightAnkle = new Vector3[2];
            public static Vector3[] LeftFootPos = new Vector3[2], RightFootPos = new Vector3[2];

            public static void PlayerInputUpdateHook_OnUpdate(int player, bool HasUpdateThisFrame)
            {
                if (HasUpdateThisFrame)
                {
                    if (PlayerTracked[player])
                    {
                        if (!PlayerTracked_[player])
                        {
                            LeftFootOnTheGround_[player] = LeftFootOnTheGround[player];
                            RightFootOnTheGround_[player] = RightFootOnTheGround[player];
                            LeftAnkleOnTheGround_[player] = LeftAnkleOnTheGround[player];
                            RightAnkleOnTheGround_[player] = RightAnkleOnTheGround[player];
                            SpineBase_[player] = SpineBase[player];
                            LeftHip_[player] = LeftHip[player];
                            RightHip_[player] = RightHip[player];
                            LeftKnee_[player] = LeftKnee[player];
                            RightKnee_[player] = RightKnee[player];
                            LeftAnkle_[player] = LeftAnkle[player];
                            RightAnkle_[player] = RightAnkle[player];
                            LeftFootPos_[player] = LeftFootPos[player];
                            RightFootPos_[player] = RightFootPos[player];
                        }
                        PlayerTracked_[player] = PlayerTracked[player];

                        Action<FashionShowCombo> LeftFootCallback = null;
                        Action<FashionShowCombo> RightFootCallback = null;

                        MLPAction LeftFootAction = TestArea(LeftHip[player], SpineBase[player], LeftAnkle[player]);
                        MLPAction RightFootAction = TestArea(RightHip[player], SpineBase[player], RightAnkle[player]);
                        switch (LeftFootAction)
                        {
                            case MLPAction.RIGHT:
                            case MLPAction.LEFT:
                            case MLPAction.UP:
                            case MLPAction.DOWN:
                                if (!LeftAnkleOnTheGround_[player] && LeftAnkleOnTheGround[player])
                                {
                                    LogMessage("Left Foot " + LeftFootAction);
                                    FashionShowComboUpdateOverride.OnPlayerFootDown[player] += (FashionShowCombo __instance) =>
                                    {
                                        FashionShowComboCheckActionOverride.Forgiving(__instance, LeftFootAction);
                                    };
                                }
                                break;
                            case MLPAction.INTERACT:
                                if (!LeftAnkleOnTheGround_[player] && LeftAnkleOnTheGround[player])
                                {
                                    LogMessage("Left Foot " + LeftFootAction);
                                    FashionShowComboUpdateOverride.OnPlayerFootDown[player] += (FashionShowCombo __instance) =>
                                    {
                                        FashionShowComboCheckActionOverride.Forgiving(__instance, LeftFootAction);
                                    };
                                }
                                break;
                        }
                        switch (RightFootAction)
                        {
                            case MLPAction.RIGHT:
                            case MLPAction.LEFT:
                            case MLPAction.UP:
                            case MLPAction.DOWN:
                                if (!RightAnkleOnTheGround_[player] && RightAnkleOnTheGround[player])
                                {
                                    LogMessage("Right Foot " + RightFootAction);
                                    FashionShowComboUpdateOverride.OnPlayerFootDown[player] += (FashionShowCombo __instance) =>
                                    {
                                        FashionShowComboCheckActionOverride.Forgiving(__instance, RightFootAction);
                                    };
                                }
                                break;
                            case MLPAction.INTERACT:
                                if (!RightAnkleOnTheGround_[player] && RightAnkleOnTheGround[player])
                                {
                                    LogMessage("Right Foot " + RightFootAction);
                                    FashionShowComboUpdateOverride.OnPlayerFootDown[player] += (FashionShowCombo __instance) =>
                                    {
                                        FashionShowComboCheckActionOverride.Forgiving(__instance, RightFootAction);
                                    };
                                }
                                break;
                        }

                        LeftFootOnTheGround_[player] = LeftFootOnTheGround[player];
                        RightFootOnTheGround_[player] = RightFootOnTheGround[player];
                        LeftAnkleOnTheGround_[player] = LeftAnkleOnTheGround[player];
                        RightAnkleOnTheGround_[player] = RightAnkleOnTheGround[player];
                        SpineBase_[player] = SpineBase[player];
                        LeftHip_[player] = LeftHip[player];
                        RightHip_[player] = RightHip[player];
                        LeftKnee_[player] = LeftKnee[player];
                        RightKnee_[player] = RightKnee[player];
                        LeftAnkle_[player] = LeftAnkle[player];
                        RightAnkle_[player] = RightAnkle[player];
                        LeftFootPos_[player] = LeftFootPos[player];
                        RightFootPos_[player] = RightFootPos[player];
                        LeftFootAction_[player] = LeftFootAction;
                        RightFootAction_[player] = RightFootAction;

                        FashionShowComboUpdateOverride.DanceMutex.WaitOne();
                        FashionShowComboUpdateOverride.OnPlayerFootDown[player] += LeftFootCallback;
                        FashionShowComboUpdateOverride.OnPlayerFootDown[player] += RightFootCallback;
                        FashionShowComboUpdateOverride.DanceMutex.ReleaseMutex();
                    }
                    else
                    {
                        LeftFootAction_[player] = RightFootAction_[player] = null;
                    }
                }
            }
            public static bool[] PlayerTracked_ = new bool[2];
            public static bool[] LeftFootOnTheGround_ = new bool[2], RightFootOnTheGround_ = new bool[2];
            public static bool[] LeftAnkleOnTheGround_ = new bool[2], RightAnkleOnTheGround_ = new bool[2];
            public static Vector3[] SpineBase_ = new Vector3[2];
            public static Vector3[] LeftHip_ = new Vector3[2], RightHip_ = new Vector3[2];
            public static Vector3[] LeftKnee_ = new Vector3[2], RightKnee_ = new Vector3[2];
            public static Vector3[] LeftAnkle_ = new Vector3[2], RightAnkle_ = new Vector3[2];
            public static Vector3[] LeftFootPos_ = new Vector3[2], RightFootPos_ = new Vector3[2];
            public static MLPAction?[] LeftFootAction_ = new MLPAction?[2], RightFootAction_ = new MLPAction?[2];

            public static MLPAction TestArea(Vector3 Hip, Vector3 SpineBase, Vector3 Ankle)
            {
                float DeviationForwardBackward = Ankle.z - Hip.z;
                float DeviationLeftRight = Ankle.x - SpineBase.x;
                if (DeviationForwardBackward > 0.3f) return MLPAction.DOWN;
                else if (DeviationForwardBackward < 0.0f) return MLPAction.UP;
                else if (DeviationLeftRight > 0.2f) return MLPAction.RIGHT;
                else if (DeviationLeftRight < -0.2f) return MLPAction.LEFT;
                else return MLPAction.INTERACT;
            }
        }

        [HarmonyPatch(typeof(FashionShowCombo))]
        [HarmonyPatch("CheckAction")] // triggers when a key is pressed, to check if it's the correct key
        class FashionShowComboCheckActionOverride
        {
            public static bool Prefix(FashionShowCombo __instance, MLPAction action)
            {
                // code copied from the original method
                if (!Traverse.Create(__instance.playerCombo.fashionShowMinigame).Field<bool>("playing").Value)
                {
                    return false;
                }

                Signal current = __instance.signalCombo.GetCurrent();
                if (!(null != current) || !current.gameObject.activeSelf)
                {
                    return false;
                }

                float num = Vector3.Distance(current.finalPosition, current.transform.position);
                float num2 = Vector3.Distance(current.finalPosition, Traverse.Create(
                    Traverse.Create(current).Field<SignalLauncher>("signalLauncher").Value
                    ).Field<Transform>("hitAreaStart").Value.position);
                if (!(num <= num2))
                {
                    return false;
                }

                if (MLPAction.ANY != current.action)
                {
                    if (action == current.action)
                    {
                        __instance.onSignalHit(__instance, __instance.signalCombo, current);
                        return false;
                    }
                    __instance.StopEmojis();
                    __instance.onSignalFailed(__instance, __instance.signalCombo, current);
                }
                else
                {
                    if (!__instance.kids.isPlaying)
                    {
                        __instance.kids.Play();
                    }

                    _ = 6; // <- I don't know the purpose of this code, maybe someone added a variable set to MLPAction.INTERACT and forgot about
                }
                return false;
            }
            public static void Forgiving(FashionShowCombo __instance, MLPAction action)
            {
                // code copied from the original method
                if (!Traverse.Create(__instance.playerCombo.fashionShowMinigame).Field<bool>("playing").Value)
                {
                    return;
                }

                Signal current = __instance.signalCombo.GetCurrent();
                if (!(null != current) || !current.gameObject.activeSelf)
                {
                    return;
                }

                float num = Vector3.Distance(current.finalPosition, current.transform.position);
                float num2 = Vector3.Distance(current.finalPosition, Traverse.Create(
                    Traverse.Create(current).Field<SignalLauncher>("signalLauncher").Value
                    ).Field<Transform>("hitAreaStart").Value.position);
                if (!(num <= num2))
                {
                    return;
                }

                if (MLPAction.ANY != current.action)
                {
                    if (action == current.action || MLPAction.INTERACT == current.action)
                    {
                        __instance.onSignalHit(__instance, __instance.signalCombo, current);
                        return;
                    }
                    // forgiving: don't do anything if it's the wrong key
                    //__instance.StopEmojis();
                    //__instance.onSignalFailed(__instance, __instance.signalCombo, current);
                }
                else
                {
                    if (!__instance.kids.isPlaying)
                    {
                        __instance.kids.Play();
                    }
                }
                return;
            }
        }

        [HarmonyPatch(typeof(FashionShowCombo))]
        [HarmonyPatch("Update")] // triggers per tick to check if a large bar of "A" is being hit
        public class FashionShowComboUpdateOverride
        {
            public static bool Prefix(FashionShowCombo __instance)
            {
                Traverse<float> lastLargeHit = Traverse.Create(__instance).Field<float>("lastLargeHit");
                ParticleSystem repeat = Traverse.Create(__instance).Field<ParticleSystem>("repeat").Value;
                int player = 0;
                if (__instance.playerCombo.fashionShowMinigame.playerCombo_1 == __instance.playerCombo)
                {
                    player = 1;
                }

                if (Traverse.Create(__instance.playerCombo.fashionShowMinigame).Field<bool>("playing").Value)
                {
                    Signal current = __instance.signalCombo.GetCurrent();
                    if (null != current && Vector3.Distance(current.transform.position, __instance.signalCombo.signalLauncher.center.position) < 0.5f && MLPAction.ANY == current.action &&
                        (__instance.input.GetButton(MLPAction.ANY) || FashionShowComboStartHook.LeftFootOnTheGround[player] || FashionShowComboStartHook.RightFootOnTheGround[player]))
                    {
                        // if player's foot is on the ground then the bar is considered being hit
                        if (Time.time - lastLargeHit.Value >= __instance.signalCombo.configuration.beat / 4f)
                        {
                            lastLargeHit.Value = Time.time;
                            __instance.onLargeSignalHit(__instance, __instance.signalCombo, current);
                        }

                        if (!repeat.isPlaying)
                        {
                            repeat.Play();
                        }
                    }

                    if (repeat.isPlaying && Time.time - lastLargeHit.Value >= __instance.signalCombo.configuration.beat / 2f)
                    {
                        repeat.Stop();
                    }
                    Process(__instance, player);
                }
                else if (repeat.isPlaying)
                {
                    repeat.Stop();
                }
                return false;
            }
            public static void Process(FashionShowCombo __instance, int player)
            {
                DanceMutex.WaitOne();
                OnPlayerFootDown[player]?.Invoke(__instance);
                OnPlayerFootDown[player] = null;
                DanceMutex.ReleaseMutex();
            }
            public static Mutex DanceMutex = new Mutex();
            public static Action<FashionShowCombo>[] OnPlayerFootDown = new Action<FashionShowCombo>[2];
        }

        [HarmonyPatch(typeof(SignalCombo))]
        [HarmonyPatch("Update")]
        class SignalComboUpdateOverride
        {
            public static bool Prefix(SignalCombo __instance)
            {
                Traverse<int> nextPunch = Traverse.Create(__instance).Field<int>("nextPunch");
                Traverse<int> total = Traverse.Create(__instance).Field<int>("total");
                int player = 0;
                if (__instance.fashionShowCombo.playerCombo.fashionShowMinigame.playerCombo_1 == __instance.fashionShowCombo.playerCombo)
                {
                    player = 1;
                }

                if (!Traverse.Create(__instance.fashionShowCombo.playerCombo.fashionShowMinigame).Field<bool>("playing").Value || 0 >= __instance.puncher.punches.Count)
                {
                    return false;
                }

                float time = __instance.source.time;
                if (nextPunch.Value >= __instance.puncher.punches.Count)
                {
                    return false;
                }

                BeatType beatType = __instance.puncher.punches[nextPunch.Value];
                float num = Vector3.Distance(__instance.signalLauncher.hit.position, __instance.signalLauncher.start.position) / __instance.configuration.speeds[__instance.signalLauncher.lifetimeIndex];
                if (beatType.time <= time + num)
                {
                    total.Value++;
                    PackData data = __instance.comboPack.GetData(total.Value);
                    if (beatType.large)
                    {
                        data.action = MLPAction.ANY;
                    }
                    else data.action = Adjust(data.action, player); // replacement code

                    nextPunch.Value++;
                    Signal signal = __instance.signalLauncher.Launch(data);
                    if ((bool)signal)
                    {
                        signal.traveling.startTime = __instance.source.time;
                        signal.traveling.hitTime = beatType.time - 0.16f;
                    }
                }
                return false;
            }
            public static MLPAction Adjust(MLPAction Previous, int player)
            {
                if (player == 0 && Previous == MLPAction.RIGHT)
                {
                    LogMessage("right change to left");
                    return MLPAction.LEFT;
                }
                else if (player == 1 && Previous == MLPAction.LEFT)
                {
                    LogMessage("left change to right");
                    return MLPAction.RIGHT;
                }
                else return Previous;
            }
        }

        #endregion

        #region Zipp's Flight Academy

        [HarmonyPatch(typeof(DashThroughTheSkyMiniGame))]
        [HarmonyPatch("StartGame")]
        public class DashThroughTheSkyStartGameHook
        {
            public static float? Player1, Player2;
            public static void Postfix(DashThroughTheSkyMiniGame __instance)
            {
                __instance.audioSource.volume = 2.0f; // somehow the background music here was too quiet,
                                                      // changing volumn settings in the game menu have no affect
                                                      // so I'll just adjust it here
                KinectUpdate.OnProcessPlayer += KinectUpdate_OnProcessPlayer;
            }

            public static void KinectUpdate_OnProcessPlayer(int index)
            {
                if (KinectUpdate.Players[index] == null)
                {
                    if (index == 0) Player1 = null;
                    else if (index == 1) Player2 = null;
                }
                else
                {
                    int body = KinectUpdate.Players[index].Value;
                    Vector2 Head = new Vector2(KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.Head), KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.Head));
                    Vector2 Neck = new Vector2(KinectUpdate.GetJointPosX(body, (int)KinectUpdate.JointType.Neck), KinectUpdate.GetJointPosY(body, (int)KinectUpdate.JointType.Neck));
                    Vector2 Up = new Vector2(0.0f, 1.0f);
                    Vector2 Deviation = (Head - Neck).normalized;
                    float Angle = Mathf.Clamp(Vector2.SignedAngle(Deviation, Up) / 15.0f, -1.0f, 1.0f);
                    if (index == 0) Player1 = Angle;
                    else if (index == 1) Player2 = Angle;
                }
            }

            public static Vector3? GetPlayerPos(int player, Vector3 PlayerLeft, Vector3 PlayerMid, Vector3 PlayerRight)
            {
                // player head tilting left & right = character moving left & right
                float factor;
                if (player == 1 && Player1.HasValue) factor = Player1.Value;
                else if (player == 2 && Player2.HasValue) factor = Player2.Value;
                else return null;

                if (factor < 0.0f) return Vector3.Lerp(PlayerMid, PlayerLeft, -factor);
                else return Vector3.Lerp(PlayerMid, PlayerRight, factor);
            }
        }

        [HarmonyPatch(typeof(DashThroughTheSkyMiniGame))]
        [HarmonyPatch("FinishGame")]
        public class DashThroughTheSkyFinishGameHook
        {
            public static void Prefix()
            {
                DashThroughTheSkyStartGameHook.Player1 = DashThroughTheSkyStartGameHook.Player2 = null;
                KinectUpdate.OnProcessPlayer -= DashThroughTheSkyStartGameHook.KinectUpdate_OnProcessPlayer;
            }
        }

        [HarmonyPatch(typeof(DashThroughTheSkyMiniGame))]
        [HarmonyPatch("UpdateControls")] // controls flying position
        public class DashThroughTheSkyUpdateControlsOverride
        {
            public static bool Prefix(DashThroughTheSkyMiniGame __instance)
            {
                FlyingLevelGenerator flyingLevelGenerator = Traverse.Create(__instance).Field<FlyingLevelGenerator>("flyingLevelGenerator").Value;
                DashThroughTheSkyLevelPredefinedGenerator singleplayer = flyingLevelGenerator as DashThroughTheSkyLevelPredefinedGenerator;
                DashThroughTheSkyLevelProceduralGenerator multiplayer = flyingLevelGenerator as DashThroughTheSkyLevelProceduralGenerator;
                List<Transform> character1MovePoints = Traverse.Create(flyingLevelGenerator).Field<List<Transform>>("character1MovePoints").Value;
                List<Transform> character2MovePoints = Traverse.Create(multiplayer).Field<List<Transform>>("character2MovePoints").Value;
                float timerResumePlayerStop = Traverse.Create(__instance).Field<float>("timerResumePlayerStop").Value;

                // code copied from the original method
                MLPCharacter component = __instance.Characters[0].GetComponent<MLPCharacter>();

                if (!flyingLevelGenerator.GetPlayerStop(1, timerResumePlayerStop))
                {
                    // my modification
                    Vector3? player1pos = null;
                    player1pos = DashThroughTheSkyStartGameHook.GetPlayerPos(1, character1MovePoints[0].position, character1MovePoints[1].position, character1MovePoints[2].position);
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
                    player2pos = DashThroughTheSkyStartGameHook.GetPlayerPos(2, character2MovePoints[0].position, character2MovePoints[1].position, character2MovePoints[2].position);
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

        #region Sprout's Roller-Blading Chase

        [HarmonyPatch(typeof(Runner1MiniGame))]
        [HarmonyPatch("StartGame")]
        class RunnerStartGameHook
        {
            public static RunnerStates[] Runners;
            public static void Postfix(Runner1MiniGame __instance)
            {
                Runners = new RunnerStates[2] {
                    (RunnerStates)__instance.Characters[0].bodyController.states,
                    __instance.Characters.Count > 1 ? (RunnerStates)__instance.Characters[1].bodyController.states : null
                };
                KinectUpdate.OnProcessPlayer += KinectUpdate_OnProcessPlayer;
            }

            public static void KinectUpdate_OnProcessPlayer(int player)
            {
                if (KinectUpdate.Players[player] != null)
                {
                    if (KinectUpdate.GetLeftAnkleOnTheGround(KinectUpdate.Players[player].Value, 0.4f) > 0.0f &&
                        KinectUpdate.GetRightAnkleOnTheGround(KinectUpdate.Players[player].Value, 0.4f) > 0.0f)
                    {
                        LogMessage("Not Jumping player " + player);
                    }
                    else
                    {
                        LogMessage("Jumping player " + player);
                        Runners[player]?.SetCurrrent(Runners[player].jumping);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Runner1MiniGame))]
        [HarmonyPatch("FinishGame")]
        class RunnerFinishGameHook
        {
            public static void Postfix()
            {
                KinectUpdate.OnProcessPlayer -= RunnerStartGameHook.KinectUpdate_OnProcessPlayer;
            }
        }

        #endregion
    }
}
