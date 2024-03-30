#include "pch.h"

using namespace std;

extern "C" {
	__declspec(dllexport) DWORD __stdcall InstallHook();
	__declspec(dllexport) DWORD __stdcall UninstallHook();
	__declspec(dllexport) DWORD __stdcall SetRunning(bool Running);

	__declspec(dllexport) int __stdcall BeginProcessCompletionEvent();
	__declspec(dllexport) DWORD __stdcall EndProcessCompletionEvent();

	__declspec(dllexport) intptr_t __stdcall Get_DXGI_DLL_Address();
	__declspec(dllexport) DWORD __stdcall Get_DXGI_DLL_ImageSize();
	__declspec(dllexport) intptr_t __stdcall Get_IDXGISwapChain_Present_Original();
	__declspec(dllexport) intptr_t __stdcall Get_IDXGISwapChain1_Present1_Original();
	__declspec(dllexport) intptr_t __stdcall Get_IDXGIFactory_CreateSwapChain_Original();
	__declspec(dllexport) intptr_t __stdcall Get_IDXGIFactory2_CreateSwapChainForHwnd_Original();

	__declspec(dllexport) void** __stdcall Get_LocalVariablesArray();

	__declspec(dllexport) bool __stdcall Get_Present_PreviousDetourHookDetected();
	__declspec(dllexport) bool __stdcall Get_Present1_PreviousDetourHookDetected();
	__declspec(dllexport) intptr_t __stdcall Get_GameOverlayRenderer64_DLL_Address();
	__declspec(dllexport) DWORD __stdcall Get_GameOverlayRenderer64_DLL_ImageSize();
	__declspec(dllexport) bool __stdcall Get_Present_HasOriginalFirstFiveBytesOfInstruction();
	__declspec(dllexport) bool __stdcall Get_Present1_HasOriginalFirstFiveBytesOfInstruction();
	__declspec(dllexport) bool __stdcall Get_Present_HasLoadedFirstFiveBytesOfInstruction();
	__declspec(dllexport) bool __stdcall Get_Present1_HasLoadedFirstFiveBytesOfInstruction();
	__declspec(dllexport) void __stdcall Refresh_Present_LoadedFirstFiveBytesOfInstruction();
	__declspec(dllexport) void __stdcall Refresh_Present1_LoadedFirstFiveBytesOfInstruction();
	__declspec(dllexport) intptr_t __stdcall Get_Present_OriginalFirstFiveBytesOfInstruction();
	__declspec(dllexport) intptr_t __stdcall Get_Present1_OriginalFirstFiveBytesOfInstruction();
	__declspec(dllexport) intptr_t __stdcall Get_Present_LoadedFirstFiveBytesOfInstruction();
	__declspec(dllexport) intptr_t __stdcall Get_Present1_LoadedFirstFiveBytesOfInstruction();

	/// <returns>1 for true, 0 for false, -1 for error</returns>
	__declspec(dllexport) int __stdcall JmpEndsUpInRange(intptr_t SrcAddr, intptr_t RangeStart, DWORD Size);
	__declspec(dllexport) BYTE __stdcall JmpEndsUpInRange_LastInstruction();
	__declspec(dllexport) intptr_t __stdcall JmpEndsUpInRange_LastAddress();
	__declspec(dllexport) DWORD __stdcall JmpEndsUpInRange_LastError();
}

typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain_Present_Proc)(IDXGISwapChain* This, UINT SyncInterval, UINT Flags);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain1_Present1_Proc)(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags,
	_In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
typedef HRESULT(STDMETHODCALLTYPE* IDXGIFactory_CreateSwapChain_Proc)(IDXGIFactory* This,
	_In_  IUnknown* pDevice,
	_In_::DXGI_SWAP_CHAIN_DESC* pDesc,
	_COM_Outptr_  IDXGISwapChain** ppSwapChain);
typedef HRESULT(STDMETHODCALLTYPE* IDXGIFactory2_CreateSwapChainForHwnd_Proc)(IDXGIFactory2* This,
	_In_  IUnknown* pDevice,
	_In_  HWND hWnd,
	_In_  const ::DXGI_SWAP_CHAIN_DESC1* pDesc,
	_In_opt_  const ::DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc,
	_In_opt_  IDXGIOutput* pRestrictToOutput,
	_COM_Outptr_  IDXGISwapChain1** ppSwapChain);
IDXGISwapChain_Present_Proc IDXGISwapChain_Present_Original = nullptr;
IDXGISwapChain1_Present1_Proc IDXGISwapChain1_Present1_Original = nullptr;
IDXGIFactory_CreateSwapChain_Proc IDXGIFactory_CreateSwapChain_Original = nullptr;
IDXGIFactory2_CreateSwapChainForHwnd_Proc IDXGIFactory2_CreateSwapChainForHwnd_Original = nullptr;

intptr_t FactoryVTableAddress = 0;
intptr_t Factory2VTableAddress = 0;
intptr_t SwapChainVTableAddress = 0;
intptr_t SwapChain1VTableAddress = 0;
constexpr size_t IDXGIFactory_CreateSwapChain_VTableIndex = 10;
constexpr size_t IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex = 15;
constexpr size_t IDXGISwapChain_Present_VTableIndex = 8;
constexpr size_t IDXGISwapChain1_Present1_VTableIndex = 22;
constexpr size_t IUnknown_Release_VTableIndex = 2;

HMODULE		DXGI_DLL = NULL;
intptr_t	DXGI_DLL_BaseAddress = 0;
DWORD		DXGI_DLL_ImageSize = 0;
HMODULE		GameOverlayRenderer64_DLL = NULL;
intptr_t	GameOverlayRenderer64_DLL_BaseAddress = 0;
DWORD		GameOverlayRenderer64_DLL_ImageSize = 0;
constexpr size_t InstructionCompareByteCount = 32;
constexpr HRESULT Discord = 0xD15C03D;
BYTE Present_OriginalFirstFiveBytesOfInstruction[5]{ 0 };
BYTE Present1_OriginalFirstFiveBytesOfInstruction[5]{ 0 };
BYTE Present_LoadedFirstFiveBytesOfInstruction[5]{ 0 };
BYTE Present1_LoadedFirstFiveBytesOfInstruction[5]{ 0 };
bool Present_HasOriginalFirstFiveBytesOfInstruction = false;
bool Present1_HasOriginalFirstFiveBytesOfInstruction = false;
bool Present_HasLoadedFirstFiveBytesOfInstruction = false;
bool Present1_HasLoadedFirstFiveBytesOfInstruction = false;
bool Present_PreviousDetourHookDetected = false;
bool Present1_PreviousDetourHookDetected = false;
HRESULT DetectPreviousDetourHook();

HRESULT IDXGISwapChain_Present_PatchFix();
HRESULT IDXGISwapChain_Present_UnPatchFix();
HRESULT IDXGISwapChain1_Present1_PatchFix();
HRESULT IDXGISwapChain1_Present1_UnPatchFix();

atomic<bool> Running = false;
HANDLE OnCompletionEvent = NULL;
HANDLE AckCompletionEvent = NULL;
HANDLE StopEvent = NULL;
int CompletionID = 0;
void* PtrList[8]{ 0 };

HRESULT STDMETHODCALLTYPE IDXGISwapChain_Present_Override(IDXGISwapChain* This, UINT SyncInterval, UINT Flags);
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
HRESULT STDMETHODCALLTYPE IDXGIFactory_CreateSwapChain_Override(IDXGIFactory* This,
	_In_  IUnknown* pDevice,
	_In_::DXGI_SWAP_CHAIN_DESC* pDesc,
	_COM_Outptr_  IDXGISwapChain** ppSwapChain);
HRESULT STDMETHODCALLTYPE IDXGIFactory2_CreateSwapChainForHwnd_Override(IDXGIFactory2* This,
	_In_  IUnknown* pDevice,
	_In_  HWND hWnd,
	_In_  const ::DXGI_SWAP_CHAIN_DESC1* pDesc,
	_In_opt_  const ::DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc,
	_In_opt_  IDXGIOutput* pRestrictToOutput,
	_COM_Outptr_  IDXGISwapChain1** ppSwapChain);

constexpr WCHAR ClassName[] = L"MAGIC";
constexpr WCHAR WindowName[] = L"Magic";
HINSTANCE Module = NULL;
HWND hWndForTest = NULL;
HANDLE WindowThread = NULL;
HANDLE WindowCreated = NULL;
DWORD CreateHwndForTest(LPVOID Arg);
LRESULT DummyWndProc(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam);

/// <summary>
/// compare the loaded original Present function with byte code from the original file; 
/// for a specified number of byte codes, excluding the first five bytes (which can hold a jmp instruction): 
/// we match the byte code with the original file;
/// If a match exists then we believe that we have found the original function's byte code;
/// so next step we compare the first five bytes;
/// if the first five bytes didn't match, then it must be a detour hook.
/// if the first five bytes did match however, there were no detour hooks.
/// </summary>
/// <returns>if no byte code ever matched, returns STATUS_ENTRYPOINT_NOT_FOUND (didn't find the original function)</returns>
HRESULT DetectPreviousDetourHook()
{
	BYTE CurrentPresentInstructions[InstructionCompareByteCount];
	CopyMemory(CurrentPresentInstructions, IDXGISwapChain_Present_Original, InstructionCompareByteCount);
	CopyMemory(Present_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain_Present_Original, sizeof(Present_LoadedFirstFiveBytesOfInstruction));
	Present_HasLoadedFirstFiveBytesOfInstruction = true;

	BYTE CurrentPresent1Instructions[InstructionCompareByteCount];
	CopyMemory(CurrentPresent1Instructions, IDXGISwapChain1_Present1_Original, InstructionCompareByteCount);
	CopyMemory(Present1_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain1_Present1_Original, sizeof(Present1_LoadedFirstFiveBytesOfInstruction));
	Present1_HasLoadedFirstFiveBytesOfInstruction = true;

	wstring RealDllPath = L"C:/Windows/System32/DXGI.dll";
	HANDLE DLL_File = CreateFileW(RealDllPath.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_READONLY, NULL);
	if (DLL_File == INVALID_HANDLE_VALUE) {
		return E_FAIL;
	}

	size_t CurrentPos = 0x80; // skip the "This program cannot be run in DOS mode" header that every dll have
	LARGE_INTEGER Large;
	if (!GetFileSizeEx(DLL_File, &Large)) {
		return E_FAIL;
	}

	bool PresentFound = false;
	bool Present1Found = false;
	size_t FileSize = Large.QuadPart;
	while (((CurrentPos + InstructionCompareByteCount) < FileSize) && (!PresentFound || !Present1Found)) {
		Large.QuadPart = CurrentPos;
		if (!SetFilePointerEx(DLL_File, Large, nullptr, FILE_BEGIN)) {
			return E_FAIL;
		}
		BYTE OriginalInstructions[InstructionCompareByteCount]{ 0 };
		if (!ReadFile(DLL_File, OriginalInstructions, InstructionCompareByteCount, nullptr, nullptr)) {
			return E_FAIL;
		}
		if (RtlEqualMemory(CurrentPresentInstructions + 5, OriginalInstructions + 5, InstructionCompareByteCount - 5)) {
			Present_PreviousDetourHookDetected = RtlCompareMemory(CurrentPresentInstructions, OriginalInstructions, 5) != 5;
			CopyMemory(Present_OriginalFirstFiveBytesOfInstruction, OriginalInstructions, 5);
			Present_HasOriginalFirstFiveBytesOfInstruction = true;
			PresentFound = true;
		}
		if (RtlEqualMemory(CurrentPresent1Instructions + 5, OriginalInstructions + 5, InstructionCompareByteCount - 5)) {
			Present1_PreviousDetourHookDetected = RtlCompareMemory(CurrentPresent1Instructions, OriginalInstructions, 5) != 5;
			CopyMemory(Present1_OriginalFirstFiveBytesOfInstruction, OriginalInstructions, 5);
			Present1_HasOriginalFirstFiveBytesOfInstruction = true;
			Present1Found = true;
		}
		CurrentPos += 0x10; // it seems that the beginning position of every function are aligned to 0x10
	}
	if (!PresentFound) {
		return STATUS_ENTRYPOINT_NOT_FOUND;
	}

	if (!CloseHandle(DLL_File)) {
		return E_FAIL;
	}

	return S_OK;
}

HRESULT IDXGISwapChain_Present_PatchFix()
{
	DWORD OldProtect;
	if (!VirtualProtect(IDXGISwapChain_Present_Original, sizeof(Present_OriginalFirstFiveBytesOfInstruction), PAGE_EXECUTE_READWRITE, &OldProtect)) return E_FAIL;
	if (!Present_HasLoadedFirstFiveBytesOfInstruction) {
		CopyMemory(Present_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain_Present_Original, sizeof(Present_LoadedFirstFiveBytesOfInstruction));
		Present_HasLoadedFirstFiveBytesOfInstruction = true;
	}
	CopyMemory(IDXGISwapChain_Present_Original, Present_OriginalFirstFiveBytesOfInstruction, sizeof(Present_OriginalFirstFiveBytesOfInstruction));
	if (!VirtualProtect(IDXGISwapChain_Present_Original, sizeof(Present_OriginalFirstFiveBytesOfInstruction), OldProtect, &OldProtect)) return E_FAIL;
	return S_OK;
}

HRESULT IDXGISwapChain_Present_UnPatchFix()
{
	if (!Present_HasLoadedFirstFiveBytesOfInstruction) return E_NOT_VALID_STATE;
	DWORD OldProtect;
	if (!VirtualProtect(IDXGISwapChain_Present_Original, sizeof(Present_LoadedFirstFiveBytesOfInstruction), PAGE_EXECUTE_READWRITE, &OldProtect)) return E_FAIL;
	CopyMemory(IDXGISwapChain_Present_Original, Present_LoadedFirstFiveBytesOfInstruction, sizeof(Present_LoadedFirstFiveBytesOfInstruction));
	if (!VirtualProtect(IDXGISwapChain_Present_Original, sizeof(Present_LoadedFirstFiveBytesOfInstruction), OldProtect, &OldProtect)) return E_FAIL;
	return S_OK;
}

HRESULT IDXGISwapChain1_Present1_PatchFix()
{
	DWORD OldProtect;
	if (!VirtualProtect(IDXGISwapChain1_Present1_Original, sizeof(Present_OriginalFirstFiveBytesOfInstruction), PAGE_EXECUTE_READWRITE, &OldProtect)) return E_FAIL;
	if (!Present1_HasLoadedFirstFiveBytesOfInstruction) {
		CopyMemory(Present1_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain1_Present1_Original, sizeof(Present1_LoadedFirstFiveBytesOfInstruction));
		Present1_HasLoadedFirstFiveBytesOfInstruction = true;
	}
	CopyMemory(IDXGISwapChain1_Present1_Original, Present1_OriginalFirstFiveBytesOfInstruction, sizeof(Present_OriginalFirstFiveBytesOfInstruction));
	if (!VirtualProtect(IDXGISwapChain1_Present1_Original, sizeof(Present1_OriginalFirstFiveBytesOfInstruction), OldProtect, &OldProtect)) return E_FAIL;
	return S_OK;
}

HRESULT IDXGISwapChain1_Present1_UnPatchFix()
{
	if (!Present1_HasLoadedFirstFiveBytesOfInstruction) return E_NOT_VALID_STATE;
	DWORD OldProtect;
	if (!VirtualProtect(IDXGISwapChain1_Present1_Original, sizeof(Present1_LoadedFirstFiveBytesOfInstruction), PAGE_EXECUTE_READWRITE, &OldProtect)) return E_FAIL;
	CopyMemory(IDXGISwapChain1_Present1_Original, Present1_LoadedFirstFiveBytesOfInstruction, sizeof(Present1_LoadedFirstFiveBytesOfInstruction));
	if (!VirtualProtect(IDXGISwapChain1_Present1_Original, sizeof(Present1_LoadedFirstFiveBytesOfInstruction), OldProtect, &OldProtect)) return E_FAIL;
	return S_OK;
}

std::map<HWND, ComPtr<IDXGISwapChain>> Window_SwapChain_Map;

std::map<IDXGISwapChain*, int> IDXGISwapChain_Present_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain_Present_Override(IDXGISwapChain* This, UINT SyncInterval, UINT Flags)
{
	if (IDXGISwapChain_Present_StackCount.find(This) == IDXGISwapChain_Present_StackCount.end()) {
		IDXGISwapChain_Present_StackCount[This] = 0;
	}
	bool StackOverflowFixNeeded = ++IDXGISwapChain_Present_StackCount[This] >= 2;
	if (Running && !StackOverflowFixNeeded) {
		HRESULT result = 0;
		DXGI_SWAP_CHAIN_DESC Desc{};
		result = This->GetDesc(&Desc);
		if (FAILED(result)) throw exception(("SwapChain GetDesc failed " + std::to_string(result)).c_str());
		result = This->QueryInterface(IID_PPV_ARGS(&Window_SwapChain_Map[Desc.OutputWindow]));
		if (FAILED(result)) throw exception(("SwapChain QueryInterface failed " + std::to_string(result)).c_str());
		ZeroMemory(PtrList, sizeof(PtrList));
		PtrList[0] = &This;
		PtrList[1] = &SyncInterval;
		PtrList[2] = &Flags;
		CompletionID = -static_cast<int>(IDXGISwapChain_Present_VTableIndex);
		if (!SetEvent(OnCompletionEvent)) throw exception("synchronization error");
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		if (WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE) != WAIT_OBJECT_0) {
			throw exception("synchronization error");
		}
	}
	HRESULT result = 0;
	if (StackOverflowFixNeeded) {
		if (Present_HasOriginalFirstFiveBytesOfInstruction) {
			// I have a fix for this stack overflow which is known caused by steam overlay hook (GameOverlayRenderer64.dll)
			HRESULT PatchResult = 0;
			PatchResult = IDXGISwapChain_Present_PatchFix();
			if (FAILED(PatchResult)) throw exception(("failed to patch stackoverflow fix - " + std::to_string(PatchResult)).c_str());
			result = IDXGISwapChain_Present_Original(This, ++SyncInterval, Flags);
			PatchResult = IDXGISwapChain_Present_UnPatchFix();
			if (FAILED(PatchResult)) throw exception(("failed to patch stackoverflow fix - " + std::to_string(PatchResult)).c_str());
		}
		else {
			// I don't have a fix for this stack overflow (we're doomed! celestia help us all)
			result = IDXGISwapChain_Present_Original(This, ++SyncInterval, Flags);
		}
	}
	else {
		result = IDXGISwapChain_Present_Original(This, ++SyncInterval, Flags);
	}
	--IDXGISwapChain_Present_StackCount[This];
	if (FAILED(result)) return result;
	if (Running && !StackOverflowFixNeeded) {
		CompletionID = IDXGISwapChain_Present_VTableIndex;
		if (!SetEvent(OnCompletionEvent)) throw exception("synchronization error");
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		if (WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE) != WAIT_OBJECT_0) {
			throw exception("synchronization error");
		}
	}
	return result;
}

