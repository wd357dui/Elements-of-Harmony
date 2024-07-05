#include "pch.h"
#include <DirectXHook.h>

using namespace std;

CallbackProc HookCallback1 = nullptr;
CallbackProc HookCallback2 = nullptr;

atomic_bool Running = false;

extern "C" {
	__declspec(dllexport) void __stdcall ForceBreakpoint();
	__declspec(dllexport) LogCallbackProc LogCallback = nullptr;

	__declspec(dllexport) HRESULT __stdcall InstallHook();
	__declspec(dllexport) HRESULT __stdcall UninstallHook();
	__declspec(dllexport) void __stdcall SetRunning(bool Running);

	__declspec(dllexport) void __stdcall SetHookCallback1(CallbackProc HookCallback1);
	__declspec(dllexport) void __stdcall SetHookCallback2(CallbackProc HookCallback2);
	__declspec(dllexport) void __stdcall SetLogCallback(LogCallbackProc LogCallback);

	__declspec(dllexport) HRESULT __stdcall InstallHookForDevice(_In_ ID3D11Device* Device);
	__declspec(dllexport) HRESULT __stdcall InstallHookForDeviceContext(_In_ ID3D11DeviceContext* DeviceContext);
	__declspec(dllexport) HRESULT __stdcall InstallHookForFactory(_In_ IDXGIFactory* Factory, _In_opt_ IDXGIFactory2* Factory2);
	__declspec(dllexport) HRESULT __stdcall InstallHookForSwapChain(_In_ IDXGISwapChain* SwapChain, _In_opt_ IDXGISwapChain1* SwapChain1, _In_opt_ IDXGISwapChain3* SwapChain3, _In_opt_ IDXGISwapChain4* SwapChain4);
	
	__declspec(dllexport) HRESULT __stdcall UninstallHookForDevice();
	__declspec(dllexport) HRESULT __stdcall UninstallHookForDeviceContext();
	__declspec(dllexport) HRESULT __stdcall UninstallHookForFactory();
	__declspec(dllexport) HRESULT __stdcall UninstallHookForSwapChain();

	__declspec(dllexport) HRESULT __stdcall DetermineOutputHDR(_In_ IDXGISwapChain* SwapChain, _Out_ DXGI_FORMAT* Format, _Out_ bool* IsHDR);

	__declspec(dllexport) intptr_t __stdcall Get_Present_MemoryOriginal_Proc();
	__declspec(dllexport) intptr_t __stdcall Get_Present1_MemoryOriginal_Proc();
	__declspec(dllexport) BYTE* __stdcall Get_Present_MemoryOriginal_Bytes();
	__declspec(dllexport) BYTE* __stdcall Get_Present1_MemoryOriginal_Bytes();
	__declspec(dllexport) bool __stdcall Get_Present_DetourHookDetected();
	__declspec(dllexport) bool __stdcall Get_Present1_DetourHookDetected();

	__declspec(dllexport) intptr_t __stdcall Get_D3D11_DLL_BaseAddress();
	__declspec(dllexport) intptr_t __stdcall Get_DXGI_DLL_BaseAddress();
	__declspec(dllexport) intptr_t __stdcall Get_GameOverlayRenderer64_DLL_BaseAddress();
	__declspec(dllexport) DWORD __stdcall Get_D3D11_DLL_ImageSize();
	__declspec(dllexport) DWORD __stdcall Get_DXGI_DLL_ImageSize();
	__declspec(dllexport) DWORD __stdcall Get_GameOverlayRenderer64_DLL_ImageSize();

	/// <returns>1 for true, 0 for false, -1 for error</returns>
	__declspec(dllexport) bool __stdcall JmpEndsUpInRange(intptr_t SrcAddr, intptr_t RangeStart, DWORD Size);

	/// <summary>
	/// get debug name in a ID3D11Device or ID3D11DeviceChild object
	/// </summary>
	__declspec(dllexport) HRESULT __stdcall GetName(_In_ IUnknown* D3D11_Interface, _Out_ LPCSTR* ppCharArray);

	__declspec(dllexport) HRESULT __stdcall CompileVertexShader(_In_ ID3D11Device* Device, _In_ LPCWSTR FileName, _In_ LPCSTR EntryPoint,
		_In_opt_ LPCSTR DebugName, _Out_ ID3D11VertexShader** VertexShader);
	__declspec(dllexport) HRESULT __stdcall CompilePixelShader(_In_ ID3D11Device* Device, _In_ LPCWSTR FileName, _In_ LPCSTR EntryPoint,
		_In_opt_ LPCSTR DebugName, _Out_ ID3D11PixelShader** PixelShader);
	/*
	__declspec(dllexport) HRESULT __stdcall EnsureConstantBuffer(_In_ ID3D11Device* Device, _In_ ID3D11DeviceContext* DeviceContext,
		_Inout_ ID3D11Buffer** ConstantBuffer, _In_reads_(4) float* Values);
	*/
}

HRESULT STDMETHODCALLTYPE IDXGIFactory_CreateSwapChain_Override(IDXGIFactory* This,
	_In_  IUnknown* pDevice,
	_In_::DXGI_SWAP_CHAIN_DESC* pDesc,
	IDXGISwapChain** ppSwapChain);
HRESULT STDMETHODCALLTYPE IDXGIFactory2_CreateSwapChainForHwnd_Override(IDXGIFactory2* This,
	_In_  IUnknown* pDevice,
	_In_  HWND hWnd,
	_In_  const ::DXGI_SWAP_CHAIN_DESC1* pDesc,
	_In_opt_  const ::DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc,
	_In_opt_  IDXGIOutput* pRestrictToOutput,
	IDXGISwapChain1** ppSwapChain);
HRESULT STDMETHODCALLTYPE IDXGISwapChain_Present_Override(IDXGISwapChain* This, UINT SyncInterval, UINT Flags);
HRESULT STDMETHODCALLTYPE IDXGISwapChain_ResizeBuffers_Override(IDXGISwapChain* This,
	UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags);
HRESULT STDMETHODCALLTYPE IDXGISwapChain_ResizeTarget_Override(IDXGISwapChain* This,
	_In_ const DXGI_MODE_DESC* pNewTargetParameters);
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
HRESULT STDMETHODCALLTYPE ID3D11Device_CreateShaderResourceView_Override(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_SHADER_RESOURCE_VIEW_DESC* pDesc,
	ID3D11ShaderResourceView** ppSRView);
HRESULT STDMETHODCALLTYPE ID3D11Device_CreateRenderTargetView_Override(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_RENDER_TARGET_VIEW_DESC* pDesc,
	ID3D11RenderTargetView** ppRTView);
HRESULT STDMETHODCALLTYPE ID3D11Device_CreatePixelShader_Override(ID3D11Device* This,
	_In_reads_(BytecodeLength)  const void* pShaderBytecode, _In_  SIZE_T BytecodeLength,
	_In_opt_  ID3D11ClassLinkage* pClassLinkage,
	ID3D11PixelShader** ppPixelShader);
void STDMETHODCALLTYPE ID3D11DeviceContext_PSSetShader_Override(ID3D11DeviceContext* This,
	_In_opt_ ID3D11PixelShader* pPixelShader,
	_In_reads_opt_(NumClassInstances) ID3D11ClassInstance* const* ppClassInstances,
	UINT NumClassInstances);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexed_Override(ID3D11DeviceContext* This,
	_In_  UINT IndexCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation);
void STDMETHODCALLTYPE ID3D11DeviceContext_Draw_Override(ID3D11DeviceContext* This,
	_In_  UINT VertexCount,
	_In_  UINT StartVertexLocation);
void STDMETHODCALLTYPE ID3D11DeviceContext_PSSetConstantBuffers_Override(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1)  UINT StartSlot,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - StartSlot)  UINT NumBuffers,
	_In_reads_opt_(NumBuffers)  ID3D11Buffer* const* ppConstantBuffers);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexedInstanced_Override(ID3D11DeviceContext* This,
	_In_  UINT IndexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation,
	_In_  UINT StartInstanceLocation);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawInstanced_Override(ID3D11DeviceContext* This,
	_In_  UINT VertexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartVertexLocation,
	_In_  UINT StartInstanceLocation);
void STDMETHODCALLTYPE ID3D11DeviceContext_OMSetRenderTargets_Override(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)  UINT NumViews,
	_In_reads_opt_(NumViews)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView);
void STDMETHODCALLTYPE ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Override(ID3D11DeviceContext* This,
	_In_  UINT NumRTVs,
	_In_reads_opt_(NumRTVs)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView,
	_In_range_(0, D3D11_1_UAV_SLOT_COUNT - 1)  UINT UAVStartSlot,
	_In_  UINT NumUAVs,
	_In_reads_opt_(NumUAVs)  ID3D11UnorderedAccessView* const* ppUnorderedAccessViews,
	_In_reads_opt_(NumUAVs)  const UINT* pUAVInitialCounts);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawAuto_Override(ID3D11DeviceContext* This);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexedInstancedIndirect_Override(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs);
void STDMETHODCALLTYPE ID3D11DeviceContext_DrawInstancedIndirect_Override(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs);
HRESULT STDMETHODCALLTYPE IDXGISwapChain3_SetColorSpace1_Override(IDXGISwapChain3* This,
	_In_  DXGI_COLOR_SPACE_TYPE ColorSpace);
HRESULT STDMETHODCALLTYPE IDXGISwapChain4_SetHDRMetaData_Override(IDXGISwapChain4* This,
	_In_  DXGI_HDR_METADATA_TYPE Type,
	_In_  UINT Size,
	_In_reads_opt_(Size)  void* pMetaData);

intptr_t* VTableDeviceContext = nullptr;

struct DllInfo
{
	HMODULE		hModule = NULL;
	intptr_t	BaseAddress = 0;
	DWORD		ImageSize = 0;

	HRESULT Load(LPCWSTR DllName)
	{
		if (hModule == NULL) {
			hModule = GetModuleHandleW(DllName);
			if (hModule == NULL) return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		MODULEINFO Info{};
		if (!GetModuleInformation(GetCurrentProcess(), hModule, &Info, sizeof(MODULEINFO))) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		BaseAddress = reinterpret_cast<intptr_t>(Info.lpBaseOfDll);
		ImageSize = Info.SizeOfImage;
		return S_OK;
	}
} DXGI_DLL, D3D11_DLL, GameOverlayRenderer64_DLL;

struct FirstFiveBytes
{
	// the first 5 bytes of this function in memory before our hook is installed,
	// this byte code can be already replaced by other hooks,
	// for example, the steam overlay hook
	BYTE MemoryOriginalBytes[5]{ 0 };
	bool HasMemoryOriginalBytes = false;

