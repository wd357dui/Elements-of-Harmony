#include "pch.h"

using namespace std;

void ForceBreakpoint();

struct Arguments;
typedef void(__stdcall* CallbackProc)(Arguments* Args);
typedef void(__stdcall* LogCallbackProc)(LPCWSTR Message);
CallbackProc HookCallback = nullptr;
LogCallbackProc LogCallback = nullptr;

atomic_bool Running = false;

extern "C" {
	__declspec(dllexport) HRESULT __stdcall InstallHook();
	__declspec(dllexport) HRESULT __stdcall UninstallHook();
	__declspec(dllexport) void __stdcall SetRunning(bool Running);

	__declspec(dllexport) void __stdcall SetCallbacks(CallbackProc HookCallback, LogCallbackProc LogCallback);

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
}

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
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain_Present_Proc)(IDXGISwapChain* This, UINT SyncInterval, UINT Flags);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain1_Present1_Proc)(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags,
	_In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_OMSetRenderTargets_Proc)(
	_In_range_(0, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)  UINT NumViews,
	_In_reads_opt_(NumViews)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc)(
	_In_  UINT NumRTVs,
	_In_reads_opt_(NumRTVs)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView,
	_In_range_(0, D3D11_1_UAV_SLOT_COUNT - 1)  UINT UAVStartSlot,
	_In_  UINT NumUAVs,
	_In_reads_opt_(NumUAVs)  ID3D11UnorderedAccessView* const* ppUnorderedAccessViews,
	_In_reads_opt_(NumUAVs)  const UINT* pUAVInitialCounts);

intptr_t FactoryVTableAddress = 0;
intptr_t Factory2VTableAddress = 0;
intptr_t SwapChainVTableAddress = 0;
intptr_t SwapChain1VTableAddress = 0;
intptr_t DeviceVTableAddress = 0;
intptr_t DeviceContextVTableAddress = 0;
constexpr size_t IDXGIFactory_CreateSwapChain_VTableIndex = 10;
constexpr size_t IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex = 15;
constexpr size_t IDXGISwapChain_Present_VTableIndex = 8;
constexpr size_t IDXGISwapChain1_Present1_VTableIndex = 22;
constexpr size_t ID3D11DeviceContext_OMSetRenderTargets_VTableIndex = 33;
constexpr size_t ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex = 34;

constexpr size_t InstructionCompareByteCount = 0x20;

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
	// this can be already replaced by other hooks,
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
		return HasMemoryOriginalBytes && HasTrueOriginalBytes && !RtlEqualMemory(MemoryOriginalBytes, TrueOriginalBytes, 5 * sizeof(BYTE));
	}
	__declspec(property(get = DetectDetourHook)) bool DetourHookDetected;
} PresentBytes, Present1Bytes;

struct HookPatch
{
	using Delegate = intptr_t;

	Delegate* Pointer = nullptr;
	Delegate MemoryOriginalProc = 0;
	Delegate Override = 0;

	void Init(Delegate* Pointer, Delegate Override)
	{
		this->Pointer = Pointer;
		MemoryOriginalProc = *Pointer;
		this->Override = Override;
	}
	inline bool getInitialized() const
	{
		return Pointer != nullptr;
	}
	__declspec(property(get = getInitialized)) bool Initialized;
	HRESULT Patch()
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (!VirtualProtect(Pointer, sizeof(Delegate), PAGE_EXECUTE_READWRITE, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		*Pointer = Override;
		if (!VirtualProtect(Pointer, sizeof(Delegate), OldProtect, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		if (FAILED(result)) return result;
		return result;
	}
	HRESULT UnPatch()
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (!VirtualProtect(Pointer, sizeof(Delegate), PAGE_EXECUTE_READWRITE, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		if (FAILED(result)) return result;
		*Pointer = MemoryOriginalProc;
		if (!VirtualProtect(Pointer, sizeof(Delegate), OldProtect, &OldProtect)) {
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
		}
		return result;
	}
	HRESULT RestoreMemoryOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasMemoryOriginalBytes) {
			if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), 5, PAGE_EXECUTE_READWRITE, &OldProtect)) {
				return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
			}
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.MemoryOriginalBytes, 5);
			if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), 5, OldProtect, &OldProtect)) {
				return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
			}
		}
		else return E_PENDING;
		return result;
	}
	HRESULT RestoreTrueOriginal(FirstFiveBytes& Bytes) const
	{
		HRESULT result = 0;
		DWORD OldProtect = 0;
		if (Bytes.HasTrueOriginalBytes) {
			if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), 5, PAGE_EXECUTE_READWRITE, &OldProtect)) {
				return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
			}
			CopyMemory(reinterpret_cast<void*>(MemoryOriginalProc), Bytes.TrueOriginalBytes, 5);
			if (!VirtualProtect(reinterpret_cast<void*>(MemoryOriginalProc), 5, OldProtect, &OldProtect)) {
				return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, GetLastError());
			}
		}
		else return E_PENDING;
		return result;
	}
} CreateSwapChain, CreateSwapChainForHwnd, Present, Present1, OMSetRenderTargets, OMSetRenderTargetsAndUnorderedAccessViews;