std::map<IDXGISwapChain1*, int> IDXGISwapChain1_Present1_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters)
{
	if (IDXGISwapChain1_Present1_StackCount.find(This) == IDXGISwapChain1_Present1_StackCount.end()) {
		IDXGISwapChain1_Present1_StackCount[This] = 0;
	}
	bool StackOverflowFixNeeded = ++IDXGISwapChain1_Present1_StackCount[This] >= 2;
	if (Running && !StackOverflowFixNeeded) {
		HRESULT result = 0;
		DXGI_SWAP_CHAIN_DESC Desc{};
		result = This->GetDesc(&Desc);
		if (FAILED(result)) throw exception(("SwapChain1 GetDesc failed " + std::to_string(result)).c_str());
		result = This->QueryInterface(IID_PPV_ARGS(&Window_SwapChain_Map[Desc.OutputWindow]));
		if (FAILED(result)) throw exception(("SwapChain1 QueryInterface failed " + std::to_string(result)).c_str());
		ZeroMemory(PtrList, sizeof(PtrList));
		PtrList[0] = &This;
		PtrList[1] = &SyncInterval;
		PtrList[2] = &Flags;
		PtrList[3] = &pPresentParameters;
		CompletionID = -static_cast<int>(IDXGISwapChain1_Present1_VTableIndex);
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	HRESULT result = 0;
	if (StackOverflowFixNeeded) {
		if (Present1_HasOriginalFirstFiveBytesOfInstruction) {
			// I have a fix for this stack overflow which is known caused by steam overlay hook (GameOverlayRenderer64.dll)
			HRESULT PatchResult = 0;
			PatchResult = IDXGISwapChain1_Present1_PatchFix();
			if (FAILED(PatchResult)) throw exception(("failed to patch stackoverflow fix - " + std::to_string(PatchResult)).c_str());
			result = IDXGISwapChain1_Present1_Original(This, ++SyncInterval, Flags, pPresentParameters);
			PatchResult = IDXGISwapChain1_Present1_UnPatchFix();
			if (FAILED(PatchResult)) throw exception(("failed to patch stackoverflow fix - " + std::to_string(PatchResult)).c_str());
		}
		else {
			// I don't have a fix for this stack overflow (we're doomed! celestia help us all)
			result = IDXGISwapChain1_Present1_Original(This, ++SyncInterval, Flags, pPresentParameters);
		}
	}
	else {
		result = IDXGISwapChain1_Present1_Original(This, ++SyncInterval, Flags, pPresentParameters);
	}
	--IDXGISwapChain1_Present1_StackCount[This];
	if (FAILED(result)) return result;
	if (Running && !StackOverflowFixNeeded) {
		CompletionID = IDXGISwapChain1_Present1_VTableIndex;
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	return result;
}

HRESULT STDMETHODCALLTYPE IDXGIFactory_CreateSwapChain_Override(IDXGIFactory* This,
	_In_  IUnknown* pDevice,
	_In_::DXGI_SWAP_CHAIN_DESC* pDesc,
	_COM_Outptr_  IDXGISwapChain** ppSwapChain)
{
	DXGI_SWAP_CHAIN_DESC Desc = *pDesc;
	pDesc = &Desc;
	if (Window_SwapChain_Map.find(Desc.OutputWindow) != Window_SwapChain_Map.end()) {
		CompletionID = IUnknown_Release_VTableIndex;
		ZeroMemory(PtrList, sizeof(PtrList));
		PtrList[0] = Window_SwapChain_Map[Desc.OutputWindow].Get();
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
		Window_SwapChain_Map.erase(Desc.OutputWindow);
	}
	if (Running) {
		ZeroMemory(PtrList, sizeof(PtrList));
		PtrList[0] = &This;
		PtrList[1] = &pDevice;
		PtrList[2] = &pDesc;
		PtrList[3] = &ppSwapChain;
		CompletionID = -static_cast<int>(IDXGIFactory_CreateSwapChain_VTableIndex);
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	HRESULT result = IDXGIFactory_CreateSwapChain_Original(This, pDevice, pDesc, ppSwapChain);
	if (FAILED(result)) return result;
	if (Running) {
		CompletionID = IDXGIFactory_CreateSwapChain_VTableIndex;
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	return result;
}

HRESULT STDMETHODCALLTYPE IDXGIFactory2_CreateSwapChainForHwnd_Override(IDXGIFactory2* This,
	_In_  IUnknown* pDevice,
	_In_  HWND hWnd,
	_In_  const ::DXGI_SWAP_CHAIN_DESC1* pDesc,
	_In_opt_  const ::DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc,
	_In_opt_  IDXGIOutput* pRestrictToOutput,
	_COM_Outptr_  IDXGISwapChain1** ppSwapChain)
{
	DXGI_SWAP_CHAIN_DESC1 Desc = *pDesc;
	DXGI_SWAP_CHAIN_FULLSCREEN_DESC FullscreenDesc = pFullscreenDesc != nullptr ? *pFullscreenDesc : DXGI_SWAP_CHAIN_FULLSCREEN_DESC{};
	pDesc = &Desc;
	if (Window_SwapChain_Map.find(hWnd) != Window_SwapChain_Map.end()) {
		CompletionID = IUnknown_Release_VTableIndex;
		ZeroMemory(PtrList, sizeof(PtrList));
		PtrList[0] = Window_SwapChain_Map[hWnd].Get();
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
		Window_SwapChain_Map.erase(hWnd);
	}
	if (Running) {
		ZeroMemory(PtrList, sizeof(PtrList));
		intptr_t FullscreenDescOptionalPointers[3]{
			reinterpret_cast<intptr_t>(&pFullscreenDesc), // pointer to local pointer
			reinterpret_cast<intptr_t>(pFullscreenDesc), // pointer to original struct or nullptr
			reinterpret_cast<intptr_t>(&FullscreenDesc), // pointer to local struct
		};
		PtrList[0] = &This;
		PtrList[1] = &pDevice;
		PtrList[2] = &hWnd;
		PtrList[3] = &pDesc;
		PtrList[4] = FullscreenDescOptionalPointers;
		PtrList[5] = &pRestrictToOutput;
		PtrList[6] = &ppSwapChain;
		CompletionID = -static_cast<int>(IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex);
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	HRESULT result = IDXGIFactory2_CreateSwapChainForHwnd_Original(This, pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
	if (FAILED(result)) return result;
	if (Running) {
		CompletionID = IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex;
		if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - SetEvent - " + std::to_string(GetLastError())).c_str());
		HANDLE WaitHandles[2]{ AckCompletionEvent, StopEvent };
		DWORD WaitResult = WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE);
		if (WaitResult != WAIT_OBJECT_0 && WaitResult != WAIT_OBJECT_0 + 1) {
			if (!SetEvent(OnCompletionEvent)) throw exception(("synchronization error - WaitForMultipleObjects - " + std::to_string(GetLastError())).c_str());
		}
	}
	return result;
}

DWORD CreateHwndForTest(LPVOID Arg)
{
	WNDCLASSEXW wcexw{};
	wcexw.cbSize = sizeof(WNDCLASSEXW);
	wcexw.style = CS_HREDRAW | CS_VREDRAW;
	wcexw.lpfnWndProc = DummyWndProc;
	wcexw.hInstance = Module = GetModuleHandleW(NULL);
	wcexw.hIcon = NULL;
	wcexw.hIconSm = NULL;
	wcexw.hCursor = LoadCursorW(nullptr, IDC_ARROW);
	wcexw.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wcexw.lpszClassName = ClassName;

	ATOM Class = RegisterClassExW(&wcexw);
	if (Class == NULL) return E_FAIL;

	hWndForTest = CreateWindowW(ClassName, WindowName, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0,
		NULL, NULL, Module, nullptr);
	if (hWndForTest == NULL) return E_FAIL;

	SetEvent(WindowCreated);

	MSG msg;
	while (GetMessageW(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessageW(&msg);
	}

	if (!UnregisterClassW(ClassName, Module)) {
		return E_FAIL;
	}
	return S_OK;
}

LRESULT DummyWndProc(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam)
{
	switch (Msg)
	{
	case WM_CLOSE:
		DestroyWindow(hWnd);
		return 0;
	case WM_DESTROY:
		PostQuitMessage(0);
		return 0;
	}
	return DefWindowProcW(hWnd, Msg, wParam, lParam);
}

DWORD __stdcall InstallHook()
{
	HRESULT result = 0;

	ComPtr<IDXGIFactory> Factory;
	ComPtr<IDXGIFactory2> Factory2;
	ComPtr<IDXGISwapChain> SwapChain;
	ComPtr<IDXGISwapChain1> SwapChain1;

	if (DXGI_DLL == NULL) {
		DXGI_DLL = GetModuleHandleW(L"DXGI.dll");
		if (DXGI_DLL == NULL) return GetLastError();
	}
	MODULEINFO Info{};
	if (!GetModuleInformation(GetCurrentProcess(), DXGI_DLL, &Info, sizeof(MODULEINFO))) {
		return GetLastError();
	}
	DXGI_DLL_BaseAddress = reinterpret_cast<intptr_t>(Info.lpBaseOfDll);
	DXGI_DLL_ImageSize = Info.SizeOfImage;
	
	if (GameOverlayRenderer64_DLL == NULL) {
		GameOverlayRenderer64_DLL = GetModuleHandleW(L"GameOverlayRenderer64.dll");
	}
	if (GameOverlayRenderer64_DLL != NULL) {
		MODULEINFO Info{};
		if (!GetModuleInformation(GetCurrentProcess(), GameOverlayRenderer64_DLL, &Info, sizeof(MODULEINFO))) {
			return GetLastError();
		}
		GameOverlayRenderer64_DLL_BaseAddress = reinterpret_cast<intptr_t>(Info.lpBaseOfDll);
		GameOverlayRenderer64_DLL_ImageSize = Info.SizeOfImage;
	}

	if (Factory == nullptr) {
		result = CreateDXGIFactory(IID_PPV_ARGS(&Factory2));
		if (FAILED(result))
		{
			result = CreateDXGIFactory(IID_PPV_ARGS(&Factory));
			if (FAILED(result)) return result;
		}
		result = Factory2.As(&Factory);
		if (FAILED(result)) return result;
	}

	ComPtr<ID3D11Device> Device;
	D3D_FEATURE_LEVEL FeatureLevels[]{
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
	};
	result = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, NULL, 0,
		FeatureLevels, ARRAYSIZE(FeatureLevels),
		D3D11_SDK_VERSION, &Device, nullptr, nullptr);
	if (FAILED(result)) return result;

	WindowCreated = CreateEventW(nullptr, TRUE, FALSE, nullptr);
	if (WindowCreated == NULL) {
		return GetLastError();
	}
	WindowThread = CreateThread(nullptr, 0, CreateHwndForTest, nullptr, 0, nullptr);
	if (WindowThread == NULL) {
		return GetLastError();
	}

	HANDLE WaitHandles[2]{ WindowCreated, WindowThread };
	if (WaitForMultipleObjects(2, WaitHandles, FALSE, INFINITE) != WAIT_OBJECT_0) {
		return GetLastError();
	}

	DWORD OldProtect;
	intptr_t* VTable;

	if (Factory2 != nullptr) {
		::DXGI_SWAP_CHAIN_DESC1 SwapChainDesc{};
		SwapChainDesc.Width = 640;
		SwapChainDesc.Height = 360;
		SwapChainDesc.Format = ::DXGI_FORMAT_R8G8B8A8_UNORM;
		SwapChainDesc.SampleDesc.Count = 1;
		SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		SwapChainDesc.BufferCount = 2;
		SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
		SwapChainDesc.Flags = 0;
		result = Factory2->CreateSwapChainForHwnd(Device.Get(), hWndForTest, &SwapChainDesc, nullptr, nullptr, &SwapChain1);
		if (FAILED(result)) return result;

		result = SwapChain1.As(&SwapChain);
		if (FAILED(result)) return result;
	}
	else {
		::DXGI_SWAP_CHAIN_DESC SwapChainDesc{};
		SwapChainDesc.BufferDesc.Width = 640;
		SwapChainDesc.BufferDesc.Height = 320;
		SwapChainDesc.BufferDesc.Format = ::DXGI_FORMAT_R8G8B8A8_UNORM;
		SwapChainDesc.SampleDesc.Count = 1;
		SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		SwapChainDesc.BufferCount = 2;
		SwapChainDesc.OutputWindow = hWndForTest;
		SwapChainDesc.Windowed = TRUE;
		SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
		SwapChainDesc.Flags = 0;
		result = Factory->CreateSwapChain(Device.Get(), &SwapChainDesc, &SwapChain);
		if (FAILED(result)) return result;
	}

	// saving original function pointers
	if (SwapChain1 != nullptr) {
		SwapChain1VTableAddress = *reinterpret_cast<intptr_t*>(SwapChain1.Get());
		VTable = reinterpret_cast<intptr_t*>(SwapChain1VTableAddress);
		IDXGISwapChain1_Present1_Original = reinterpret_cast<IDXGISwapChain1_Present1_Proc>(VTable[IDXGISwapChain1_Present1_VTableIndex]);
	}

	SwapChainVTableAddress = *reinterpret_cast<intptr_t*>(SwapChain.Get());
	VTable = reinterpret_cast<intptr_t*>(SwapChainVTableAddress);
	IDXGISwapChain_Present_Original = reinterpret_cast<IDXGISwapChain_Present_Proc>(VTable[IDXGISwapChain_Present_VTableIndex]);

	if (Factory2 != nullptr) {
		Factory2VTableAddress = *reinterpret_cast<intptr_t*>(Factory2.Get());
		VTable = reinterpret_cast<intptr_t*>(Factory2VTableAddress);
		IDXGIFactory2_CreateSwapChainForHwnd_Original = reinterpret_cast<IDXGIFactory2_CreateSwapChainForHwnd_Proc>(VTable[IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex]);
	}

	FactoryVTableAddress = *reinterpret_cast<intptr_t*>(Factory.Get());
	VTable = reinterpret_cast<intptr_t*>(FactoryVTableAddress);
	IDXGIFactory_CreateSwapChain_Original = reinterpret_cast<IDXGIFactory_CreateSwapChain_Proc>(VTable[IDXGIFactory_CreateSwapChain_VTableIndex]);

	bool Discord = false;
	result = DetectPreviousDetourHook();
	if (result == STATUS_ENTRYPOINT_NOT_FOUND) Discord = true;
	else if (result == E_FAIL) return GetLastError();
	else if (FAILED(result)) return result;

	// changing function pointers
	if (SwapChain1VTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(SwapChain1VTableAddress);

		if (!VirtualProtect(VTable + IDXGISwapChain1_Present1_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
		VTable[IDXGISwapChain1_Present1_VTableIndex] = reinterpret_cast<intptr_t>(IDXGISwapChain1_Present1_Override);
		if (!VirtualProtect(VTable + IDXGISwapChain1_Present1_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
	}

	VTable = reinterpret_cast<intptr_t*>(SwapChainVTableAddress);

	if (!VirtualProtect(VTable + IDXGISwapChain_Present_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
	VTable[IDXGISwapChain_Present_VTableIndex] = reinterpret_cast<intptr_t>(IDXGISwapChain_Present_Override);
	if (!VirtualProtect(VTable + IDXGISwapChain_Present_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();

	if (Factory2VTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(Factory2VTableAddress);

		if (!VirtualProtect(VTable + IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
		VTable[IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex] = reinterpret_cast<intptr_t>(IDXGIFactory2_CreateSwapChainForHwnd_Override);
		if (!VirtualProtect(VTable + IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
	}

	VTable = reinterpret_cast<intptr_t*>(FactoryVTableAddress);

	if (!VirtualProtect(VTable + IDXGIFactory_CreateSwapChain_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
	VTable[IDXGIFactory_CreateSwapChain_VTableIndex] = reinterpret_cast<intptr_t>(IDXGIFactory_CreateSwapChain_Override);
	if (!VirtualProtect(VTable + IDXGIFactory_CreateSwapChain_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
	
	SwapChain.Reset();
	Device.Reset();

	if (!PostMessageW(hWndForTest, WM_CLOSE, 0, 0)) {
		return GetLastError();
	}
	if (WaitForSingleObject(WindowThread, INFINITE) != WAIT_OBJECT_0) {
		return GetLastError();
	}

	CloseHandle(WindowCreated);
	CloseHandle(WindowThread);

	OnCompletionEvent = CreateEventW(nullptr, FALSE, FALSE, nullptr);
	AckCompletionEvent = CreateEventW(nullptr, FALSE, FALSE, nullptr);
	StopEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);

	return Discord ? ::Discord : result;
}

DWORD __stdcall UninstallHook()
{
	intptr_t* VTable = nullptr;
	DWORD OldProtect = 0;

	if (Factory2VTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(Factory2VTableAddress);

		if (IDXGIFactory2_CreateSwapChainForHwnd_Original != nullptr) {
			if (!VirtualProtect(VTable + IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
			VTable[IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex] = reinterpret_cast<intptr_t>(IDXGIFactory2_CreateSwapChainForHwnd_Original);
			if (!VirtualProtect(VTable + IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
		}
	}
	if (FactoryVTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(FactoryVTableAddress);

		if (IDXGIFactory_CreateSwapChain_Original != nullptr) {
			if (!VirtualProtect(VTable + IDXGIFactory_CreateSwapChain_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
			VTable[IDXGIFactory_CreateSwapChain_VTableIndex] = reinterpret_cast<intptr_t>(IDXGIFactory_CreateSwapChain_Original);
			if (!VirtualProtect(VTable + IDXGIFactory_CreateSwapChain_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
		}
	}

	if (SwapChain1VTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(SwapChain1VTableAddress);

		if (IDXGISwapChain1_Present1_Original != nullptr) {
			if (!VirtualProtect(VTable + IDXGISwapChain1_Present1_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
			VTable[IDXGISwapChain1_Present1_VTableIndex] = reinterpret_cast<intptr_t>(IDXGISwapChain1_Present1_Original);
			if (!VirtualProtect(VTable + IDXGISwapChain1_Present1_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
		}
	}
	if (SwapChainVTableAddress != 0) {
		VTable = reinterpret_cast<intptr_t*>(SwapChainVTableAddress);

		if (IDXGISwapChain_Present_Original != nullptr) {
			if (!VirtualProtect(VTable + IDXGISwapChain_Present_VTableIndex, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect)) return GetLastError();
			VTable[IDXGISwapChain_Present_VTableIndex] = reinterpret_cast<intptr_t>(IDXGISwapChain_Present_Original);
			if (!VirtualProtect(VTable + IDXGISwapChain_Present_VTableIndex, sizeof(intptr_t), OldProtect, &OldProtect)) return GetLastError();
		}
	}

	CloseHandle(OnCompletionEvent);
	CloseHandle(AckCompletionEvent);
	CloseHandle(StopEvent);

	return S_OK;
}

DWORD __stdcall SetRunning(bool Running)
{
	::Running = Running;
	if (!Running) {
		if (!SetEvent(StopEvent)) {
			return GetLastError();
		}
	}
	else {
		if (!ResetEvent(StopEvent)) {
			return GetLastError();
		}
	}
	return S_OK;
}

int __stdcall BeginProcessCompletionEvent()
{
	HANDLE Handles[2]{ OnCompletionEvent, StopEvent };
	if (WaitForMultipleObjects(2, Handles, FALSE, INFINITE) != WAIT_OBJECT_0) {
		return INT_MAX;
	}
	else return static_cast<int>(CompletionID);
}

DWORD __stdcall EndProcessCompletionEvent()
{
	CompletionID = INT_MAX;
	if (!SetEvent(AckCompletionEvent)) {
		return GetLastError();
	}
	return S_OK;
}

intptr_t __stdcall Get_DXGI_DLL_Address()
{
	return DXGI_DLL_BaseAddress;
}

DWORD __stdcall Get_DXGI_DLL_ImageSize()
{
	return DXGI_DLL_ImageSize;
}

intptr_t __stdcall Get_IDXGISwapChain_Present_Original()
{
	return reinterpret_cast<intptr_t>(IDXGISwapChain_Present_Original);
}

intptr_t __stdcall Get_IDXGISwapChain1_Present1_Original()
{
	return reinterpret_cast<intptr_t>(IDXGISwapChain1_Present1_Original);
}

intptr_t __stdcall Get_IDXGIFactory_CreateSwapChain_Original()
{
	return reinterpret_cast<intptr_t>(IDXGIFactory_CreateSwapChain_Original);
}

intptr_t __stdcall Get_IDXGIFactory2_CreateSwapChainForHwnd_Original()
{
	return reinterpret_cast<intptr_t>(IDXGIFactory2_CreateSwapChainForHwnd_Original);
}

void** __stdcall Get_LocalVariablesArray()
{
	return PtrList;
}

bool __stdcall Get_Present_PreviousDetourHookDetected()
{
	return Present_PreviousDetourHookDetected;
}

bool __stdcall Get_Present1_PreviousDetourHookDetected()
{
	return Present1_PreviousDetourHookDetected;
}

intptr_t __stdcall Get_GameOverlayRenderer64_DLL_Address()
{
	return GameOverlayRenderer64_DLL_BaseAddress;
}

DWORD __stdcall Get_GameOverlayRenderer64_DLL_ImageSize()
{
	return GameOverlayRenderer64_DLL_ImageSize;
}

bool __stdcall Get_Present_HasOriginalFirstFiveBytesOfInstruction()
{
	return Present_HasOriginalFirstFiveBytesOfInstruction;
}

bool __stdcall Get_Present1_HasOriginalFirstFiveBytesOfInstruction()
{
	return Present1_HasOriginalFirstFiveBytesOfInstruction;
}

bool __stdcall Get_Present_HasLoadedFirstFiveBytesOfInstruction()
{
	return Present_HasLoadedFirstFiveBytesOfInstruction;
}

bool __stdcall Get_Present1_HasLoadedFirstFiveBytesOfInstruction()
{
	return Present1_HasLoadedFirstFiveBytesOfInstruction;
}

void __stdcall Refresh_Present_LoadedFirstFiveBytesOfInstruction()
{
	CopyMemory(Present_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain_Present_Original, sizeof(Present_LoadedFirstFiveBytesOfInstruction));
}

void __stdcall Refresh_Present1_LoadedFirstFiveBytesOfInstruction()
{
	CopyMemory(Present1_LoadedFirstFiveBytesOfInstruction, IDXGISwapChain1_Present1_Original, sizeof(Present1_LoadedFirstFiveBytesOfInstruction));
}

intptr_t __stdcall Get_Present_OriginalFirstFiveBytesOfInstruction()
{
	return reinterpret_cast<intptr_t>(Present_OriginalFirstFiveBytesOfInstruction);
}

intptr_t __stdcall Get_Present1_OriginalFirstFiveBytesOfInstruction()
{
	return reinterpret_cast<intptr_t>(Present1_OriginalFirstFiveBytesOfInstruction);
}

intptr_t __stdcall Get_Present_LoadedFirstFiveBytesOfInstruction()
{
	return reinterpret_cast<intptr_t>(Present_LoadedFirstFiveBytesOfInstruction);
}

intptr_t __stdcall Get_Present1_LoadedFirstFiveBytesOfInstruction()
{
	return reinterpret_cast<intptr_t>(Present1_LoadedFirstFiveBytesOfInstruction);
}

constexpr BYTE OpCode_jmp = 0xE9;
constexpr BYTE OpCode_jmpFF = 0xFF;
BYTE LastInstruction = 0;
intptr_t LastAddress = 0;
DWORD LastError = 0;
int __stdcall JmpEndsUpInRange(intptr_t SrcAddr, intptr_t RangeStart, DWORD Size)
{
begin:
	intptr_t NewAddr = LastAddress = SrcAddr;

	DWORD OldProtect;
	if (!VirtualProtect(reinterpret_cast<void*>(NewAddr), 1, PAGE_EXECUTE_READWRITE, &OldProtect)) {
		LastError = GetLastError();
		return -1;
	}
	BYTE Instruction = LastInstruction = *reinterpret_cast<BYTE*>(NewAddr);
	if (!VirtualProtect(reinterpret_cast<void*>(NewAddr), 1, OldProtect, &OldProtect)) {
		LastError = GetLastError();
		return -1;
	}

	if (Instruction == OpCode_jmp) {
		// E9 XXXXXXXX
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 1), 4, PAGE_EXECUTE_READWRITE, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		int Offset = *reinterpret_cast<int*>(NewAddr + 1);
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 1), 4, OldProtect, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		SrcAddr += sizeof(BYTE) + sizeof(int) + Offset;
		goto begin;
	}
	if (Instruction == OpCode_jmpFF) {
		// FF25 00000000 XXXXXXXX_XXXXXXXX
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 1), 1, PAGE_EXECUTE_READWRITE, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		BYTE TwentyFive = *reinterpret_cast<BYTE*>(NewAddr + 1);
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 1), 1, OldProtect, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		if (TwentyFive != 25) { // I don't know what 25 means here though, I don't really know assembly code
			LastError = E_UNEXPECTED;
		}

		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 2), 1, PAGE_EXECUTE_READWRITE, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		int Zero = *reinterpret_cast<BYTE*>(NewAddr + 2);
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 2), 1, OldProtect, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		if (Zero != 0) { // I don't know what 0 means exactly
			LastError = E_UNEXPECTED;
		}
		
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 2 + sizeof(int)), 1, PAGE_EXECUTE_READWRITE, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		long long Addr = *reinterpret_cast<long long*>(NewAddr + 2 + sizeof(int));
		if (!VirtualProtect(reinterpret_cast<void*>(NewAddr + 2 + sizeof(int)), 1, OldProtect, &OldProtect)) {
			LastError = GetLastError();
			return -1;
		}
		SrcAddr = Addr; // absolute address

		goto begin;
	}

	if (NewAddr >= RangeStart && NewAddr < (RangeStart + Size)) {
		return 1;
	}
	return 0;
}

BYTE __stdcall JmpEndsUpInRange_LastInstruction()
{
	return LastInstruction;
}

intptr_t __stdcall JmpEndsUpInRange_LastAddress()
{
	return LastAddress;
}

DWORD __stdcall JmpEndsUpInRange_LastError()
{
	return LastError;
}
