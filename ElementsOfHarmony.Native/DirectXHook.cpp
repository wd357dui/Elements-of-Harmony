#include "pch.h"
#include "DirectXHook.h"

extern "C" __declspec(dllimport) void __stdcall SetHookCallback2(CallbackProc HookCallback2);

extern "C" {
	__declspec(dllexport) HRESULT __stdcall InitNativeCallbacks();
}

static void HookCallback(Arguments* Args)
{
	switch (Args->VTableIndex)
	{
	}
}

HRESULT __stdcall InitNativeCallbacks()
{
	SetHookCallback2(HookCallback);
	return S_OK;
}