struct Arguments
{
	static_assert(sizeof(void*) == sizeof(int64_t), "target platform (pointer size) must be the same (64-bit) between here and DirectXHook.cs");

	GUID IID; // uuid of the interface
	void* PPV; // pointer of the interface
	size_t VTableIndex; // vtable index of the function
	BOOL Post = FALSE; // determines whether if this is called after the original function is called
	HRESULT Result = S_OK; // if Post is TRUE, the HRESULT return value of the function
	void* Args[11]{ nullptr };	// function arguments, array size is in favor of 128 byte struct alignment
								// alignment is not necessary (at least not for now), it's just satisfying.

	template <typename StructType>
	struct OptionalStruct
	{
		BOOL Exist = FALSE;
		StructType Struct{};
		OptionalStruct(const StructType* Ptr) {
			if (Ptr == nullptr) Exist = FALSE;
			else {
				Struct = *Ptr;
				Exist = TRUE;
			}
		}
		StructType* Ptr() {
			if (Exist) return &Struct;
			else return nullptr;
		}
	};

	inline static Arguments PreCreateSwapChain(IDXGIFactory* This,
		IUnknown*& pDevice, DXGI_SWAP_CHAIN_DESC& pDesc)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGIFactory);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain_Present_VTableIndex;
		Result.Args[0] = &pDevice;
		Result.Args[1] = &pDesc;
		return Result;
	}
	inline static void PostCreateSwapChain(Arguments& Previous, IDXGISwapChain** ppSwapChain, HRESULT result)
	{
		Previous.Args[2] = ppSwapChain;
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreCreateSwapChainForHwnd(IDXGIFactory2* This,
		IUnknown*& pDevice, HWND& hWnd, DXGI_SWAP_CHAIN_DESC1& Desc, OptionalStruct<DXGI_SWAP_CHAIN_FULLSCREEN_DESC>& FullscreenDesc,
		IDXGIOutput*& pRestrictToOutput)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGIFactory);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain_Present_VTableIndex;
		Result.Args[0] = &pDevice;
		Result.Args[1] = &hWnd;
		Result.Args[2] = &Desc;
		Result.Args[3] = &FullscreenDesc;
		Result.Args[4] = &pRestrictToOutput;
		return Result;
	}
	inline static void PostCreateSwapChainForHwnd(Arguments& Previous, IDXGISwapChain1** ppSwapChain, HRESULT result)
	{
		Previous.Args[5] = ppSwapChain;
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PrePresent(IDXGISwapChain* This, UINT& SyncInterval, UINT& Flags)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain_Present_VTableIndex;
		Result.Args[0] = &SyncInterval;
		Result.Args[1] = &Flags;
		return Result;
	}
	inline static void PostPresent(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PrePresent1(IDXGISwapChain1* This, UINT& SyncInterval, UINT& Flags, DXGI_PRESENT_PARAMETERS& PresentParameters)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain1);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain1_Present1_VTableIndex;
		Result.Args[0] = &SyncInterval;
		Result.Args[1] = &Flags;
		Result.Args[2] = &PresentParameters;
		return Result;
	}
	inline static void PostPresent1(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreOMSetRenderTargets(ID3D11DeviceContext* This,
		UINT& NumViews, ID3D11RenderTargetView**& ppRenderTargetViews, ID3D11DepthStencilView*& pDepthStencilView)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_OMSetRenderTargets_VTableIndex;
		Result.Args[0] = &NumViews;
		Result.Args[1] = &ppRenderTargetViews;
		Result.Args[2] = &pDepthStencilView;
		return Result;
	}
	inline static void PostOMSetRenderTargets(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreOMSetRenderTargetsAndUnorderedAccessViews(ID3D11DeviceContext* This,
		UINT& NumRTVs, ID3D11RenderTargetView**& ppRenderTargetViews, ID3D11DepthStencilView*& pDepthStencilView,
		UINT& UAVStartSlot,
		UINT& NumUAVs, ID3D11UnorderedAccessView**& ppUnorderedAccessViews,
		UINT*& pUAVInitialCounts)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex;
		Result.Args[0] = &NumRTVs;
		Result.Args[1] = &ppRenderTargetViews;
		Result.Args[2] = &pDepthStencilView;
		Result.Args[3] = &UAVStartSlot;
		Result.Args[4] = &NumUAVs;
		Result.Args[5] = &ppUnorderedAccessViews;
		Result.Args[6] = &pUAVInitialCounts;
		return Result;
	}
	inline static void PostOMSetRenderTargetsAndUnorderedAccessViews(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}
};

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
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
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

