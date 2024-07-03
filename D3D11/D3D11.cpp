/* the purpose of this DLL is to forcefully
* enable D3D11 debug layer through DLL spoofing hook
* so that debugging on DirectXHook can be easier.
* 
* code copied & improved from AgilityPotion project
* 
* this DLL should not be included in the release build
*/

#define WIN32_LEAN_AND_MEAN
#include <D3D11.h>

#if (_M_X64 || _M_AMD64) && !defined(__ARM_ARCH) && !defined(_M_ARM64) && !defined(_M_ARM64EC)
// x86_64
#else
#error this dll must only target x86_64, because the detour hook here only assembles machine code in x86_64
#endif

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
);

HMODULE D3D11_DLL = NULL;

PFN_D3D11_CREATE_DEVICE D3D11CreateDevice_Original = nullptr;
PFN_D3D11_CREATE_DEVICE_AND_SWAP_CHAIN D3D11CreateDeviceAndSwapChain_Original = nullptr;

HRESULT WINAPI D3D11CreateDevice(
	IDXGIAdapter* pAdapter,
	D3D_DRIVER_TYPE DriverType,
	HMODULE Software,
	UINT Flags,
	D3D_FEATURE_LEVEL* pFeatureLevels,
	UINT FeatureLevels,
	UINT SDKVersion,
	ID3D11Device** ppDevice,
	D3D_FEATURE_LEVEL* pFeatureLevel,
	ID3D11DeviceContext** ppImmediateContext)
{
	if (D3D11CreateDevice_Original == nullptr)
	{
		D3D11CreateDevice_Original = reinterpret_cast<PFN_D3D11_CREATE_DEVICE>(GetProcAddress(D3D11_DLL, "D3D11CreateDevice"));
		if (D3D11CreateDevice_Original == nullptr) return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}
#ifdef _DEBUG
	Flags |= D3D11_CREATE_DEVICE_DEBUG; // enable debug layer
#endif
	HRESULT result = D3D11CreateDevice_Original(pAdapter, DriverType, Software,
		Flags,
		pFeatureLevels, FeatureLevels, SDKVersion, ppDevice, pFeatureLevel, ppImmediateContext);
	return result;
}

HRESULT WINAPI D3D11CreateDeviceAndSwapChain(
	IDXGIAdapter* pAdapter,
	D3D_DRIVER_TYPE DriverType,
	HMODULE Software,
	UINT Flags,
	D3D_FEATURE_LEVEL* pFeatureLevels,
	UINT FeatureLevels,
	UINT SDKVersion,
	DXGI_SWAP_CHAIN_DESC* pSwapChainDesc,
	IDXGISwapChain** ppSwapChain,
	ID3D11Device** ppDevice,
	D3D_FEATURE_LEVEL* pFeatureLevel,
	ID3D11DeviceContext** ppImmediateContext)
{
	if (D3D11CreateDeviceAndSwapChain_Original == nullptr)
	{
		D3D11CreateDeviceAndSwapChain_Original = reinterpret_cast<PFN_D3D11_CREATE_DEVICE_AND_SWAP_CHAIN>(GetProcAddress(D3D11_DLL, "D3D11CreateDeviceAndSwapChain"));
		if (D3D11CreateDeviceAndSwapChain_Original == nullptr) return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}
#ifdef _DEBUG
	Flags |= D3D11_CREATE_DEVICE_DEBUG; // enable debug layer
#endif
	HRESULT result = D3D11CreateDeviceAndSwapChain_Original(pAdapter, DriverType, Software,
		Flags,
		pFeatureLevels, FeatureLevels, SDKVersion, pSwapChainDesc, ppSwapChain, ppDevice, pFeatureLevel, ppImmediateContext);
	return result;
}

