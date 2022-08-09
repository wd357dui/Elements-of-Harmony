using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ElementsOfHarmony
{
    // this is a motion control sub-mod using Microsoft Kinect V2
    // I may be planning on adding more sub-mods in the future, the sub-mods shouldn't have dependencies on each other if they're not related
    // which is going to be problematic because I don't have an idea on how to properly seperate them
    // I'm a little unwilling to create new DLLs (because that would require extra work to setup the mod) (but this is prbably what I'm going to end up with in the end)
    // I don't want to create new github repos either, I want to keep sub-mods as branches if possible
    // also I'm still struggling to understand github branches & stuff
    public class Loyalty
    {
        public static void LogMessage(string Message)
        {
            ElementsOfHarmony.LogMessage(Message);
        }

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
            public static void ProcessPlayer(int index)
            {
                Right_[index] = Right[index];
                Left_[index] = Left[index];
                Up_[index] = Up[index];
                Down_[index] = Down[index];

                A_[index] = A[index];
                B_[index] = B[index];
                Y_[index] = Y[index];
                Start_[index] = Start[index];

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
            }

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
                        return false; // pending
                    case MLPAction.MENURIGHT:
                        return false; // pending
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
                        result = false; // pending
                        break;
                    case MLPAction.MENURIGHT:
                        result = false; // pending
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
            public static void Prefix()
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
            }
            public static void Postfix()
            {
                KinectUpdate.UpdateMutex.ReleaseMutex();
            }
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
    }
}