HRESULT DummyCreateDirectXInstances(
	ID3D11Device** Device, ID3D11DeviceContext** Context,
	IDXGIFactory** Factory, IDXGIFactory2** Factory2,
	IDXGISwapChain** SwapChain, IDXGISwapChain1** SwapChain1);
HRESULT DummyRelease();

HRESULT __stdcall InstallHook()
{
	HRESULT result = 0;

	result = DXGI_DLL.Load(L"DXGI.dll");
	if (FAILED(result)) return result;

	result = D3D11_DLL.Load(L"D3D11.dll");
	if (FAILED(result)) return result;

	result = GameOverlayRenderer64_DLL.Load(L"GameOverlayRenderer64.dll");
	if (FAILED(result)) return result;

	ComPtr<ID3D11Device> Device;
	ComPtr<ID3D11DeviceContext> Context;

	ComPtr<IDXGIFactory> Factory;
	ComPtr<IDXGIFactory2> Factory2;

	ComPtr<IDXGISwapChain> SwapChain;
	ComPtr<IDXGISwapChain1> SwapChain1;

	result = DummyCreateDirectXInstances(&Device, &Context, &Factory, &Factory2, &SwapChain, &SwapChain1);
	if (FAILED(result)) return result;

	// saving original function pointers
	intptr_t* VTable = nullptr;

	VTable = reinterpret_cast<intptr_t*>(SwapChainVTableAddress = *reinterpret_cast<intptr_t*>(SwapChain.Get()));
	Present.Init(&VTable[IDXGISwapChain_Present_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain_Present_Override));
	result = PresentBytes.Init(VTable[IDXGISwapChain_Present_VTableIndex], L"C:/Windows/System32/DXGI.dll");
	if (FAILED(result)) return result;

	VTable = reinterpret_cast<intptr_t*>(SwapChain1VTableAddress = *reinterpret_cast<intptr_t*>(SwapChain1.Get()));
	Present1.Init(&VTable[IDXGISwapChain1_Present1_VTableIndex], reinterpret_cast<intptr_t>(IDXGISwapChain1_Present1_Override));
	result = Present1Bytes.Init(VTable[IDXGISwapChain1_Present1_VTableIndex], L"C:/Windows/System32/DXGI.dll");
	if (FAILED(result)) return result;

	if (Factory2 != nullptr) {
		VTable = reinterpret_cast<intptr_t*>(Factory2VTableAddress = *reinterpret_cast<intptr_t*>(Factory2.Get()));
		CreateSwapChainForHwnd.Init(&VTable[IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex], reinterpret_cast<intptr_t>(IDXGIFactory2_CreateSwapChainForHwnd_Override));
	}

	VTable = reinterpret_cast<intptr_t*>(FactoryVTableAddress = *reinterpret_cast<intptr_t*>(Factory.Get()));
	CreateSwapChain.Init(&VTable[IDXGIFactory_CreateSwapChain_VTableIndex], reinterpret_cast<intptr_t>(IDXGIFactory_CreateSwapChain_Override));

	VTable = reinterpret_cast<intptr_t*>(DeviceContextVTableAddress = *reinterpret_cast<intptr_t*>(Context.Get()));
	OMSetRenderTargets.Init(&VTable[ID3D11DeviceContext_OMSetRenderTargets_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_OMSetRenderTargets_Override));
	OMSetRenderTargetsAndUnorderedAccessViews.Init(&VTable[ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex], reinterpret_cast<intptr_t>(ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Override));

	// changing function pointers
	result = Present.Patch();
	if (FAILED(result)) return result;
	
	result = Present1.Patch();
	if (FAILED(result)) return result;

	if (CreateSwapChainForHwnd.Initialized) {
		result = CreateSwapChainForHwnd.Patch();
		if (FAILED(result)) return result;
	}

	result = CreateSwapChain.Patch();
	if (FAILED(result)) return result;
	
	result = OMSetRenderTargets.Patch();
	if (FAILED(result)) return result;
	
	result = OMSetRenderTargetsAndUnorderedAccessViews.Patch();
	if (FAILED(result)) return result;

	SwapChain.Reset();
	Context.Reset();
	Device.Reset();
	Factory.Reset();

	return result;
}