/*
* an export function D3D11CoreRegisterLayers
* must be present to allow for enabling the debug layer
* but the system did not use GetProcAddress to call this function
* it just seem to know which address to call already, without asking where it is
* might have something to do with ordinal number in Source.def (I don't know for sure)
* 
* anyway, that means the following code, which was hooking GetProcAddress
* to "redirect" the CPU to the original D3D11CoreRegisterLayers functions,
* is actually useless
* 
CRITICAL_SECTION Mutex{};
 
typedef FARPROC(WINAPI* PFN_GET_PROC_ADDRESS)(_In_ HMODULE hModule, _In_ LPCSTR lpProcName);

FARPROC WINAPI GetProcAddressHookBase(_In_ HMODULE hModule, _In_ LPCSTR lpProcName);
FARPROC WINAPI GetProcAddressHook32(_In_ HMODULE hModule, _In_ LPCSTR lpProcName);

PFN_GET_PROC_ADDRESS GetProcAddressBase_Original = NULL;
PFN_GET_PROC_ADDRESS GetProcAddress32_Original = NULL;
constexpr SIZE_T DetourByteCodeCount = 2 + 4 + 8;
constexpr SIZE_T Detour_Address64_ByteCodeIndex = 2 + 4;
BYTE GetProcAddressBase_OriginalByteCode[DetourByteCodeCount]{ 0x00 };
BYTE GetProcAddress32_OriginalByteCode[DetourByteCodeCount]{ 0x00 };
BYTE GetProcAddressBase_HookedByteCode[DetourByteCodeCount]{
	0xFF, 0x25,
	0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 64-bit target address
};
BYTE GetProcAddress32_HookedByteCode[DetourByteCodeCount]{
	0xFF, 0x25,
	0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 64-bit target address
};

void static CaluclateDetourJmpDst_Base()
{
	*reinterpret_cast<UINT64*>(GetProcAddressBase_HookedByteCode + Detour_Address64_ByteCodeIndex) = reinterpret_cast<UINT64>(GetProcAddressHookBase);
}

void static CaluclateDetourJmpDst_32()
{
	*reinterpret_cast<UINT64*>(GetProcAddress32_HookedByteCode + Detour_Address64_ByteCodeIndex) = reinterpret_cast<UINT64>(GetProcAddressHook32);
}

void static PatchGetProcAddressBase()
{
	DWORD Old;
	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddressBase_Original, GetProcAddressBase_HookedByteCode, DetourByteCodeCount);
	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, Old, &Old);
}

void static UnpatchGetProcAddressBase()
{
	DWORD Old;
	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddressBase_Original, GetProcAddressBase_OriginalByteCode, DetourByteCodeCount);
	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, Old, &Old);
}

void static PatchGetProcAddress32()
{
	DWORD Old;
	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddress32_Original, GetProcAddress32_HookedByteCode, DetourByteCodeCount);
	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, Old, &Old);
}

void static UnpatchGetProcAddress32()
{
	DWORD Old;
	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddress32_Original, GetProcAddress32_OriginalByteCode, DetourByteCodeCount);
	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, Old, &Old);
}

HMODULE This = NULL;
void static Init()
{
	D3D11_DLL = LoadLibraryW(L"C:\\Windows\\System32\\D3D11.dll");
	if (D3D11_DLL == NULL)
	{
#pragma warning( push )
#pragma warning( disable : 6011 )
		int* nope = nullptr;
		*nope = 1;
#pragma warning( pop )
	}

	This = GetModuleHandleW(L"D3D11.dll");
	if (This == NULL)
	{
#pragma warning( push )
#pragma warning( disable : 6011 )
		int* nope = nullptr;
		*nope = 1;
#pragma warning( pop )
	}

	HMODULE KERNEL_BASE_DLL = LoadLibraryW(L"KernelBase.dll");
	if (KERNEL_BASE_DLL == NULL)
	{
#pragma warning( push )
#pragma warning( disable : 6011 )
		int* nope = nullptr;
		*nope = 1;
#pragma warning( pop )
	}

	if (GetProcAddressBase_Original == NULL) {
#pragma warning( push )
#pragma warning( disable : 6387 )
		GetProcAddressBase_Original = (PFN_GET_PROC_ADDRESS)GetProcAddress(KERNEL_BASE_DLL, "GetProcAddress");
#pragma warning( pop )
	}

	HMODULE KERNEL_32_DLL = LoadLibraryW(L"Kernel32.dll");
	if (KERNEL_32_DLL == NULL)
	{
#pragma warning( push )
#pragma warning( disable : 6011 )
		int* nope = nullptr;
		*nope = 1;
#pragma warning( pop )
	}

	if (GetProcAddress32_Original == NULL) {
#pragma warning( push )
#pragma warning( disable : 6387 )
		GetProcAddress32_Original = (PFN_GET_PROC_ADDRESS)GetProcAddress(KERNEL_32_DLL, "GetProcAddress");
#pragma warning( pop )
	}

	DWORD Old;

	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddressBase_OriginalByteCode, GetProcAddressBase_Original, DetourByteCodeCount);
	VirtualProtect(GetProcAddressBase_Original, DetourByteCodeCount, Old, &Old);

	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(GetProcAddress32_OriginalByteCode, GetProcAddress32_Original, DetourByteCodeCount);
	VirtualProtect(GetProcAddress32_Original, DetourByteCodeCount, Old, &Old);

	CaluclateDetourJmpDst_Base();
	CaluclateDetourJmpDst_32();

	PatchGetProcAddressBase();
	PatchGetProcAddress32();
}

FARPROC WINAPI GetProcAddressHookBase(_In_ HMODULE hModule, _In_ LPCSTR lpProcName)
{
	FARPROC result = NULL;
	EnterCriticalSection(&Mutex);
	UnpatchGetProcAddressBase();
	if (hModule == This && !RtlEqualMemory(lpProcName, "D3D11CreateDevice", 18) && !RtlEqualMemory(lpProcName, "D3D11CreateDeviceAndSwapChain", 30)) {
#pragma warning( push )
#pragma warning( disable : 6387 )
		result = GetProcAddressBase_Original(D3D11_DLL, lpProcName);
#pragma warning( pop )
	}
	else {
		result = GetProcAddressBase_Original(hModule, lpProcName);
	}
	PatchGetProcAddressBase();
	LeaveCriticalSection(&Mutex);
	return result;
}

FARPROC WINAPI GetProcAddressHook32(_In_ HMODULE hModule, _In_ LPCSTR lpProcName)
{
	FARPROC result = NULL;
	EnterCriticalSection(&Mutex);
	UnpatchGetProcAddress32();
	if (hModule == This && !RtlEqualMemory(lpProcName, "D3D11CreateDevice", 18) && !RtlEqualMemory(lpProcName, "D3D11CreateDeviceAndSwapChain", 30)) {
#pragma warning( push )
#pragma warning( disable : 6387 )
		result = GetProcAddress32_Original(D3D11_DLL, lpProcName);
#pragma warning( pop )
	}
	else {
		result = GetProcAddress32_Original(hModule, lpProcName);
	}
	PatchGetProcAddress32();
	LeaveCriticalSection(&Mutex);
	return result;
}
*/

