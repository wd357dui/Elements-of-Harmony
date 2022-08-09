// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

ComPtr<IKinectSensor> Sensor;
ComPtr<IBodyFrameSource> Source;
ComPtr<IBodyFrameReader> Reader;
ComPtr<IBodyFrame> Frame;
TIMESPAN FrameTime = 0;
array<IBody*, BODY_COUNT> Bodies;
array<BOOLEAN, BODY_COUNT> BodyTracked;
array<HandState, BODY_COUNT> BodyLeftHandState;
array<HandState, BODY_COUNT> BodyRightHandState;
array<TrackingConfidence, BODY_COUNT> BodyLeftHandConfidence;
array<TrackingConfidence, BODY_COUNT> BodyRightHandConfidence;
array<array<Joint, size_t(JointType_Count)>, BODY_COUNT> Joints;

extern "C" __declspec(dllexport) int KinectInit();
extern "C" __declspec(dllexport) int AcquireLatestFrame();
extern "C" __declspec(dllexport) __int64 GetFrameTime();
extern "C" __declspec(dllexport) bool GetBodyTracked(int body);
extern "C" __declspec(dllexport) float GetJointPosX(int body, int joint);
extern "C" __declspec(dllexport) float GetJointPosY(int body, int joint);
extern "C" __declspec(dllexport) float GetJointPosZ(int body, int joint);
extern "C" __declspec(dllexport) int GetHandStateLeft(int body);
extern "C" __declspec(dllexport) int GetHandStateRight(int body);
extern "C" __declspec(dllexport) int GetHandConfidenceLeft(int body);
extern "C" __declspec(dllexport) int GetHandConfidenceRight(int body);
extern "C" __declspec(dllexport) int GetJointState(int body, int joint);

void ReleaseLastFrame();

int KinectInit()
{
	HRESULT result = 0;

	result = GetDefaultKinectSensor(&Sensor);
	if (FAILED(result)) return result;
	result = Sensor->Open();
	if (FAILED(result)) return result;
	result = Sensor->get_BodyFrameSource(&Source);
	if (FAILED(result)) return result;
	result = Source->OpenReader(&Reader);
	if (FAILED(result)) return result;

	return result;
}

int AcquireLatestFrame()
{
	HRESULT result = 0;

	ReleaseLastFrame();
	result = Reader->AcquireLatestFrame(&Frame);
	if (FAILED(result)) return result;
	result = Frame->GetAndRefreshBodyData(BODY_COUNT, Bodies.data());
	if (FAILED(result)) return result;
	result = Frame->get_RelativeTime(&FrameTime);
	if (FAILED(result)) return result;

	LARGE_INTEGER Large{};
	if (!QueryPerformanceFrequency(&Large)) {
		return GetLastError();
	}

	for (int body = 0; body < BODY_COUNT; body++) {
		result = Bodies[body]->get_IsTracked(&BodyTracked[body]);
		if (FAILED(result)) return result;
		result = Bodies[body]->GetJoints(UINT(Joints[body].size()), Joints[body].data());
		if (FAILED(result)) return result;
		result = Bodies[body]->get_HandLeftState(&BodyLeftHandState[body]);
		if (FAILED(result)) return result;
		result = Bodies[body]->get_HandRightState(&BodyRightHandState[body]);
		if (FAILED(result)) return result;
		result = Bodies[body]->get_HandLeftConfidence(&BodyLeftHandConfidence[body]);
		if (FAILED(result)) return result;
		result = Bodies[body]->get_HandLeftConfidence(&BodyRightHandConfidence[body]);
		if (FAILED(result)) return result;
	}

	return result;
}

__int64 GetFrameTime()
{
	return FrameTime;
}

bool GetBodyTracked(int body)
{
	return BodyTracked[body] != 0;
}

float GetJointPosX(int body, int joint)
{
	return Joints[body][joint].Position.X;
}

float GetJointPosY(int body, int joint)
{
	return Joints[body][joint].Position.Y;
}

float GetJointPosZ(int body, int joint)
{
	return Joints[body][joint].Position.Z;
}

int GetHandStateLeft(int body)
{
	return BodyLeftHandState[body];
}

int GetHandStateRight(int body)
{
	return BodyRightHandState[body];
}

int GetHandConfidenceLeft(int body)
{
	return BodyLeftHandConfidence[body];
}

int GetHandConfidenceRight(int body)
{
	return BodyRightHandConfidence[body];
}

int GetJointState(int body, int joint)
{
	return Joints[body][joint].TrackingState;
}

void ReleaseLastFrame()
{
	for (IBody*& Ptr : Bodies) {
		if (Ptr != nullptr) {
			Ptr->Release();
			Ptr = nullptr;
		}
	}
	Frame.Reset();
}