HRESULT __stdcall UninstallHook()
{
	HRESULT result = 0;

	result = Present.UnPatch();
	if (FAILED(result)) return result;

	result = Present1.UnPatch();
	if (FAILED(result)) return result;

	if (CreateSwapChainForHwnd.Initialized) {
		result = CreateSwapChainForHwnd.UnPatch();
		if (FAILED(result)) return result;
	}

	result = CreateSwapChain.UnPatch();
	if (FAILED(result)) return result;

	result = OMSetRenderTargets.UnPatch();
	if (FAILED(result)) return result;

	result = OMSetRenderTargetsAndUnorderedAccessViews.UnPatch();
	if (FAILED(result)) return result;

	return S_OK;
}

void __stdcall SetRunning(bool Running)
{
	::Running = Running;
}

void __stdcall SetCallbacks(CallbackProc HookCallback, LogCallbackProc LogCallback)
{
	::HookCallback = HookCallback;
	::LogCallback = LogCallback;
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

		HRESULT result = reinterpret_cast<IDXGIFactory_CreateSwapChain_Proc>(CreateSwapChain.MemoryOriginalProc)(This, pDevice, pDesc, ppSwapChain);

		Arguments::PostCreateSwapChain(Args, ppSwapChain, result);
		HookCallback(&Args);

		return result;
	}
	else return reinterpret_cast<IDXGIFactory_CreateSwapChain_Proc>(CreateSwapChain.MemoryOriginalProc)(This, pDevice, pDesc, ppSwapChain);
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

		HRESULT result = reinterpret_cast<IDXGIFactory2_CreateSwapChainForHwnd_Proc>(CreateSwapChainForHwnd.MemoryOriginalProc)(This,
			pDevice, hWnd, pDesc, FullscreenDesc.Ptr(), pRestrictToOutput, ppSwapChain);

		Arguments::PostCreateSwapChainForHwnd(Args, ppSwapChain, result);
		HookCallback(&Args);

		return result;
	}
	else return reinterpret_cast<IDXGIFactory2_CreateSwapChainForHwnd_Proc>(CreateSwapChainForHwnd.MemoryOriginalProc)(This,
		pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
}