/*
* so the next solution is to write all these export functions, but the problem with that is,
* there are no documents on these functions, there's no way to know the arguments or return value types,
* even if there is, it must be very troublesome to add all of them, and it won't worth the time
* 
* so the following code is a solution that doesn't require the knowledge of the argument/return value types:
* 
* we will write all those functions,
* but those functions will have no arguments nor return values because they don't matter (will be ignored later),
* we change their byte code (at startup time) into instructions that will jump to the original functions (detour hook).
*/

typedef void (WINAPI* NOTHING)();

constexpr SIZE_T Detour32_ByteCodeCount = 1 + 4;
constexpr SIZE_T Detour32_ByteCodeIndex = 1;
BYTE Detour32_ByteCode[Detour32_ByteCodeCount]{
	0xE9,
	0x00, 0x00, 0x00, 0x00, // 32-bit target address
};

constexpr SIZE_T Detour64_ByteCodeCount = 2 + 4 + 8;
constexpr SIZE_T Detour64_ByteCodeIndex = 2 + 4;
BYTE Detour64_ByteCode[Detour64_ByteCodeCount]{
	0xFF, 0x25,
	0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 64-bit target address
};

void static Detour(NOTHING DummyFunction, FARPROC TargetAddress, _Out_writes_(Detour64_ByteCodeCount) BYTE SecondStageJump[Detour64_ByteCodeCount])
{
	DWORD Old;
	
	// 2024-6-30
	// for some reason I can't directly CopyMemory from Detour64_ByteCode to DummyFunction
	// some stack corruption happens when I do,
	// however, Detour32_ByteCode is ok;
	// 
	// so I set up a 2-stage jump, a 32-bit and then followed by a 64-bit jump;
	// this is how steam overlay hook had been implemented as well
	// I was wondering why they didn't just do one single 64-bit jump
	// now I think maybe this stack corruption is the reason
	// 
	// but I still don't know why that is,
	// I mean the corruption didn't happen when I was hooking GetProcAddress with only a 64-bit jump
	// so why did it happen to any other function?
	//
	// 2024-7-1
	// I figured it out by looking at the bytes in Cheat Engine,
	// the pointers to the functions themselves didn't point directly to the function's byte code,
	// instead they pointed to an jmp entry in an array of jmp entries which are 32-bit jump instructions
	// which finally jumps to the actual functions.
	// so as for steam overlay, steam overlay didn't do this, the compiler did
	// 
	// which means the stack corruption probably happens because each jmp entry only have 5 bytes each,
	// and 64-bit jump have 14 bytes, so it will overflow

	// assemble second stage jump address
	*reinterpret_cast<UINT64*>(Detour64_ByteCode + Detour64_ByteCodeIndex) = reinterpret_cast<UINT64>(TargetAddress);
	CopyMemory(SecondStageJump, Detour64_ByteCode, Detour64_ByteCodeCount);

	// assemble first stage jump address
	*reinterpret_cast<INT32*>(Detour32_ByteCode + Detour32_ByteCodeIndex) = 
		static_cast<INT32>(reinterpret_cast<intptr_t>(SecondStageJump)) -
		(static_cast<INT32>(reinterpret_cast<intptr_t>(DummyFunction)) + Detour32_ByteCodeCount);

	// add full access including "execute" to second stage jump's memory in case it didn't have it already
	VirtualProtect(SecondStageJump, Detour64_ByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);

	// patch first stage jump
	VirtualProtect(DummyFunction, Detour32_ByteCodeCount, PAGE_EXECUTE_READWRITE, &Old);
	CopyMemory(DummyFunction, Detour32_ByteCode, Detour32_ByteCodeCount);
	VirtualProtect(DummyFunction, Detour32_ByteCodeCount, Old, &Old);
}