	// the true original first 5 bytes of this function
	// determined by reading the original DLL file as binary file
	BYTE TrueOriginalBytes[5]{ 0 };
	bool HasTrueOriginalBytes = false;

	/// <summary>
	/// compare the original function in memory with byte code from the original file; 
	/// for a specified number of byte codes, excluding the first 5 bytes (which can hold a jmp instruction): 
	/// we match the byte code with the original file;
	/// If a match exists then we believe that we have found the original function's byte code;
	/// so next step we compare the first 5 bytes;
	/// if the first 5 bytes didn't match, then there must have been a detour hook installed prior to this point.
	/// if the first 5 bytes did match however, there were no detour hooks.
	/// </summary>
	/// <returns>if no byte code ever matched, returns STATUS_ENTRYPOINT_NOT_FOUND (didn't find the original function)</returns>
	HRESULT Init(intptr_t Proc, LPCWSTR DllPath)
	{
		CopyMemory(MemoryOriginalBytes, reinterpret_cast<void*>(Proc), 5);
		HasMemoryOriginalBytes = true;

		HANDLE DLL_File = CreateFileW(DllPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_READONLY, NULL);
		if (DLL_File == INVALID_HANDLE_VALUE) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}

		size_t CurrentPos = 0x80; // skip the "This program cannot be run in DOS mode" header that every dll have
		LARGE_INTEGER Large;
		if (!GetFileSizeEx(DLL_File, &Large)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}

		size_t FileSize = Large.QuadPart;
		while (((CurrentPos + InstructionCompareByteCount) < FileSize) && !HasTrueOriginalBytes) {
			Large.QuadPart = CurrentPos;
			if (!SetFilePointerEx(DLL_File, Large, nullptr, FILE_BEGIN)) {
				return E_FAIL;
			}
			BYTE Buffer[InstructionCompareByteCount]{ 0 };
			if (!ReadFile(DLL_File, Buffer, InstructionCompareByteCount, nullptr, nullptr)) {
				return E_FAIL;
			}
			if (RtlEqualMemory(reinterpret_cast<BYTE*>(Proc) + 5, Buffer + 5, InstructionCompareByteCount - 5)) {
				CopyMemory(TrueOriginalBytes, Buffer, 5);
				HasTrueOriginalBytes = true;
			}
			CurrentPos += 0x10; // it seems that the beginning position of every function are aligned to 0x10
		}
		if (!HasTrueOriginalBytes) {
			return STATUS_ENTRYPOINT_NOT_FOUND;
		}
		if (!CloseHandle(DLL_File)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		return S_OK;
	}

	bool DetectDetourHook() const
	{
		return HasMemoryOriginalBytes && HasTrueOriginalBytes && !RtlEqualMemory(MemoryOriginalBytes, TrueOriginalBytes, 5);
	}
	__declspec(property(get = DetectDetourHook)) bool DetourHookDetected;
} PresentBytes, Present1Bytes;

template <typename ProcType, typename ...ArgTypes>
concept ReturnsHResult = requires(ProcType proc, ArgTypes ...args) {
	{ proc(args...) } -> std::same_as<HRESULT>;
};

struct VTableHook
{
	using Delegate = intptr_t;

	Delegate* PointerInVTable = nullptr;
	Delegate MemoryOriginalProc = 0;
	Delegate Override = 0;

	HRESULT Init(Delegate* PointerInVTable, Delegate Override)
	{
		this->PointerInVTable = PointerInVTable;
		MemoryOriginalProc = *PointerInVTable;
		this->Override = Override;
		DWORD OldProtect = 0;
		if (!VirtualProtect(PointerInVTable, sizeof(Delegate), PAGE_EXECUTE_READWRITE, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), 5, PAGE_EXECUTE_READWRITE, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		return S_OK;
	}
	inline bool getInitialized() const
	{
		return PointerInVTable != nullptr && MemoryOriginalProc != 0 && Override != 0;
	}
	__declspec(property(get = getInitialized)) bool Initialized;

	HRESULT Patch()
	{
		DWORD OldProtect = 0;
		*PointerInVTable = Override;
		return S_OK;
	}
	HRESULT UnPatch()
	{
		DWORD OldProtect = 0;
		*PointerInVTable = MemoryOriginalProc;
		return S_OK;
	}
	/// <summary>
	/// used for fixing stack overflow error caused by Steam overlay's detour hook
	/// </summary>
	HRESULT RestoreMemoryOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasMemoryOriginalBytes) {
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.MemoryOriginalBytes, 5);
			return S_OK;
		}
		else return E_PENDING;
	}
	/// <summary>
	/// used for fixing stack overflow error caused by Steam overlay's detour hook
	/// </summary>
	HRESULT RestoreTrueOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasTrueOriginalBytes) {
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.TrueOriginalBytes, 5);
			return S_OK;
		}
		else return E_PENDING;
	}

	template <typename ProcType, typename ...ArgTypes>
		requires ReturnsHResult<ProcType, ArgTypes...>
	HRESULT InvokeHResult(ArgTypes ...Args)
	{
		return reinterpret_cast<ProcType>(MemoryOriginalProc)(Args...);
	}

	template <typename ProcType, typename ...ArgTypes>
	void Invoke(ArgTypes ...Args)
	{
		reinterpret_cast<ProcType>(MemoryOriginalProc)(Args...);
	}

} CreateSwapChain, CreateSwapChainForHwnd,
//ResizeBuffers,
ResizeTarget,
Present, Present1,
CreateShaderResourceView, CreateRenderTargetView, CreatePixelShader,
//PSSetShader, DrawIndexed, Draw, PSSetConstantBuffers, DrawIndexedInstanced, DrawInstanced, OMSetRenderTargets, OMSetRenderTargetsAndUnorderedAccessViews, DrawAuto, DrawIndexedInstancedIndirect, DrawInstancedIndirect,
SetColorSpace1, SetHDRMetaData;

struct DetourHook
{
	using Delegate = intptr_t;

	Delegate MemoryOriginalProc = 0;
	Delegate Override = 0;

	BYTE MemoryOriginalBytes[14]{ 0 };
	bool HasMemoryOriginalBytes = false;