std::map<IDXGISwapChain*, int> IDXGISwapChain_Present_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain_Present_Override(IDXGISwapChain* This, UINT SyncInterval, UINT Flags)
{
	if (IDXGISwapChain_Present_StackCount.find(This) == IDXGISwapChain_Present_StackCount.end()) {
		IDXGISwapChain_Present_StackCount[This] = 0;
	}

	HRESULT result = 0, result2 = 0;
	bool StackOverflowFixNeeded = ++IDXGISwapChain_Present_StackCount[This] >= 2;

	if (StackOverflowFixNeeded) {
		result2 = Present.RestoreTrueOriginal(PresentBytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain_Present_Override -> Present.RestoreTrueOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}

	Arguments Args{};
	if (Running && HookCallback != nullptr) {
		Args = Arguments::PrePresent(This, SyncInterval, Flags);
		HookCallback(&Args);
	}

	result = reinterpret_cast<IDXGISwapChain_Present_Proc>(Present.MemoryOriginalProc)(This, SyncInterval, Flags);

	if (Running && HookCallback != nullptr) {
		Arguments::PostPresent(Args, result);
		HookCallback(&Args);
	}

	if (StackOverflowFixNeeded) {
		result2 = Present.RestoreMemoryOriginal(PresentBytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain_Present_Override -> Present.RestoreMemoryOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}

	--IDXGISwapChain_Present_StackCount[This];
	return result;
}

std::map<IDXGISwapChain1*, int> IDXGISwapChain1_Present1_StackCount;
HRESULT STDMETHODCALLTYPE IDXGISwapChain1_Present1_Override(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters)
{
	if (IDXGISwapChain1_Present1_StackCount.find(This) == IDXGISwapChain1_Present1_StackCount.end()) {
		IDXGISwapChain1_Present1_StackCount[This] = 0;
	}

	HRESULT result = 0, result2 = 0;
	bool StackOverflowFixNeeded = ++IDXGISwapChain1_Present1_StackCount[This] >= 2;

	if (StackOverflowFixNeeded) {
		result2 = Present1.RestoreTrueOriginal(Present1Bytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain1_Present1_Override -> Present1.RestoreTrueOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}

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
	if (Running && HookCallback != nullptr) {
		HookCallback(&Args);
	}

	result = reinterpret_cast<IDXGISwapChain1_Present1_Proc>(Present1.MemoryOriginalProc)(This, SyncInterval, Flags, &PresentParameters);

	if (Running && HookCallback != nullptr) {
		Arguments::PostPresent1(Args, result);
		HookCallback(&Args);
	}

	if (StackOverflowFixNeeded) {
		result2 = Present1.RestoreMemoryOriginal(Present1Bytes);
		if (FAILED(result2)) {
			LogCallback((L"IDXGISwapChain1_Present1_Override -> Present1.RestoreMemoryOriginal -> error code: " + to_wstring(result2)).c_str());
			ForceBreakpoint();
		}
	}

	--IDXGISwapChain1_Present1_StackCount[This];
	return result;
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

		reinterpret_cast<ID3D11DeviceContext_OMSetRenderTargets_Proc>(OMSetRenderTargets.MemoryOriginalProc)(NumViews,
			ArrayRTV, pDepthStencilView);

		Arguments::PostOMSetRenderTargets(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		reinterpret_cast<ID3D11DeviceContext_OMSetRenderTargets_Proc>(OMSetRenderTargets.MemoryOriginalProc)(NumViews,
			ppRenderTargetViews, pDepthStencilView);
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

		reinterpret_cast<ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc>(OMSetRenderTargetsAndUnorderedAccessViews.MemoryOriginalProc)(NumRTVs,
			ArrayRTV, pDepthStencilView,
			UAVStartSlot, NumUAVs, ArrayUAV, ArrayUAVInitialCounts);

		Arguments::PostOMSetRenderTargetsAndUnorderedAccessViews(Args, S_OK);
		HookCallback(&Args);
	}
	else {
		reinterpret_cast<ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc>(OMSetRenderTargetsAndUnorderedAccessViews.MemoryOriginalProc)(NumRTVs,
			ppRenderTargetViews, pDepthStencilView,
			UAVStartSlot, NumUAVs, ppUnorderedAccessViews, pUAVInitialCounts);
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
		return 1;
	}
	return 0;
}



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
	IDXGISwapChain** SwapChain, IDXGISwapChain1** SwapChain1)
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
	result = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, NULL, 0,
		FeatureLevels, ARRAYSIZE(FeatureLevels),
		D3D11_SDK_VERSION, Device, nullptr, Context);
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
		::DXGI_SWAP_CHAIN_DESC1 SwapChainDesc{};
		SwapChainDesc.Width = 640;
		SwapChainDesc.Height = 360;
		SwapChainDesc.Format = ::DXGI_FORMAT_R8G8B8A8_UNORM;
		SwapChainDesc.SampleDesc.Count = 1;
		SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		SwapChainDesc.BufferCount = 2;
		SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
		SwapChainDesc.Flags = 0;

		result = (*Factory2)->CreateSwapChainForHwnd(*Device, hWndForTest, &SwapChainDesc, nullptr, nullptr, SwapChain1);
		if (FAILED(result)) return result;

		result = (*SwapChain1)->QueryInterface(SwapChain);
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
		result = (*Factory)->CreateSwapChain(*Device, &SwapChainDesc, SwapChain);
		if (FAILED(result)) return result;
	}

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