void WINAPI D3D11CreateDeviceForD3D12();
void WINAPI D3DKMTCloseAdapter();
void WINAPI D3DKMTDestroyAllocation();
void WINAPI D3DKMTDestroyContext();
void WINAPI D3DKMTDestroyDevice();
void WINAPI D3DKMTDestroySynchronizationObject();
void WINAPI D3DKMTPresent();
void WINAPI D3DKMTQueryAdapterInfo();
void WINAPI D3DKMTSetDisplayPrivateDriverFormat();
void WINAPI D3DKMTSignalSynchronizationObject();
void WINAPI D3DKMTUnlock();
void WINAPI D3DKMTWaitForSynchronizationObject();
void WINAPI EnableFeatureLevelUpgrade();
void WINAPI OpenAdapter10();
void WINAPI OpenAdapter10_2();
void WINAPI CreateDirect3D11DeviceFromDXGIDevice();
void WINAPI CreateDirect3D11SurfaceFromDXGISurface();
void WINAPI D3D11CoreCreateDevice();
void WINAPI D3D11CoreCreateLayeredDevice();
void WINAPI D3D11CoreGetLayeredDeviceSize();
void WINAPI D3D11CoreRegisterLayers();
void WINAPI D3D11On12CreateDevice();
void WINAPI D3DKMTCreateAllocation();
void WINAPI D3DKMTCreateContext();
void WINAPI D3DKMTCreateDevice();
void WINAPI D3DKMTCreateSynchronizationObject();
void WINAPI D3DKMTEscape();
void WINAPI D3DKMTGetContextSchedulingPriority();
void WINAPI D3DKMTGetDeviceState();
void WINAPI D3DKMTGetDisplayModeList();
void WINAPI D3DKMTGetMultisampleMethodList();
void WINAPI D3DKMTGetRuntimeData();
void WINAPI D3DKMTGetSharedPrimaryHandle();
void WINAPI D3DKMTLock();
void WINAPI D3DKMTOpenAdapterFromHdc();
void WINAPI D3DKMTOpenResource();
void WINAPI D3DKMTQueryAllocationResidency();
void WINAPI D3DKMTQueryResourceInfo();
void WINAPI D3DKMTRender();
void WINAPI D3DKMTSetAllocationPriority();
void WINAPI D3DKMTSetContextSchedulingPriority();
void WINAPI D3DKMTSetDisplayMode();
void WINAPI D3DKMTSetGammaRamp();
void WINAPI D3DKMTSetVidPnSourceOwner();
void WINAPI D3DKMTWaitForVerticalBlankEvent();
void WINAPI D3DPerformance_BeginEvent();
void WINAPI D3DPerformance_EndEvent();
void WINAPI D3DPerformance_GetStatus();
void WINAPI D3DPerformance_SetMarker();