	HRESULT Init(Delegate* PointerInVTable, Delegate Override)
	{
		MemoryOriginalProc = *PointerInVTable;
		this->Override = Override;

		DWORD OldProtect = 0;
		if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), sizeof(Delegate), PAGE_EXECUTE_READWRITE, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		CopyMemory(MemoryOriginalBytes, reinterpret_cast<void*>(MemoryOriginalProc), 14);
		HasMemoryOriginalBytes = true;
		return S_OK;
	}
	inline bool getInitialized() const
	{
		return MemoryOriginalProc != 0 && Override != 0 && HasMemoryOriginalBytes;
	}
	__declspec(property(get = getInitialized)) bool Initialized;

	HRESULT Patch() const
	{
		BYTE DetourJump64[14]{
			0xFF, 0x25,
			0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		};
		*reinterpret_cast<intptr_t*>(DetourJump64 + 6) = Override;
		CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), DetourJump64, 14);
		return S_OK;
	}
	HRESULT UnPatch() const
	{
		CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), MemoryOriginalBytes, 14);
		return S_OK;
	}
	
	/// <summary>
	/// used for fixing stack overflow error caused by Steam overlay's detour hook
	/// </summary>
	HRESULT RestoreMemoryOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasMemoryOriginalBytes) {
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.MemoryOriginalBytes, 5);
			return S_OK;
		}
		else return E_PENDING;
	}
	/// <summary>
	/// used for fixing stack overflow error caused by Steam overlay's detour hook
	/// </summary>
	HRESULT RestoreTrueOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasTrueOriginalBytes) {
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.TrueOriginalBytes, 5);
			return S_OK;
		}
		else return E_PENDING;
	}

	template <typename ProcType, typename ...ArgTypes>
		requires ReturnsHResult<ProcType, ArgTypes...>
	HRESULT InvokeHResult(ArgTypes ...Args)
	{
		HRESULT result2 = UnPatch();
		if (FAILED(result2)) {
			LogCallback((L"DetourHook::UnPatch failed, return value " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
		HRESULT result = reinterpret_cast<ProcType>(MemoryOriginalProc)(Args...);
		result2 = Patch();
		if (FAILED(result2)) {
			LogCallback((L"DetourHook::UnPatch failed, return value " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
		return result;
	}

	template <typename ProcType, typename ...ArgTypes>
	void Invoke(ArgTypes ...Args)
	{
		HRESULT result2 = UnPatch();
		if (FAILED(result2)) {
			LogCallback((L"DetourHook::UnPatch failed, return value " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
		reinterpret_cast<ProcType>(MemoryOriginalProc)(Args...);
		result2 = Patch();
		if (FAILED(result2)) {
			LogCallback((L"DetourHook::UnPatch failed, return value " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}

} ResizeBuffers,
PSSetShader, DrawIndexed, Draw, PSSetConstantBuffers, DrawIndexedInstanced, DrawInstanced, OMSetRenderTargets, OMSetRenderTargetsAndUnorderedAccessViews, DrawAuto, DrawIndexedInstancedIndirect, DrawInstancedIndirect;

static inline wstring Convert(string MultiByte)
{
	vector<WCHAR> Wide;
	Wide.assign(MultiByte.size() + 1, L'\0');
	int Result = 0;
	if ((Result = MultiByteToWideChar(CP_UTF8, 0, MultiByte.c_str(), static_cast<int>(MultiByte.size()), Wide.data(), static_cast<int>(Wide.size())) == 0)) {
		if (*--Wide.end() != L'\0') Wide.push_back(L'\0');
		return Wide.data();
	}
	else {
		return L" [failed to parse message, MultiByteToWideChar returns value " + to_wstring(Result) + L"] ";
	}
}

/* 2024-7-2
* by looking at memory region with Cheat Engine,
* I've confirmed that the VTable of ID3D11DeviceContext is constantly being updated,
* which means it is dynamically assembled, which explains why it is in allocated memory
* instead of within D3D11.dll or any DLL's memory space
* 
* this is a problem because it's effectively constantly un-patching my VTable hooks,
* and calling this function `EnsureDeviceContextPatch` every frame does not fix the issue;
* I'm guessing that the VTable is being updated each time a ID3D11DeviceContext (or its inheritance)
* had been "QueryInterface"-ed or "AddRef"-ed
* 
* to fix this issue, I see 2 possible solutions:
* 1. use detour hook instead
* 2. we maintain one our own VTable array for ID3D11DeviceContext,
*    hook ID3D11Device::GetImmediateContext and IUnknown::QueryInterface to
*    detect every creation of a ID3D11DeviceContext reference,
*    and replace each one of them's VTable array pointer to ours;
*    the downside is that, in order to not miss the creation of a ID3D11DeviceContext
*    when D3D11CreateDevice is called,
*    we would have to make use of our imposter dll `D3D11.dll`
*    (which was originally intended on enabling debug layer in debug only builds)
*    and keep it in the release build
* 
* and... I've decided to go with detour hook
*
static inline HRESULT EnsureDeviceContextPatch(ID3D11Device* Device)
{
	HRESULT result = 0;

	ComPtr<ID3D11DeviceContext> DeviceContext;
	Device->GetImmediateContext(&DeviceContext);

	intptr_t* CurrentVTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(DeviceContext.Get()));
	if (VTableDeviceContext != CurrentVTable) {
		result = UninstallHookForDeviceContext();
		if (FAILED(result)) return result;
		result = InstallHookForDeviceContext(DeviceContext.Get());
		if (FAILED(result)) return result;
	}

	if (PSSetShader.Initialized) {
		result = PSSetShader.Patch();
		if (FAILED(result)) return result;
	}

	if (DrawIndexed.Initialized) {
		result = DrawIndexed.Patch();
		if (FAILED(result)) return result;
	}
	
	if (Draw.Initialized) {
		result = Draw.Patch();
		if (FAILED(result)) return result;
	}
	
	if (PSSetConstantBuffers.Initialized) {
		result = PSSetConstantBuffers.Patch();
		if (FAILED(result)) return result;
	}
	
	if (DrawIndexedInstanced.Initialized) {
		result = DrawIndexedInstanced.Patch();
		if (FAILED(result)) return result;
	}
	
	if (DrawInstanced.Initialized) {
		result = DrawInstanced.Patch();
		if (FAILED(result)) return result;
	}

	if (OMSetRenderTargets.Initialized) {
		result = OMSetRenderTargets.Patch();
		if (FAILED(result)) return result;
	}

	if (OMSetRenderTargetsAndUnorderedAccessViews.Initialized) {
		result = OMSetRenderTargetsAndUnorderedAccessViews.Patch();
		if (FAILED(result)) return result;
	}
	
	if (DrawAuto.Initialized) {
		result = DrawAuto.Patch();
		if (FAILED(result)) return result;
	}
	
	if (DrawIndexedInstancedIndirect.Initialized) {
		result = DrawIndexedInstancedIndirect.Patch();
		if (FAILED(result)) return result;
	}
	
	if (DrawInstancedIndirect.Initialized) {
		result = DrawInstancedIndirect.Patch();
		if (FAILED(result)) return result;
	}

	return result;
}
*/

static inline void HookCallback(Arguments* Args)
{
	if (HookCallback1 != nullptr) HookCallback1(Args);
	if (HookCallback2 != nullptr) HookCallback2(Args);
}

HRESULT DummyCreateDirectXInstances(
	ID3D11Device** Device, ID3D11DeviceContext** Context,
	IDXGIFactory** Factory, IDXGIFactory2** Factory2,
	IDXGISwapChain** SwapChain, IDXGISwapChain1** SwapChain1, IDXGISwapChain3** SwapChain3, IDXGISwapChain4** SwapChain4);
HRESULT DummyRelease();

HRESULT __stdcall InstallHook()
{
	HRESULT result = 0;

	result = DXGI_DLL.Load(L"DXGI.dll");
	if (FAILED(result)) return result;

	result = D3D11_DLL.Load(L"D3D11.dll");
	if (FAILED(result)) return result;

	result = GameOverlayRenderer64_DLL.Load(L"GameOverlayRenderer64.dll");
	if (FAILED(result)) {
		LogCallback(L"GameOverlayRenderer64.dll (the Steam overlay hook) is not loaded");
	}
	else LogCallback(L"GameOverlayRenderer64.dll (the Steam overlay hook) is loaded");

	ComPtr<ID3D11Device> Device;
	ComPtr<ID3D11DeviceContext> DeviceContext;

	ComPtr<IDXGIFactory> Factory;
	ComPtr<IDXGIFactory2> Factory2;

	ComPtr<IDXGISwapChain> SwapChain;
	ComPtr<IDXGISwapChain1> SwapChain1;
	ComPtr<IDXGISwapChain3> SwapChain3;
	ComPtr<IDXGISwapChain4> SwapChain4;

	result = DummyCreateDirectXInstances(&Device, &DeviceContext,
		&Factory, &Factory2,
		&SwapChain, &SwapChain1, &SwapChain3, &SwapChain4);
	if (FAILED(result)) return result;

	result = InstallHookForDevice(Device.Get());
	if (FAILED(result)) return result;
	
	result = InstallHookForDeviceContext(DeviceContext.Get());
	if (FAILED(result)) return result;
	
	result = InstallHookForFactory(Factory.Get(), Factory2.Get());
	if (FAILED(result)) return result;
	
	result = InstallHookForSwapChain(SwapChain.Get(), SwapChain1.Get(), SwapChain3.Get(), SwapChain4.Get());
	if (FAILED(result)) return result;

	return result;
}

HRESULT __stdcall UninstallHook()
{
	HRESULT result = 0;

	result = UninstallHookForFactory();
	if (FAILED(result)) return result;

	result = UninstallHookForSwapChain();
	if (FAILED(result)) return result;

	result = UninstallHookForDevice();
	if (FAILED(result)) return result;

	result = UninstallHookForDeviceContext();
	if (FAILED(result)) return result;

	return result;
}

void __stdcall SetRunning(bool Running)
{
	::Running = Running;
}

__declspec(dllexport) void __stdcall SetHookCallback1(CallbackProc HookCallback1)
{
	::HookCallback1 = HookCallback1;
}

__declspec(dllexport) void __stdcall SetHookCallback2(CallbackProc HookCallback2)
{
	::HookCallback2 = HookCallback2;
}

__declspec(dllexport) void __stdcall SetLogCallback(LogCallbackProc LogCallback)
{
	::LogCallback = LogCallback;
}

#define InitHook(Name, PointerInVTable, Override) \
result = Name.Init(PointerInVTable, Override); \
if (FAILED(result)) return result;

HRESULT __stdcall InstallHookForDevice(_In_ ID3D11Device* Device)
{
	HRESULT result = 0;
	intptr_t* VTable = nullptr;
	int NumHooks = 0;

	VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(Device));
	InitHook(CreateShaderResourceView, &VTable[ID3D11Device_CreateShaderResourceView_VTableIndex], reinterpret_cast<intptr_t>(ID3D11Device_CreateShaderResourceView_Override));
	InitHook(CreateRenderTargetView, &VTable[ID3D11Device_CreateRenderTargetView_VTableIndex], reinterpret_cast<intptr_t>(ID3D11Device_CreateRenderTargetView_Override));
	InitHook(CreatePixelShader, &VTable[ID3D11Device_CreatePixelShader_VTableIndex], reinterpret_cast<intptr_t>(ID3D11Device_CreatePixelShader_Override));

	if (CreateShaderResourceView.Initialized) {
		result = CreateShaderResourceView.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (CreateRenderTargetView.Initialized) {
		result = CreateRenderTargetView.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (CreatePixelShader.Initialized) {
		result = CreatePixelShader.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"InstallHookForDevice complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall InstallHookForDeviceContext(_In_ ID3D11DeviceContext* DeviceContext)
{
	HRESULT result = 0;
	intptr_t* VTable = nullptr;
	int NumHooks = 0;

	VTable = VTableDeviceContext = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(DeviceContext));
	InitHook(PSSetShader, &VTable[ID3D11DeviceContext_PSSetShader_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_PSSetShader_Override));
	InitHook(DrawIndexed, &VTable[ID3D11DeviceContext_DrawIndexed_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawIndexed_Override));
	InitHook(Draw, &VTable[ID3D11DeviceContext_Draw_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_Draw_Override));
	InitHook(PSSetConstantBuffers, &VTable[ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_PSSetConstantBuffers_Override));
	InitHook(DrawIndexedInstanced, &VTable[ID3D11DeviceContext_DrawIndexedInstanced_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawIndexedInstanced_Override));
	InitHook(DrawInstanced, &VTable[ID3D11DeviceContext_DrawInstanced_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawInstanced_Override));
	InitHook(OMSetRenderTargets, &VTable[ID3D11DeviceContext_OMSetRenderTargets_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_OMSetRenderTargets_Override));
	InitHook(OMSetRenderTargetsAndUnorderedAccessViews, &VTable[ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Override));
	InitHook(DrawAuto, &VTable[ID3D11DeviceContext_DrawAuto_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawAuto_Override));
	InitHook(DrawIndexedInstancedIndirect, &VTable[ID3D11DeviceContext_DrawIndexedInstancedIndirect_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawIndexedInstancedIndirect_Override));
	InitHook(DrawInstancedIndirect, &VTable[ID3D11DeviceContext_DrawInstancedIndirect_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_DrawInstancedIndirect_Override));

	if (PSSetShader.Initialized) {
		result = PSSetShader.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexed.Initialized) {
		result = DrawIndexed.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (Draw.Initialized) {
		result = Draw.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (PSSetConstantBuffers.Initialized) {
		result = PSSetConstantBuffers.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexedInstanced.Initialized) {
		result = DrawIndexedInstanced.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawInstanced.Initialized) {
		result = DrawInstanced.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (OMSetRenderTargets.Initialized) {
		result = OMSetRenderTargets.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (OMSetRenderTargetsAndUnorderedAccessViews.Initialized) {
		result = OMSetRenderTargetsAndUnorderedAccessViews.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawAuto.Initialized) {
		result = DrawAuto.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexedInstancedIndirect.Initialized) {
		result = DrawIndexedInstancedIndirect.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawInstancedIndirect.Initialized) {
		result = DrawInstancedIndirect.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"InstallHookForDeviceContext complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall InstallHookForFactory(_In_ IDXGIFactory* Factory, _In_opt_ IDXGIFactory2* Factory2)
{
	HRESULT result = 0;
	intptr_t* VTable = nullptr;
	int NumHooks = 0;

	VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(Factory));
	InitHook(CreateSwapChain, &VTable[IDXGIFactory_CreateSwapChain_VTableIndex], reinterpret_cast<intptr_t>(IDXGIFactory_CreateSwapChain_Override));

	if (CreateSwapChain.Initialized) {
		result = CreateSwapChain.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (Factory2 != nullptr) {
		VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(Factory2));
		InitHook(CreateSwapChainForHwnd, &VTable[IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex], reinterpret_cast<intptr_t>(IDXGIFactory2_CreateSwapChainForHwnd_Override));

		result = CreateSwapChainForHwnd.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"InstallHookForFactory complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall InstallHookForSwapChain(_In_ IDXGISwapChain* SwapChain, _In_opt_ IDXGISwapChain1* SwapChain1, _In_opt_ IDXGISwapChain3* SwapChain3, _In_opt_ IDXGISwapChain4* SwapChain4)
{
	HRESULT result = 0;
	intptr_t* VTable = nullptr;
	int NumHooks = 0;

	VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(SwapChain));
	InitHook(Present, &VTable[IDXGISwapChain_Present_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain_Present_Override));
	result = PresentBytes.Init(VTable[IDXGISwapChain_Present_VTableIndex], L"C:/Windows/System32/DXGI.dll");
	if (result == STATUS_ENTRYPOINT_NOT_FOUND) {
		LogCallback(L"failed to find true original byte code for IDXGISwapChain::Present, won't be able to fix stack overflow error if steam overlay hook is present");
	}
	else if (FAILED(result)) return result;
	else result = S_OK;

	InitHook(ResizeBuffers, &VTable[IDXGISwapChain_ResizeBuffers_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain_ResizeBuffers_Override));
	InitHook(ResizeTarget, &VTable[IDXGISwapChain_ResizeTarget_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain_ResizeTarget_Override));

	if (Present.Initialized) {
		result = Present.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (ResizeBuffers.Initialized) {
		result = ResizeBuffers.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (ResizeTarget.Initialized) {
		result = ResizeTarget.Patch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (SwapChain1 != nullptr) {
		VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(SwapChain1));
		InitHook(Present1, &VTable[IDXGISwapChain1_Present1_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain1_Present1_Override));
		result = Present1Bytes.Init(VTable[IDXGISwapChain1_Present1_VTableIndex], L"C:/Windows/System32/DXGI.dll");
		if (result == STATUS_ENTRYPOINT_NOT_FOUND) {
			LogCallback(L"failed to find true original byte code for IDXGISwapChain1::Present1, won't be able to fix stack overflow error if steam overlay hook is present");
		}
		else if (FAILED(result)) return result;
		else result = S_OK;

		if (Present1.Initialized) {
			result = Present1.Patch();
			if (FAILED(result)) return result;
			NumHooks++;
		}
	}

	if (SwapChain3 != nullptr) {
		VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(SwapChain3));
		InitHook(SetColorSpace1, &VTable[IDXGISwapChain3_SetColorSpace1_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain3_SetColorSpace1_Override));

		if (SetColorSpace1.Initialized) {
			result = SetColorSpace1.Patch();
			if (FAILED(result)) return result;
			NumHooks++;
		}
	}

	if (SwapChain4 != nullptr) {
		VTable = reinterpret_cast<intptr_t*>(*reinterpret_cast<intptr_t*>(SwapChain4));
		InitHook(SetHDRMetaData, &VTable[IDXGISwapChain4_SetHDRMetaData_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain4_SetHDRMetaData_Override));

		if (SetHDRMetaData.Initialized) {
			result = SetHDRMetaData.Patch();
			if (FAILED(result)) return result;
			NumHooks++;
		}
	}

	LogCallback((L"InstallHookForSwapChain complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall UninstallHookForDevice()
{
	HRESULT result = 0;
	int NumHooks = 0;

	if (CreatePixelShader.Initialized) {
		result = CreatePixelShader.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}
	
	if (CreateShaderResourceView.Initialized) {
		result = CreateShaderResourceView.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (CreateRenderTargetView.Initialized) {
		result = CreateRenderTargetView.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"UninstallHookForDevice complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall UninstallHookForDeviceContext()
{
	HRESULT result = 0;
	int NumHooks = 0;

	if (PSSetShader.Initialized) {
		result = PSSetShader.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexed.Initialized) {
		result = DrawIndexed.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (Draw.Initialized) {
		result = Draw.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (PSSetConstantBuffers.Initialized) {
		result = PSSetConstantBuffers.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexedInstanced.Initialized) {
		result = DrawIndexedInstanced.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawInstanced.Initialized) {
		result = DrawInstanced.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (OMSetRenderTargets.Initialized) {
		result = OMSetRenderTargets.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (OMSetRenderTargetsAndUnorderedAccessViews.Initialized) {
		result = OMSetRenderTargetsAndUnorderedAccessViews.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawAuto.Initialized) {
		result = DrawAuto.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawIndexedInstancedIndirect.Initialized) {
		result = DrawIndexedInstancedIndirect.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (DrawInstancedIndirect.Initialized) {
		result = DrawInstancedIndirect.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"UninstallHookForDeviceContext complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall UninstallHookForFactory()
{
	HRESULT result = 0;
	int NumHooks = 0;

	if (CreateSwapChain.Initialized) {
		result = CreateSwapChain.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (CreateSwapChainForHwnd.Initialized) {
		result = CreateSwapChainForHwnd.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"UninstallHookForFactory complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

HRESULT __stdcall UninstallHookForSwapChain()
{
	HRESULT result = 0;
	int NumHooks = 0;

	if (Present.Initialized) {
		result = Present.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (ResizeBuffers.Initialized) {
		result = ResizeBuffers.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (ResizeTarget.Initialized) {
		result = ResizeTarget.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (Present1.Initialized) {
		result = Present1.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (SetColorSpace1.Initialized) {
		result = SetColorSpace1.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	if (SetHDRMetaData.Initialized) {
		result = SetHDRMetaData.UnPatch();
		if (FAILED(result)) return result;
		NumHooks++;
	}

	LogCallback((L"UninstallHookForSwapChain complete, " + to_wstring(NumHooks) + L" hooks").c_str());

	return result;
}

ComPtr<IDXGIFactory1> Factory = nullptr;
map<HWND, RECT> WindowRects;
map<HWND, HMONITOR> PreviousMonitor;
map<HWND, bool> PreviousHDR;
map<HMONITOR, ComPtr<IDXGIOutput6>> Monitors;

inline static std::wstring DXGIColorSpaceTypeToString(DXGI_COLOR_SPACE_TYPE Type)
{
	switch (Type)
	{
	case DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709:
		return L"DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709";
	case DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709:
		return L"DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709";
	case DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P709:
		return L"DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P709";
	case DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P2020:
		return L"DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P2020";
	case DXGI_COLOR_SPACE_RESERVED:
		return L"DXGI_COLOR_SPACE_RESERVED";
	case DXGI_COLOR_SPACE_YCBCR_FULL_G22_NONE_P709_X601:
		return L"DXGI_COLOR_SPACE_YCBCR_FULL_G22_NONE_P709_X601";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P601:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P601";
	case DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P601:
		return L"DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P601";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P709:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P709";
	case DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P709:
		return L"DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P709";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P2020";
	case DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P2020";
	case DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020:
		return L"DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_LEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_LEFT_P2020";
	case DXGI_COLOR_SPACE_RGB_STUDIO_G2084_NONE_P2020:
		return L"DXGI_COLOR_SPACE_RGB_STUDIO_G2084_NONE_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_TOPLEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_TOPLEFT_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_TOPLEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_TOPLEFT_P2020";
	case DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P2020:
		return L"DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_GHLG_TOPLEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_GHLG_TOPLEFT_P2020";
	case DXGI_COLOR_SPACE_YCBCR_FULL_GHLG_TOPLEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_FULL_GHLG_TOPLEFT_P2020";
	case DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P709:
		return L"DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P709";
	case DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P2020:
		return L"DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P709:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P709";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P2020";
	case DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_TOPLEFT_P2020:
		return L"DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_TOPLEFT_P2020";
	case DXGI_COLOR_SPACE_CUSTOM:
		return L"DXGI_COLOR_SPACE_CUSTOM";
	default:
		return L"(invalid value " + to_wstring(Type) + L")";
	}
}

inline static std::wstring DXGIModeRotationToString(DXGI_MODE_ROTATION Rotation)
{
	switch (Rotation)
	{
	case DXGI_MODE_ROTATION_UNSPECIFIED:
		return L"DXGI_MODE_ROTATION_UNSPECIFIED";
	case DXGI_MODE_ROTATION_IDENTITY:
		return L"DXGI_MODE_ROTATION_IDENTITY";
	case DXGI_MODE_ROTATION_ROTATE90:
		return L"DXGI_MODE_ROTATION_ROTATE90";
	case DXGI_MODE_ROTATION_ROTATE180:
		return L"DXGI_MODE_ROTATION_ROTATE180";
	case DXGI_MODE_ROTATION_ROTATE270:
		return L"DXGI_MODE_ROTATION_ROTATE270";
	default:
		return L"(invalid value " + to_wstring(Rotation) + L")";
	}
}

inline static std::wstring DXGI_OUTPUT_DESC1_ToString(DXGI_OUTPUT_DESC1& Desc1)
{
	std::wostringstream Info;
	WCHAR DeviceNameCharacters[33] = L"";
	CopyMemory(DeviceNameCharacters, Desc1.DeviceName, 32 * sizeof(WCHAR));
	Info << L"DXGI_OUTPUT_DESC1 info:\r\n";
	Info << L"DeviceName=" << DeviceNameCharacters << L"\r\n";
	Info << L"DesktopCoordinates: Left=" << Desc1.DesktopCoordinates.left << L", Top=" << Desc1.DesktopCoordinates.top
		<< L", Right=" << Desc1.DesktopCoordinates.right << L", Bottom=" << Desc1.DesktopCoordinates.bottom << L"\r\n";
	Info << L"AttachedToDesktop=" << (Desc1.AttachedToDesktop ? L"true" : L"false") << L"\r\n";
	Info << L"Rotation=" << DXGIModeRotationToString(Desc1.Rotation) << L"\r\n";
	Info << L"MonitorHandle=" << Desc1.Monitor << L"\r\n";
	Info << L"BitsPerColor=" << Desc1.BitsPerColor << L"\r\n";
	Info << L"ColorSpace=" << DXGIColorSpaceTypeToString(Desc1.ColorSpace) << L"\r\n";
	Info << L"RedPrimary: [" << Desc1.RedPrimary[0] << L", " << Desc1.RedPrimary[1] << L"]\r\n";
	Info << L"GreenPrimary: [" << Desc1.GreenPrimary[0] << L", " << Desc1.GreenPrimary[1] << L"]\r\n";
	Info << L"BluePrimary: [" << Desc1.BluePrimary[0] << L", " << Desc1.BluePrimary[1] << L"]\r\n";
	Info << L"WhitePoint: [" << Desc1.WhitePoint[0] << L", " << Desc1.WhitePoint[1] << L"]\r\n";
	Info << L"MinLuminance=" << Desc1.MinLuminance << L"\r\n";
	Info << L"MaxLuminance=" << Desc1.MaxLuminance << L"\r\n";
	Info << L"MaxFullFrameLuminance=" << Desc1.MaxFullFrameLuminance;
	return Info.str();
}

HRESULT __stdcall DetermineOutputHDR(_In_ IDXGISwapChain* SwapChain, _Out_ DXGI_FORMAT* Format, _Out_ bool* IsHDR)
{
	HRESULT result = 0;

	(*IsHDR) = false;

	DXGI_SWAP_CHAIN_DESC Desc{};
	result = SwapChain->GetDesc(&Desc);
	if (FAILED(result)) return result;

	if ((*Format = Desc.BufferDesc.Format) != DXGI_FORMAT_R16G16B16A16_FLOAT) {
		(*IsHDR) = false;
		return S_OK;
	}

	HWND hWnd = Desc.OutputWindow;

	RECT CurrentRect{};
	if (!GetWindowRect(hWnd, &CurrentRect)) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}

	bool ReCheckNeeded;
	auto WindowRectIterator = WindowRects.find(hWnd);
	if (WindowRectIterator == WindowRects.end()) {
		WindowRects[hWnd] = CurrentRect;
		ReCheckNeeded = true;
	}
	else {
		RECT PreviousRect = (*WindowRectIterator).second;
		WindowRectIterator->second = CurrentRect;
		ReCheckNeeded = CurrentRect == PreviousRect;
	}

	if (!ReCheckNeeded) {
		*IsHDR = PreviousHDR[hWnd];
		return S_OK;
	}

	HMONITOR hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
	auto MonitorIterator = Monitors.find(hMonitor);

	bool FactoryRecreateNeeded = Factory == nullptr || !Factory->IsCurrent();
	bool ReEnumerationNeeded = FactoryRecreateNeeded || MonitorIterator == Monitors.end();
	bool DifferentMonitor = PreviousMonitor[hWnd] != hMonitor;

	PreviousMonitor[hWnd] = hMonitor;

	if (FactoryRecreateNeeded) {
		result = CreateDXGIFactory1(IID_PPV_ARGS(&Factory));
		if (FAILED(result)) return result;
	}

	if (ReEnumerationNeeded) {
		UINT NumAdapter = 0;
		ComPtr<IDXGIAdapter> Adapter;

	AdapterLoop:
		result = Factory->EnumAdapters(NumAdapter++, &Adapter);
		if (result == S_OK) {

			UINT NumOutput = 0;
			ComPtr<IDXGIOutput> Output;

		OutputLoop:
			result = Adapter->EnumOutputs(NumOutput++, &Output);
			if (result == S_OK) {

				ComPtr<IDXGIOutput6> Output6;
				DXGI_OUTPUT_DESC1 Desc1{};

				result = Output.As(&Output6);
				if (FAILED(result)) return result;
				result = Output6->GetDesc1(&Desc1);
				if (FAILED(result)) return result;

				MonitorIterator = Monitors.insert_or_assign(Desc1.Monitor, Output6).first;
				goto OutputLoop;
			}
			else if (result != DXGI_ERROR_NOT_FOUND) return result;
			goto AdapterLoop;
		}
		else if (result != DXGI_ERROR_NOT_FOUND) return result;
	}

	DXGI_OUTPUT_DESC1 Desc1{};
	result = (*MonitorIterator).second->GetDesc1(&Desc1);
	if (FAILED(result)) return result;

	bool CurrentHDR = Desc1.ColorSpace == DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020;

	if (ReEnumerationNeeded || DifferentMonitor) {
		LogCallback(DXGI_OUTPUT_DESC1_ToString(Desc1).c_str());
	}

	*IsHDR = PreviousHDR[hWnd] = CurrentHDR;

	return S_OK;
}

intptr_t __stdcall Get_Present_MemoryOriginal_Proc()
{
	return Present.MemoryOriginalProc;
}

intptr_t __stdcall Get_Present1_MemoryOriginal_Proc()
{
	return Present1.MemoryOriginalProc;
}

BYTE* __stdcall Get_Present_MemoryOriginal_Bytes()
{
	return PresentBytes.MemoryOriginalBytes;
}

BYTE* __stdcall Get_Present1_MemoryOriginal_Bytes()
{
	return Present1Bytes.MemoryOriginalBytes;
}

bool __stdcall Get_Present_DetourHookDetected()
{
	return PresentBytes.DetourHookDetected;
}

bool __stdcall Get_Present1_DetourHookDetected()
{
	return Present1Bytes.DetourHookDetected;
}

HRESULT STDMETHODCALLTYPE IDXGIFactory_CreateSwapChain_Override(IDXGIFactory* This,
	_In_  IUnknown* pDevice,
	_In_::DXGI_SWAP_CHAIN_DESC* pDesc,
	IDXGISwapChain** ppSwapChain)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreCreateSwapChain(This, pDevice, *pDesc);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = CreateSwapChain.InvokeHResult<IDXGIFactory_CreateSwapChain_Proc>(This,
			pDevice, pDesc, ppSwapChain);

		Arguments::PostCreateSwapChain(Args, ppSwapChain, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"IDXGIFactory_CreateSwapChain_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return CreateSwapChain.InvokeHResult<IDXGIFactory_CreateSwapChain_Proc>(This, pDevice, pDesc, ppSwapChain);
}

HRESULT STDMETHODCALLTYPE IDXGIFactory2_CreateSwapChainForHwnd_Override(IDXGIFactory2* This,
	_In_  IUnknown* pDevice,
	_In_  HWND hWnd,
	_In_  const ::DXGI_SWAP_CHAIN_DESC1* pDesc,
	_In_opt_  const ::DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc,
	_In_opt_  IDXGIOutput* pRestrictToOutput,
	IDXGISwapChain1** ppSwapChain)
{
	if (Running && HookCallback != nullptr) {

		DXGI_SWAP_CHAIN_DESC1 Desc = *pDesc;
		Arguments::OptionalStruct<DXGI_SWAP_CHAIN_FULLSCREEN_DESC> FullscreenDesc = pFullscreenDesc;

		pDesc = &Desc;

		Arguments Args = Arguments::PreCreateSwapChainForHwnd(This,
			pDevice, hWnd, Desc, FullscreenDesc, pRestrictToOutput);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = CreateSwapChainForHwnd.InvokeHResult<IDXGIFactory2_CreateSwapChainForHwnd_Proc>(This,
			pDevice, hWnd, pDesc, FullscreenDesc.Ptr(), pRestrictToOutput, ppSwapChain);

		Arguments::PostCreateSwapChainForHwnd(Args, ppSwapChain, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"IDXGIFactory2_CreateSwapChainForHwnd_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return CreateSwapChainForHwnd.InvokeHResult<IDXGIFactory2_CreateSwapChainForHwnd_Proc>(This,
		pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
}

std::map<IDXGISwapChain*, UINT> IDXGISwapChain_Present_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain_Present_Override(IDXGISwapChain* This, UINT SyncInterval, UINT Flags)
{
	std::map<IDXGISwapChain*, UINT>::iterator Iterator = IDXGISwapChain_Present_StackCount.try_emplace(This, 0).first;
	UINT& StackCount = Iterator->second;

	HRESULT result = 0, result2 = 0;
	bool StackOverflowFixNeeded = ++StackCount >= 2;

	if (StackOverflowFixNeeded) {
		result2 = Present.RestoreTrueOriginal(PresentBytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain_Present_Override -> Present.RestoreTrueOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}

		result = Present.InvokeHResult<IDXGISwapChain_Present_Proc>(This, SyncInterval, Flags);

		result2 = Present.RestoreMemoryOriginal(PresentBytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain_Present_Override -> Present.RestoreMemoryOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}
	else {
		if (Running && HookCallback != nullptr) {

			Arguments Args{};
			Args = Arguments::PrePresent(This, SyncInterval, Flags);
			HookCallback(&Args);

			if (Args.Stop)
			{
				result = Args.Result;
				goto skip;
			}

			result = Present.InvokeHResult<IDXGISwapChain_Present_Proc>(This, SyncInterval, Flags);

			Arguments::PostPresent(Args, result);
			HookCallback(&Args);
		}
		else result = Present.InvokeHResult<IDXGISwapChain_Present_Proc>(This, SyncInterval, Flags);
	}

	skip:

	--StackCount;
	return result;
}

HRESULT STDMETHODCALLTYPE IDXGISwapChain_ResizeBuffers_Override(IDXGISwapChain* This, UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreResizeBuffers(This,
			BufferCount, Width, Height, NewFormat, SwapChainFlags);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = ResizeBuffers.InvokeHResult<IDXGISwapChain_ResizeBuffers_Proc>(This,
				BufferCount, Width, Height, NewFormat, SwapChainFlags);

		Arguments::PostResizeBuffers(Args, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"IDXGISwapChain_ResizeBuffers_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return ResizeBuffers.InvokeHResult<IDXGISwapChain_ResizeBuffers_Proc>(This,
		BufferCount, Width, Height, NewFormat, SwapChainFlags);
}

HRESULT STDMETHODCALLTYPE IDXGISwapChain_ResizeTarget_Override(IDXGISwapChain* This, _In_ const DXGI_MODE_DESC* pNewTargetParameters)
{
	if (Running && HookCallback != nullptr) {
		DXGI_MODE_DESC NewTargetParameters = *pNewTargetParameters;

		Arguments Args = Arguments::PreResizeTarget(This, NewTargetParameters);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = ResizeTarget.InvokeHResult<IDXGISwapChain_ResizeTarget_Proc>(This,
			&NewTargetParameters);

		Arguments::PostResizeTarget(Args, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"IDXGISwapChain_ResizeTarget_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return ResizeTarget.InvokeHResult<IDXGISwapChain_ResizeTarget_Proc>(This, pNewTargetParameters);
}

std::map<IDXGISwapChain1*, UINT> IDXGISwapChain1_Present1_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters)
{
	std::map<IDXGISwapChain1*, UINT>::iterator Iterator = IDXGISwapChain1_Present1_StackCount.try_emplace(This, 0).first;
	UINT& StackCount = Iterator->second;

	HRESULT result = 0, result2 = 0;
	bool StackOverflowFixNeeded = ++StackCount >= 2;

	if (StackOverflowFixNeeded) {
		result2 = Present1.RestoreTrueOriginal(Present1Bytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain1_Present1_Override -> Present1.RestoreTrueOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}

		result = Present1.InvokeHResult<IDXGISwapChain1_Present1_Proc>(This, SyncInterval, Flags, pPresentParameters);

		result2 = Present1.RestoreMemoryOriginal(Present1Bytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain1_Present1_Override -> Present1.RestoreMemoryOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}
	else {
		if (Running && HookCallback != nullptr) {

			Arguments Args{};
			DXGI_PRESENT_PARAMETERS PresentParameters = *pPresentParameters;
			vector<RECT> DirtyRects;
			RECT ScrollRect{};
			POINT ScrollOffset{};

			if (PresentParameters.pDirtyRects != nullptr) {
				DirtyRects.resize(PresentParameters.DirtyRectsCount);
				CopyMemory(DirtyRects.data(), PresentParameters.pDirtyRects, sizeof(RECT) * PresentParameters.DirtyRectsCount);
				PresentParameters.pDirtyRects = DirtyRects.data();
			}
			if (PresentParameters.pScrollRect != nullptr) {
				ScrollRect = *PresentParameters.pScrollRect;
				PresentParameters.pScrollRect = &ScrollRect;
			}
			if (PresentParameters.pScrollOffset != nullptr) {
				ScrollOffset = *PresentParameters.pScrollOffset;
				PresentParameters.pScrollOffset = &ScrollOffset;
			}

			Args = Arguments::PrePresent1(This, SyncInterval, Flags, PresentParameters);
			HookCallback(&Args);

			if (Args.Stop) {
				result = Args.Result;
				goto skip;
			}

			result = Present1.InvokeHResult<IDXGISwapChain1_Present1_Proc>(This, SyncInterval, Flags, &PresentParameters);

			Arguments::PostPresent1(Args, result);
			HookCallback(&Args);
		}
		else result = Present1.InvokeHResult<IDXGISwapChain1_Present1_Proc>(This, SyncInterval, Flags, pPresentParameters);
	}
	
	skip:

	--StackCount;
	return result;
}

HRESULT STDMETHODCALLTYPE ID3D11Device_CreateShaderResourceView_Override(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_SHADER_RESOURCE_VIEW_DESC* pDesc,
	ID3D11ShaderResourceView** ppSRView)
{
	if (Running && HookCallback != nullptr) {
		Arguments::OptionalStruct<D3D11_SHADER_RESOURCE_VIEW_DESC> Desc = pDesc;

		Arguments Args = Arguments::PreCreateShaderResourceView(This,
			pResource, Desc);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = CreateShaderResourceView.InvokeHResult<ID3D11Device_CreateShaderResourceView_Proc>(This,
			pResource, Desc.Ptr(), ppSRView);

		Arguments::PostCreateShaderResourceView(Args, ppSRView, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"ID3D11Device_CreateShaderResourceView_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return CreateShaderResourceView.InvokeHResult<ID3D11Device_CreateShaderResourceView_Proc>(This,
		pResource, pDesc, ppSRView);
}

HRESULT STDMETHODCALLTYPE ID3D11Device_CreateRenderTargetView_Override(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_RENDER_TARGET_VIEW_DESC* pDesc,
	ID3D11RenderTargetView** ppRTView)
{
	if (Running && HookCallback != nullptr) {
		Arguments::OptionalStruct<D3D11_RENDER_TARGET_VIEW_DESC> Desc = pDesc;

		Arguments Args = Arguments::PreCreateRenderTargetView(This,
			pResource, Desc);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = CreateRenderTargetView.InvokeHResult<ID3D11Device_CreateRenderTargetView_Proc>(This,
			pResource, Desc.Ptr(), ppRTView);

		Arguments::PostCreateRenderTargetView(Args, ppRTView, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"ID3D11Device_CreateRenderTargetView_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return CreateRenderTargetView.InvokeHResult<ID3D11Device_CreateRenderTargetView_Proc>(This,
		pResource, pDesc, ppRTView);
}

HRESULT STDMETHODCALLTYPE ID3D11Device_CreatePixelShader_Override(ID3D11Device* This,
	_In_reads_(BytecodeLength)  const void* pShaderBytecode, _In_  SIZE_T BytecodeLength,
	_In_opt_  ID3D11ClassLinkage* pClassLinkage,
	ID3D11PixelShader** ppPixelShader)
{
	if (Running && HookCallback != nullptr) {
		Arguments Args = Arguments::PreCreatePixelShader(This,
			pShaderBytecode, BytecodeLength, pClassLinkage);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = CreatePixelShader.InvokeHResult<ID3D11Device_CreatePixelShader_Proc>(This,
			pShaderBytecode, BytecodeLength, pClassLinkage, ppPixelShader);

		Arguments::PostCreatePixelShader(Args, ppPixelShader, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"ID3D11Device_CreatePixelShader_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else return CreatePixelShader.InvokeHResult<ID3D11Device_CreatePixelShader_Proc>(This,
		pShaderBytecode, BytecodeLength, pClassLinkage, ppPixelShader);
}

void STDMETHODCALLTYPE ID3D11DeviceContext_PSSetShader_Override(ID3D11DeviceContext* This,
	_In_opt_ ID3D11PixelShader* pPixelShader,
	_In_reads_opt_(NumClassInstances) ID3D11ClassInstance* const* ppClassInstances,
	UINT NumClassInstances)
{
	if (Running && HookCallback != nullptr) {

		ID3D11ClassInstance** ArrayClassInstances = nullptr;
		vector<ID3D11ClassInstance*> ClassInstances;
		if (ppClassInstances != nullptr) {
			ClassInstances.resize(NumClassInstances);
			CopyMemory(ClassInstances.data(), ppClassInstances, sizeof(ID3D11ClassInstance*) * NumClassInstances);
			ArrayClassInstances = ClassInstances.data();
		}

		Arguments Args = Arguments::PrePSSetShader(This,
			pPixelShader, ArrayClassInstances, NumClassInstances);
		HookCallback(&Args);

		if (Args.Stop) return;

		PSSetShader.Invoke<ID3D11DeviceContext_PSSetShader_Proc>(This,
			pPixelShader, ArrayClassInstances, NumClassInstances);

		Arguments::PostPSSetShader(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		PSSetShader.Invoke<ID3D11DeviceContext_PSSetShader_Proc>(This,
			pPixelShader, ppClassInstances, NumClassInstances);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexed_Override(ID3D11DeviceContext* This,
	_In_  UINT IndexCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawIndexed(This,
			IndexCount, StartIndexLocation, BaseVertexLocation);
		HookCallback(&Args);

		if (Args.Stop) return;

		DrawIndexed.Invoke<ID3D11DeviceContext_DrawIndexed_Proc>(This,
			IndexCount, StartIndexLocation, BaseVertexLocation);

		Arguments::PostDrawIndexed(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		DrawIndexed.Invoke<ID3D11DeviceContext_DrawIndexed_Proc>(This,
			IndexCount, StartIndexLocation, BaseVertexLocation);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_Draw_Override(ID3D11DeviceContext* This,
	_In_  UINT VertexCount,
	_In_  UINT StartVertexLocation)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDraw(This,
			VertexCount, StartVertexLocation);
		HookCallback(&Args);

		if (Args.Stop) return;

		Draw.Invoke<ID3D11DeviceContext_Draw_Proc>(This,
			VertexCount, StartVertexLocation);

		Arguments::PostDraw(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		Draw.Invoke<ID3D11DeviceContext_Draw_Proc>(This,
			VertexCount, StartVertexLocation);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_PSSetConstantBuffers_Override(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1)  UINT StartSlot,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - StartSlot)  UINT NumBuffers,
	_In_reads_opt_(NumBuffers)  ID3D11Buffer* const* ppConstantBuffers)
{
	if (Running && HookCallback != nullptr) {
		ID3D11Buffer** ArrayConstantBuffers = nullptr;
		std::vector<ID3D11Buffer*> ConstantBuffers;
		if (ppConstantBuffers != nullptr) {
			ConstantBuffers.resize(NumBuffers);
			CopyMemory(ConstantBuffers.data(), ppConstantBuffers, sizeof(ID3D11Buffer*) * NumBuffers);
			ArrayConstantBuffers = ConstantBuffers.data();
		}

		Arguments Args = Arguments::PrePSSetConstantBuffers(This, StartSlot, NumBuffers, ArrayConstantBuffers);
		HookCallback(&Args);

		PSSetConstantBuffers.Invoke<ID3D11DeviceContext_PSSetConstantBuffers_Proc>(This, StartSlot, NumBuffers, ArrayConstantBuffers);

		Arguments::PostPSSetConstantBuffers(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		PSSetConstantBuffers.Invoke<ID3D11DeviceContext_PSSetConstantBuffers_Proc>(This, StartSlot, NumBuffers, ppConstantBuffers);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexedInstanced_Override(ID3D11DeviceContext* This,
	_In_  UINT IndexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation,
	_In_  UINT StartInstanceLocation)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawIndexedInstanced(This,
			IndexCountPerInstance, InstanceCount, StartIndexLocation, BaseVertexLocation, StartInstanceLocation);
		HookCallback(&Args);

		if (Args.Stop) return;

		DrawIndexedInstanced.Invoke<ID3D11DeviceContext_DrawIndexedInstanced_Proc>(This,
			IndexCountPerInstance, InstanceCount, StartIndexLocation, BaseVertexLocation, StartInstanceLocation);

		Arguments::PostDrawIndexedInstanced(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		DrawIndexedInstanced.Invoke<ID3D11DeviceContext_DrawIndexedInstanced_Proc>(This,
			IndexCountPerInstance, InstanceCount, StartIndexLocation, BaseVertexLocation, StartInstanceLocation);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawInstanced_Override(ID3D11DeviceContext* This,
	_In_  UINT VertexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartVertexLocation,
	_In_  UINT StartInstanceLocation)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawInstanced(This,
			VertexCountPerInstance, InstanceCount, StartVertexLocation, StartInstanceLocation);
		HookCallback(&Args);

		if (Args.Stop) return;

		DrawInstanced.Invoke<ID3D11DeviceContext_DrawInstanced_Proc>(This,
			VertexCountPerInstance, InstanceCount, StartVertexLocation, StartInstanceLocation);

		Arguments::PostDrawInstanced(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		DrawInstanced.Invoke<ID3D11DeviceContext_DrawInstanced_Proc>(This,
			VertexCountPerInstance, InstanceCount, StartVertexLocation, StartInstanceLocation);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_OMSetRenderTargets_Override(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)  UINT NumViews,
	_In_reads_opt_(NumViews)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView)
{
	if (Running && HookCallback != nullptr) {

		ID3D11RenderTargetView** ArrayRTV = nullptr;
		vector<ID3D11RenderTargetView*> RenderTargetViews;
		if (ppRenderTargetViews != nullptr) {
			RenderTargetViews.resize(NumViews);
			CopyMemory(RenderTargetViews.data(), ppRenderTargetViews, sizeof(ID3D11RenderTargetView*) * NumViews);
			ArrayRTV = RenderTargetViews.data();
		}
		
		Arguments Args = Arguments::PreOMSetRenderTargets(This,
			NumViews,
			ArrayRTV, pDepthStencilView);
		HookCallback(&Args);

		if (Args.Stop) return;

		OMSetRenderTargets.Invoke<ID3D11DeviceContext_OMSetRenderTargets_Proc>(This,
			NumViews, ArrayRTV, pDepthStencilView);

		Arguments::PostOMSetRenderTargets(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		OMSetRenderTargets.Invoke<ID3D11DeviceContext_OMSetRenderTargets_Proc>(This,
			NumViews, ppRenderTargetViews, pDepthStencilView);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Override(ID3D11DeviceContext* This,
	_In_  UINT NumRTVs,
	_In_reads_opt_(NumRTVs)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView,
	_In_range_(0, D3D11_1_UAV_SLOT_COUNT - 1)  UINT UAVStartSlot,
	_In_  UINT NumUAVs,
	_In_reads_opt_(NumUAVs)  ID3D11UnorderedAccessView* const* ppUnorderedAccessViews,
	_In_reads_opt_(NumUAVs)  const UINT* pUAVInitialCounts)
{
	if (Running && HookCallback != nullptr) {

		ID3D11RenderTargetView** ArrayRTV = nullptr;
		vector<ID3D11RenderTargetView*> RenderTargetViews;
		if (ppRenderTargetViews != nullptr) {
			RenderTargetViews.resize(NumRTVs);
			CopyMemory(RenderTargetViews.data(), ppRenderTargetViews, sizeof(ID3D11RenderTargetView*) * NumRTVs);
			ArrayRTV = RenderTargetViews.data();
		}
		
		ID3D11UnorderedAccessView** ArrayUAV = nullptr;
		vector<ID3D11UnorderedAccessView*> UnorderedAccessViews;
		if (ppUnorderedAccessViews != nullptr) {
			UnorderedAccessViews.resize(NumUAVs);
			CopyMemory(UnorderedAccessViews.data(), ppUnorderedAccessViews, sizeof(ID3D11RenderTargetView*) * NumUAVs);
			ArrayUAV = UnorderedAccessViews.data();
		}

		UINT* ArrayUAVInitialCounts = nullptr;
		vector<UINT> UAVInitialCounts;
		if (pUAVInitialCounts != nullptr) {
			UAVInitialCounts.resize(NumUAVs);
			CopyMemory(UAVInitialCounts.data(), pUAVInitialCounts, sizeof(UINT) * NumUAVs);
			ArrayUAVInitialCounts = UAVInitialCounts.data();
		}

		Arguments Args = Arguments::PreOMSetRenderTargetsAndUnorderedAccessViews(This,
			NumRTVs,
			ArrayRTV, pDepthStencilView,
			UAVStartSlot, NumUAVs,
			ArrayUAV, ArrayUAVInitialCounts);
		HookCallback(&Args);

		if (Args.Stop) return;

		OMSetRenderTargetsAndUnorderedAccessViews.Invoke<ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc>(This,
			NumRTVs, ArrayRTV, pDepthStencilView,
			UAVStartSlot, NumUAVs, ArrayUAV, ArrayUAVInitialCounts);

		Arguments::PostOMSetRenderTargetsAndUnorderedAccessViews(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		OMSetRenderTargetsAndUnorderedAccessViews.Invoke<ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc>(This,
			NumRTVs, ppRenderTargetViews, pDepthStencilView,
			UAVStartSlot, NumUAVs, ppUnorderedAccessViews, pUAVInitialCounts);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawAuto_Override(ID3D11DeviceContext* This)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawAuto(This);
		HookCallback(&Args);

		if (Args.Stop) return;

		reinterpret_cast<ID3D11DeviceContext_DrawAuto_Proc>(DrawAuto.MemoryOriginalProc)(This);

		Arguments::PostDrawAuto(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		reinterpret_cast<ID3D11DeviceContext_DrawAuto_Proc>(DrawAuto.MemoryOriginalProc)(This);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawIndexedInstancedIndirect_Override(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawIndexedInstancedIndirect(This,
			pBufferForArgs, AlignedByteOffsetForArgs);
		HookCallback(&Args);

		if (Args.Stop) return;

		DrawIndexedInstancedIndirect.Invoke<ID3D11DeviceContext_DrawIndexedInstancedIndirect_Proc>(This,
			pBufferForArgs, AlignedByteOffsetForArgs);

		Arguments::PostDrawIndexedInstancedIndirect(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		DrawIndexedInstancedIndirect.Invoke<ID3D11DeviceContext_DrawIndexedInstancedIndirect_Proc>(This,
			pBufferForArgs, AlignedByteOffsetForArgs);
	}
}

void STDMETHODCALLTYPE ID3D11DeviceContext_DrawInstancedIndirect_Override(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreDrawInstancedIndirect(This,
			pBufferForArgs, AlignedByteOffsetForArgs);
		HookCallback(&Args);

		if (Args.Stop) return;

		DrawInstancedIndirect.Invoke<ID3D11DeviceContext_DrawInstancedIndirect_Proc>(This,
			pBufferForArgs, AlignedByteOffsetForArgs);

		Arguments::PostDrawInstancedIndirect(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		DrawInstancedIndirect.Invoke<ID3D11DeviceContext_DrawInstancedIndirect_Proc>(This,
			pBufferForArgs, AlignedByteOffsetForArgs);
	}
}

HRESULT STDMETHODCALLTYPE IDXGISwapChain3_SetColorSpace1_Override(IDXGISwapChain3* This,
	_In_  DXGI_COLOR_SPACE_TYPE ColorSpace)
{
	if (Running && HookCallback != nullptr) {

		Arguments Args = Arguments::PreSetColorSpace1(This,
			ColorSpace);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = SetColorSpace1.InvokeHResult<IDXGISwapChain3_SetColorSpace1_Proc>(This,
			ColorSpace);

		Arguments::PostSetColorSpace1(Args, result);
		HookCallback(&Args);

		if (result != S_OK) {
			LogCallback((L"IDXGISwapChain3_SetColorSpace1_Override -> return value: " + to_wstring(result)).c_str());
		}
		return result;
	}
	else {
		return SetColorSpace1.InvokeHResult<IDXGISwapChain3_SetColorSpace1_Proc>(This,
			ColorSpace);
	}
}

HRESULT STDMETHODCALLTYPE IDXGISwapChain4_SetHDRMetaData_Override(IDXGISwapChain4* This,
	_In_  DXGI_HDR_METADATA_TYPE Type,
	_In_  UINT Size,
	_In_reads_opt_(Size)  void* pMetaData)
{
	if (Running && HookCallback != nullptr) {

		Arguments::OptionalStruct<DXGI_HDR_METADATA_HDR10> MetaData10 = reinterpret_cast<DXGI_HDR_METADATA_HDR10*>(pMetaData);
		Arguments::OptionalStruct<DXGI_HDR_METADATA_HDR10PLUS> MetaData10Plus = reinterpret_cast<DXGI_HDR_METADATA_HDR10PLUS*>(pMetaData);

		Arguments Args = Arguments::PreSetHDRMetaData(This,
			Type, Size, MetaData10, MetaData10Plus);
		HookCallback(&Args);

		if (Args.Stop) return Args.Result;

		HRESULT result = SetHDRMetaData.InvokeHResult<IDXGISwapChain4_SetHDRMetaData_Proc>(This,
			Type, Size,
			Type == DXGI_HDR_METADATA_TYPE_HDR10 ? reinterpret_cast<void*>(MetaData10.Ptr()) :
			Type == DXGI_HDR_METADATA_TYPE_HDR10PLUS ? reinterpret_cast<void*>(MetaData10Plus.Ptr()) : nullptr);

		Arguments::PostSetHDRMetaData(Args, result);
		HookCallback(&Args);

		LogCallback((L"IDXGISwapChain4_SetHDRMetaData_Override -> return value: " + to_wstring(result)).c_str());
		return result;
	}
	else {
		return SetHDRMetaData.InvokeHResult<IDXGISwapChain4_SetHDRMetaData_Proc>(This,
			Type, Size, pMetaData);
	}
}



intptr_t __stdcall Get_D3D11_DLL_BaseAddress()
{
	return D3D11_DLL.BaseAddress;
}

intptr_t __stdcall Get_DXGI_DLL_BaseAddress()
{
	return DXGI_DLL.BaseAddress;
}

intptr_t __stdcall Get_GameOverlayRenderer64_DLL_BaseAddress()
{
	return GameOverlayRenderer64_DLL.BaseAddress;
}

DWORD __stdcall Get_D3D11_DLL_ImageSize()
{
	return D3D11_DLL.ImageSize;
}

DWORD __stdcall Get_DXGI_DLL_ImageSize()
{
	return DXGI_DLL.ImageSize;
}

DWORD __stdcall Get_GameOverlayRenderer64_DLL_ImageSize()
{
	return GameOverlayRenderer64_DLL.ImageSize;
}

bool __stdcall JmpEndsUpInRange(intptr_t SrcAddr, intptr_t RangeStart, DWORD Size)
{
#if defined(_M_ARM)
#error this function will not work on ARM because it assumes x86 instruction machine codes
#endif

	BYTE* nextAddress = reinterpret_cast<BYTE*>(SrcAddr);

	// generated by ChatGPT because my old code (which is based on guess work) eventually failed
	// Follow jumps until a non-jump instruction is found
	while (true) {
		BYTE opcode = *nextAddress;

		if (opcode == 0xEB) { // Short jump (relative)
			int8_t offset = *(int8_t*)(nextAddress + 1);
			nextAddress = nextAddress + 2 + offset;
		}
		else if (opcode == 0xE9) { // Near jump (relative)
			int32_t offset = *(int32_t*)(nextAddress + 1);
			nextAddress = nextAddress + 5 + offset;
		}
		else if (opcode == 0xE8) { // Call (relative)
			int32_t offset = *(int32_t*)(nextAddress + 1);
			nextAddress = nextAddress + 5 + offset;
		}
		else if (opcode == 0xFF) { // Indirect jump (complex, multiple forms)
			BYTE modrm = *(nextAddress + 1);
			if ((modrm & 0xC0) == 0x00) { // [reg] or [disp32]
				if ((modrm & 0x07) == 0x05) { // [disp32]
					int32_t disp32 = *(int32_t*)(nextAddress + 2);
					nextAddress = *(BYTE**)(nextAddress + 6 + disp32);
				}
				else {
					BYTE reg = modrm & 0x07;
					nextAddress = *(BYTE**)(nextAddress + 2); // [reg]
				}
			}
			else if ((modrm & 0xC0) == 0x40) { // [reg + disp8]
				BYTE reg = modrm & 0x07;
				int8_t disp8 = *(int8_t*)(nextAddress + 2);
				nextAddress = *(BYTE**)(nextAddress + 3 + disp8);
			}
			else if ((modrm & 0xC0) == 0x80) { // [reg + disp32]
				BYTE reg = modrm & 0x07;
				int32_t disp32 = *(int32_t*)(nextAddress + 2);
				nextAddress = *(BYTE**)(nextAddress + 6 + disp32);
			}
			else {
				// Unsupported addressing mode
				break;
			}
		}
		else {
			break;
		}
	}

	if (reinterpret_cast<intptr_t>(nextAddress) >= RangeStart && reinterpret_cast<intptr_t>(nextAddress) < (RangeStart + Size)) {
		return true;
	}
	return false;
}

char CharArray[1024] = "\0";
constexpr GUID WKPDID_D3DDebugObjectName{ 0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00 };
HRESULT __stdcall GetName(_In_ IUnknown* D3D11_Interface, _Out_ LPCSTR* ppCharArray)
{
	ComPtr<ID3D11Device> Device;
	ComPtr<ID3D11DeviceChild> Child;
	HRESULT result = 0;
	UINT Length = 1024;
	if (D3D11_Interface == nullptr) {
		return E_POINTER;
	}
	else if (D3D11_Interface->QueryInterface(IID_PPV_ARGS(&Device)) == S_OK) {
		ZeroMemory(CharArray, 1024);
		*ppCharArray = CharArray;
		return Device->GetPrivateData(WKPDID_D3DDebugObjectName, &Length, CharArray);
	}
	else if (D3D11_Interface->QueryInterface(IID_PPV_ARGS(&Child)) == S_OK) {
		ZeroMemory(CharArray, 1024);
		*ppCharArray = CharArray;
		return Child->GetPrivateData(WKPDID_D3DDebugObjectName, &Length, CharArray);
	}
	else return E_NOINTERFACE;
}

static void LogErrorMessageFromBlob(wstring Prefix, ID3DBlob* Blob)
{
	SIZE_T CharCount = Blob->GetBufferSize();
	LPCSTR Message = reinterpret_cast<LPCSTR>(Blob->GetBufferPointer());
	LogCallback(Convert(string(Message, CharCount)).c_str());
}

HRESULT __stdcall CompileVertexShader(_In_ ID3D11Device* Device, _In_ LPCWSTR FileName, _In_ LPCSTR EntryPoint,
	_In_opt_ LPCSTR DebugName, _Out_ ID3D11VertexShader** VertexShader)
{
	ComPtr<ID3DBlob> Code, Error;
	HRESULT result = D3DCompileFromFile(FileName, nullptr, nullptr, EntryPoint, "vs_5_0", 0, 0, &Code, &Error);
	if (FAILED(result) && Error != nullptr) {
		LogErrorMessageFromBlob(L"compile vertex shader " + wstring(FileName) + L" failed: ", Error.Get());
		return result;
	}
	result = Device->CreateVertexShader(Code->GetBufferPointer(), Code->GetBufferSize(), nullptr, VertexShader);
	if (FAILED(result)) return result;
	if (DebugName != nullptr) {
		result = (*VertexShader)->SetPrivateData(WKPDID_D3DDebugObjectName, static_cast<UINT>(strlen(DebugName)), DebugName);
		if (FAILED(result)) return result;
	}
	return result;
}

HRESULT __stdcall CompilePixelShader(_In_ ID3D11Device* Device, _In_ LPCWSTR FileName, _In_ LPCSTR EntryPoint,
	_In_opt_ LPCSTR DebugName, _Out_ ID3D11PixelShader** PixelShader)
{
	ComPtr<ID3DBlob> Code, Error;
	HRESULT result = D3DCompileFromFile(FileName, nullptr, nullptr, EntryPoint, "ps_5_0", 0, 0, &Code, &Error);
	if (FAILED(result) && Error != nullptr) {
		LogErrorMessageFromBlob(L"compile pixel shader " + wstring(FileName) + L" failed: ", Error.Get());
		return result;
	}
	result = Device->CreatePixelShader(Code->GetBufferPointer(), Code->GetBufferSize(), nullptr, PixelShader);
	if (FAILED(result)) return result;
	if (DebugName != nullptr) {
		result = (*PixelShader)->SetPrivateData(WKPDID_D3DDebugObjectName, static_cast<UINT>(strlen(DebugName)), DebugName);
		if (FAILED(result)) return result;
	}
	return result;
}

/*
HRESULT __stdcall EnsureConstantBuffer(_In_ ID3D11Device* Device, _In_ ID3D11DeviceContext* DeviceContext,
	_Inout_ ID3D11Buffer** ConstantBuffer, _In_reads_(4) float* Values)
{
	HRESULT result = 0;

	if (*ConstantBuffer == nullptr) {
		D3D11_BUFFER_DESC Desc = {
			.ByteWidth = 4 * sizeof(float),
			.Usage = D3D11_USAGE_DYNAMIC,
			.BindFlags = D3D11_BIND_CONSTANT_BUFFER,
			.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE,
			.MiscFlags = 0,
			.StructureByteStride = sizeof(float),
		};

		result = Device->CreateBuffer(&Desc, nullptr, ConstantBuffer);
		if (FAILED(result)) return result;
	}

	D3D11_MAPPED_SUBRESOURCE Mapped{};
	result = DeviceContext->Map(*ConstantBuffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &Mapped);
	if (FAILED(result)) return result;
	
	CopyMemory(Mapped.pData, Values, 4 * sizeof(float));

	DeviceContext->Unmap(*ConstantBuffer, 0);

	return result;
}
*/


constexpr WCHAR ClassName[] = L"MAGIC";
constexpr WCHAR WindowName[] = L"Magic";
HINSTANCE Module = NULL;
HWND hWndForTest = NULL;
HANDLE WindowThread = NULL;
HANDLE WindowCreated = NULL;

LRESULT static DummyWndProc(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam)
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

DWORD static DummyCreateHwnd(LPVOID Arg)
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
	if (Class == NULL) return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());

	hWndForTest = CreateWindowW(ClassName, WindowName, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0,
		NULL, NULL, Module, nullptr);
	if (hWndForTest == NULL) return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());

	SetEvent(WindowCreated);

	MSG msg;
	while (GetMessageW(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessageW(&msg);
	}

	if (!UnregisterClassW(ClassName, Module)) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}
	return S_OK;
}

HRESULT DummyCreateDirectXInstances(
	ID3D11Device** Device, ID3D11DeviceContext** Context,
	IDXGIFactory** Factory, IDXGIFactory2** Factory2,
	IDXGISwapChain** SwapChain, IDXGISwapChain1** SwapChain1, IDXGISwapChain3** SwapChain3, IDXGISwapChain4** SwapChain4)
{
	HRESULT result = 0;

	if (*Factory == nullptr) {
		result = CreateDXGIFactory2(0, IID_PPV_ARGS(Factory2));
		if (FAILED(result))
		{
			result = CreateDXGIFactory(IID_PPV_ARGS(Factory));
			if (FAILED(result)) return result;
		}
		else {
			result = (*Factory2)->QueryInterface(Factory);
			if (FAILED(result)) return result;
		}
	}

	D3D_FEATURE_LEVEL FeatureLevels[]{
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
	};
	result = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, NULL, 0, FeatureLevels, ARRAYSIZE(FeatureLevels), D3D11_SDK_VERSION,
		Device, nullptr, Context);
	if (FAILED(result)) return result;

	WindowCreated = CreateEventW(nullptr, TRUE, FALSE, nullptr);
	if (WindowCreated == NULL) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}
	WindowThread = CreateThread(nullptr, 0, DummyCreateHwnd, nullptr, 0, nullptr);
	if (WindowThread == NULL) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}

	HANDLE Wait[2]{ WindowCreated, WindowThread };
	DWORD WaitResult = WaitForMultipleObjects(2, Wait, FALSE, INFINITE);
	if (WaitResult == (WAIT_OBJECT_0 + 1)) {
		if (!GetExitCodeThread(WindowThread, &WaitResult)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		else {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, WaitResult);
		}
	}
	else if (WaitResult != WAIT_OBJECT_0) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}

	// create swap chain
	if (*Factory2 != nullptr) {
		::DXGI_SWAP_CHAIN_DESC1 SwapChainDesc = {
			.Width = 640,
			.Height = 360,
			.Format = ::DXGI_FORMAT_R8G8B8A8_UNORM,
			.SampleDesc = {
				.Count = 1,
				.Quality = 0,
			},
			.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
			.BufferCount = 2,
			.SwapEffect = DXGI_SWAP_EFFECT_DISCARD,
			.Flags = 0,
		};

		result = (*Factory2)->CreateSwapChainForHwnd(*Device, hWndForTest, &SwapChainDesc, nullptr, nullptr, SwapChain1);
		if (FAILED(result)) return result;

		result = (*SwapChain1)->QueryInterface(SwapChain);
		if (FAILED(result)) return result;
	}
	else {
		::DXGI_SWAP_CHAIN_DESC SwapChainDesc = {
			.BufferDesc = {
				.Width = 640,
				.Height = 320,
				.Format = ::DXGI_FORMAT_R8G8B8A8_UNORM,
			},
			.SampleDesc = {
				.Count = 1,
				.Quality = 0,
			},
			.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
			.BufferCount = 2,
			.OutputWindow = hWndForTest,
			.Windowed = TRUE,
			.SwapEffect = DXGI_SWAP_EFFECT_DISCARD,
			.Flags = 0,
		};
		result = (*Factory)->CreateSwapChain(*Device, &SwapChainDesc, SwapChain);
		if (FAILED(result)) return result;
	}

	(*SwapChain)->QueryInterface(IID_PPV_ARGS(SwapChain1));
	(*SwapChain)->QueryInterface(IID_PPV_ARGS(SwapChain3));
	(*SwapChain)->QueryInterface(IID_PPV_ARGS(SwapChain4));

	return result;
}

HRESULT DummyRelease()
{
	if (!PostMessageW(hWndForTest, WM_CLOSE, 0, 0)) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}
	if (WaitForSingleObject(WindowThread, INFINITE) != WAIT_OBJECT_0) {
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
	}

	CloseHandle(WindowCreated);
	CloseHandle(WindowThread);

	return S_OK;
}



void ForceBreakpoint()
{
	int* n = nullptr;
#pragma warning (push)
#pragma warning (disable : 6011)
	*n = 1;
#pragma warning (pop)
}