constexpr BYTE NOP = 0x90;

BYTE D3D11CreateDeviceForD3D12_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTCloseAdapter_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTDestroyAllocation_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTDestroyContext_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTDestroyDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTDestroySynchronizationObject_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTPresent_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTQueryAdapterInfo_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetDisplayPrivateDriverFormat_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSignalSynchronizationObject_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTUnlock_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTWaitForSynchronizationObject_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE EnableFeatureLevelUpgrade_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE OpenAdapter10_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE OpenAdapter10_2_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE CreateDirect3D11DeviceFromDXGIDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE CreateDirect3D11SurfaceFromDXGISurface_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3D11CoreCreateDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3D11CoreCreateLayeredDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3D11CoreGetLayeredDeviceSize_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3D11CoreRegisterLayers_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3D11On12CreateDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTCreateAllocation_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTCreateContext_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTCreateDevice_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTCreateSynchronizationObject_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTEscape_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetContextSchedulingPriority_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetDeviceState_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetDisplayModeList_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetMultisampleMethodList_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetRuntimeData_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTGetSharedPrimaryHandle_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTLock_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTOpenAdapterFromHdc_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTOpenResource_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTQueryAllocationResidency_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTQueryResourceInfo_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTRender_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetAllocationPriority_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetContextSchedulingPriority_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetDisplayMode_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetGammaRamp_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTSetVidPnSourceOwner_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DKMTWaitForVerticalBlankEvent_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DPerformance_BeginEvent_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DPerformance_EndEvent_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DPerformance_GetStatus_SecondStageJump[Detour64_ByteCodeCount] { NOP };
BYTE D3DPerformance_SetMarker_SecondStageJump[Detour64_ByteCodeCount] { NOP };

void static Init()
{
	D3D11_DLL = LoadLibraryW(L"C:\\Windows\\System32\\D3D11.dll");
	if (D3D11_DLL == NULL)
	{
#pragma warning( push )
#pragma warning( disable : 6011 )
		int* nope = nullptr;
		*nope = 1;
		return;
#pragma warning( pop )
	}

	FARPROC Target;

#define Patch(Func) \
	Target = GetProcAddress(D3D11_DLL, #Func); \
	Detour(Func, Target, Func##_SecondStageJump);

	Patch(D3D11CreateDeviceForD3D12);
	Patch(D3DKMTCloseAdapter);
	Patch(D3DKMTDestroyAllocation);
	Patch(D3DKMTDestroyContext);
	Patch(D3DKMTDestroyDevice);
	Patch(D3DKMTDestroySynchronizationObject);
	Patch(D3DKMTPresent);
	Patch(D3DKMTQueryAdapterInfo);
	Patch(D3DKMTSetDisplayPrivateDriverFormat);
	Patch(D3DKMTSignalSynchronizationObject);
	Patch(D3DKMTUnlock);
	Patch(D3DKMTWaitForSynchronizationObject);
	Patch(EnableFeatureLevelUpgrade);
	Patch(OpenAdapter10);
	Patch(OpenAdapter10_2);
	Patch(CreateDirect3D11DeviceFromDXGIDevice);
	Patch(CreateDirect3D11SurfaceFromDXGISurface);
	Patch(D3D11CoreCreateDevice);
	Patch(D3D11CoreCreateLayeredDevice);
	Patch(D3D11CoreGetLayeredDeviceSize);
	Patch(D3D11CoreRegisterLayers);
	Patch(D3D11On12CreateDevice);
	Patch(D3DKMTCreateAllocation);
	Patch(D3DKMTCreateContext);
	Patch(D3DKMTCreateDevice);
	Patch(D3DKMTCreateSynchronizationObject);
	Patch(D3DKMTEscape);
	Patch(D3DKMTGetContextSchedulingPriority);
	Patch(D3DKMTGetDeviceState);
	Patch(D3DKMTGetDisplayModeList);
	Patch(D3DKMTGetMultisampleMethodList);
	Patch(D3DKMTGetRuntimeData);
	Patch(D3DKMTGetSharedPrimaryHandle);
	Patch(D3DKMTLock);
	Patch(D3DKMTOpenAdapterFromHdc);
	Patch(D3DKMTOpenResource);
	Patch(D3DKMTQueryAllocationResidency);
	Patch(D3DKMTQueryResourceInfo);
	Patch(D3DKMTRender);
	Patch(D3DKMTSetAllocationPriority);
	Patch(D3DKMTSetContextSchedulingPriority);
	Patch(D3DKMTSetDisplayMode);
	Patch(D3DKMTSetGammaRamp);
	Patch(D3DKMTSetVidPnSourceOwner);
	Patch(D3DKMTWaitForVerticalBlankEvent);
	Patch(D3DPerformance_BeginEvent);
	Patch(D3DPerformance_EndEvent);
	Patch(D3DPerformance_GetStatus);
	Patch(D3DPerformance_SetMarker);

#undef Patch
}

void D3D11CreateDeviceForD3D12() {}
void D3DKMTCloseAdapter() {}
void D3DKMTDestroyAllocation() {}
void D3DKMTDestroyContext() {}
void D3DKMTDestroyDevice() {}
void D3DKMTDestroySynchronizationObject() {}
void D3DKMTPresent() {}
void D3DKMTQueryAdapterInfo() {}
void D3DKMTSetDisplayPrivateDriverFormat() {}
void D3DKMTSignalSynchronizationObject() {}
void D3DKMTUnlock() {}
void D3DKMTWaitForSynchronizationObject() {}
void EnableFeatureLevelUpgrade() {}
void OpenAdapter10() {}
void OpenAdapter10_2() {}
void CreateDirect3D11DeviceFromDXGIDevice() {}
void CreateDirect3D11SurfaceFromDXGISurface() {}
void D3D11CoreCreateDevice() {}
void D3D11CoreCreateLayeredDevice() {}
void D3D11CoreGetLayeredDeviceSize() {}
void D3D11CoreRegisterLayers() {}
void D3D11On12CreateDevice() {}
void D3DKMTCreateAllocation() {}
void D3DKMTCreateContext() {}
void D3DKMTCreateDevice() {}
void D3DKMTCreateSynchronizationObject() {}
void D3DKMTEscape() {}
void D3DKMTGetContextSchedulingPriority() {}
void D3DKMTGetDeviceState() {}
void D3DKMTGetDisplayModeList() {}
void D3DKMTGetMultisampleMethodList() {}
void D3DKMTGetRuntimeData() {}
void D3DKMTGetSharedPrimaryHandle() {}
void D3DKMTLock() {}
void D3DKMTOpenAdapterFromHdc() {}
void D3DKMTOpenResource() {}
void D3DKMTQueryAllocationResidency() {}
void D3DKMTQueryResourceInfo() {}
void D3DKMTRender() {}
void D3DKMTSetAllocationPriority() {}
void D3DKMTSetContextSchedulingPriority() {}
void D3DKMTSetDisplayMode() {}
void D3DKMTSetGammaRamp() {}
void D3DKMTSetVidPnSourceOwner() {}
void D3DKMTWaitForVerticalBlankEvent() {}
void D3DPerformance_BeginEvent() {}
void D3DPerformance_EndEvent() {}
void D3DPerformance_GetStatus() {}
void D3DPerformance_SetMarker() {}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		//InitializeCriticalSection(&Mutex);
		Init();
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		//DeleteCriticalSection(&Mutex);
		break;
	}
	return TRUE;
}
