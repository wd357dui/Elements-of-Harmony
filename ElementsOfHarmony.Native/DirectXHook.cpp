#include "pch.h"
#include "DirectXHook.h"

using namespace std;

typedef LPCWSTR(__stdcall* StringVariableCallback)(LPCWSTR Name);
StringVariableCallback VertexShader;
StringVariableCallback PixelShader;

atomic_bool IsHDR;
atomic_bool WithinOverlayPass;
atomic_bool WithinPresentPass;
bool HDRTakeOverOutputFormat;
float HDRDynamicRangeFactor;

extern "C" __declspec(dllimport) void __stdcall SetHookCallback2(CallbackProc HookCallback2);
extern "C" __declspec(dllimport) void __stdcall ForceBreakpoint();
extern "C" __declspec(dllimport) LogCallbackProc LogCallback;

extern "C" __declspec(dllimport) HRESULT __stdcall GetName(_In_ IUnknown* D3D11_Interface, _Out_ LPCSTR* ppCharArray);
extern "C" __declspec(dllimport) HRESULT __stdcall CompilePixelShader(_In_ ID3D11Device* Device, _In_ LPCWSTR FileName, _In_ LPCSTR EntryPoint,
	_In_opt_ LPCSTR DebugName, _Out_ ID3D11PixelShader** PixelShader);

extern "C" {
	__declspec(dllexport) HRESULT __stdcall InitNativeCallbacks(
		bool HDRTakeOverOutputFormat,
		float HDRDynamicRangeFactor);
	__declspec(dllexport) void __stdcall SetCallbacks(
		StringVariableCallback VertexShader, StringVariableCallback PixelShader);
	__declspec(dllexport) void __stdcall SetIsHDR(bool IsHDR);
	__declspec(dllexport) void __stdcall SetWithinOverlayPass(bool WithinOverlayPass);
}

std::set<ComPtr<ID3D11Texture2D>> SwapChainBackBufferTextures;
static void SwapChainGetBackBufferTexture(IDXGISwapChain* pSwapChain)
{
	ComPtr<ID3D11Texture2D> Texture;
	pSwapChain->GetBuffer(0, IID_PPV_ARGS(&Texture));
	SwapChainBackBufferTextures.insert(Texture);
}
static void DirectXHook_PostPresent1_Hook(Arguments* Args)
{
	SwapChainGetBackBufferTexture(reinterpret_cast<IDXGISwapChain*>(Args->PPV));
	WithinPresentPass = false;
}
static void DirectXHook_PostPresent_Hook(Arguments* Args)
{
	SwapChainGetBackBufferTexture(reinterpret_cast<IDXGISwapChain*>(Args->PPV));
	WithinPresentPass = false;
}
static void DirectXHook_PrePresent1_Hook(Arguments* Args)
{
	WithinPresentPass = true;
}
static void DirectXHook_PrePresent_Hook(Arguments* Args)
{
	WithinPresentPass = true;
}

std::map<ComPtr<ID3D11RenderTargetView>, ComPtr<ID3D11Texture2D>> RenderTargetViewTexturesMap;
static void DirectXHook_PreResizeBuffers_Hook(Arguments* Args)
{
	SwapChainBackBufferTextures.clear();
	RenderTargetViewTexturesMap.clear();
}
static void DirectXHook_PostCreateRenderTargetView_Hook(Arguments* Args)
{
	HRESULT result = 0;

	ID3D11Resource*& pResource = *reinterpret_cast<ID3D11Resource**>(Args->Args[0]);
	ID3D11RenderTargetView*& pRTV = *reinterpret_cast<ID3D11RenderTargetView**>(Args->Args[2]);

	ComPtr<ID3D11RenderTargetView> RTV;
	ComPtr<ID3D11Texture2D> Texture2D;

	result = pResource->QueryInterface(IID_PPV_ARGS(&Texture2D));
	if (FAILED(result)) {
		LogCallback((L"QueryInterface from ID3D11Resource to ID3D11Texture2D failed, return value " + to_wstring(result)).c_str());
		ForceBreakpoint();
	}
	result = pRTV->QueryInterface(IID_PPV_ARGS(&RTV));
	if (FAILED(result)) {
		LogCallback((L"QueryInterface from ID3D11RenderTargetView to ID3D11RenderTargetView failed, return value " + to_wstring(result)).c_str());
		ForceBreakpoint();
	}

	RenderTargetViewTexturesMap[RTV] = Texture2D;
}

HRESULT static EnsurePixelShader(ID3D11Device* Device, LPCWSTR WName, LPCSTR Name, ComPtr<ID3D11PixelShader>& Shader)
{
	HRESULT result = 0;
	if (Shader == nullptr) {
		LPCWSTR FilePath = PixelShader(WName);
		if (FilePath == nullptr) {
			LogCallback((L"Pixel shader \"" + wstring(WName) + L"\" not found!").c_str());
		}
		else {
			result = CompilePixelShader(Device, FilePath, "main", Name, &Shader);
			if (FAILED(result)) {
				LogCallback((L"CompilePixelShader failed, return value " + to_wstring(result)).c_str());
				ForceBreakpoint();
			}
		}
	}
	return S_OK;
}

ComPtr<ID3D11PixelShader> BlitCopyHDRTonemap_Replacement;
std::set<ID3D11PixelShader*> Is_BlitCopyHDRTonemap;
std::set<ID3D11PixelShader*> IsNot_BlitCopyHDRTonemap;
static void DirectXHook_PrePSSetShader_Hook(Arguments* Args)
{
	if (::WithinOverlayPass) return;
	if (::WithinPresentPass) return;
	if (!::HDRTakeOverOutputFormat) return;
	if (!::IsHDR) return;

	HRESULT result = 0;

	ID3D11DeviceContext* pDeviceContext = reinterpret_cast<ID3D11DeviceContext*>(Args->PPV);
	ID3D11PixelShader*& pPixelShader = *reinterpret_cast<ID3D11PixelShader**>(Args->Args[0]);

	if (pPixelShader == nullptr) return;
	if (IsNot_BlitCopyHDRTonemap.contains(pPixelShader)) return;

	ComPtr<ID3D11Device> Device;
	pDeviceContext->GetDevice(&Device);

	LPCSTR DebugName = nullptr;
	bool Is = Is_BlitCopyHDRTonemap.contains(pPixelShader);
	if (!Is) {
		result = GetName(pPixelShader, &DebugName);
		if (FAILED(result) && result != DXGI_ERROR_NOT_FOUND) {
			LogCallback((L"get debug name from ID3D11PixelShader failed, return value " + to_wstring(result)).c_str());
			ForceBreakpoint();
		}
	}

	if (Is || (DebugName != nullptr && strcmp(DebugName, "Hidden/BlitCopyHDRTonemap") == 0)) {
		Is_BlitCopyHDRTonemap.insert(pPixelShader);
		EnsurePixelShader(Device.Get(), L"BlitCopyHDRTonemap_Replacement", "BlitCopyHDRTonemap_Replacement",
			BlitCopyHDRTonemap_Replacement);

		if (BlitCopyHDRTonemap_Replacement != nullptr) {
			pPixelShader = BlitCopyHDRTonemap_Replacement.Get();
		}
	}
	else IsNot_BlitCopyHDRTonemap.insert(pPixelShader);
}

HRESULT static EnsureConstantBuffer(_In_ ID3D11Device* Device, _In_ ID3D11DeviceContext* DeviceContext,
	ComPtr<ID3D11Buffer>& ConstantBuffer, _In_reads_(4) float* InitialValues)
{
	HRESULT result = 0;

	if (ConstantBuffer == nullptr) {
		D3D11_BUFFER_DESC Desc = {
			.ByteWidth = 4 * sizeof(float),
			.Usage = D3D11_USAGE_DEFAULT,
			.BindFlags = D3D11_BIND_CONSTANT_BUFFER,
			.CPUAccessFlags = 0,
			.MiscFlags = 0,
			.StructureByteStride = sizeof(float),
		};

		D3D11_SUBRESOURCE_DATA Data = {
			.pSysMem = InitialValues,
			.SysMemPitch = 0,
			.SysMemSlicePitch = 0,
		};

		result = Device->CreateBuffer(&Desc, &Data, &ConstantBuffer);
		if (FAILED(result)) return result;
	}

	return result;
}

ComPtr<ID3D11PixelShader> CopyTextureToSwapChainHDR;
ComPtr<ID3D11Buffer> CopyTextureToSwapChainHDR_ConstantBuffer;
static void OnDrawCall(ID3D11DeviceContext* pDeviceContext)
{
	if (::WithinOverlayPass) return;
	if (::WithinPresentPass) return;
	if (!::HDRTakeOverOutputFormat) return;
	if (!::IsHDR) return;

	HRESULT result = 0;

	ComPtr<ID3D11Device> Device;
	pDeviceContext->GetDevice(&Device);

	ComPtr<ID3D11RenderTargetView> RTV;
	pDeviceContext->OMGetRenderTargets(1, &RTV, nullptr);

	if (RTV != nullptr && SwapChainBackBufferTextures.contains(RenderTargetViewTexturesMap[RTV]))
	{
		result = EnsurePixelShader(Device.Get(), L"CopyTextureToSwapChainHDR", "CopyTextureToSwapChainHDR",
			CopyTextureToSwapChainHDR);
		if (FAILED(result)) {
			ForceBreakpoint();
		}

		if (CopyTextureToSwapChainHDR != nullptr) {

			pDeviceContext->PSSetShader(CopyTextureToSwapChainHDR.Get(), nullptr, 0);

			static float Values[4] {
				HDRDynamicRangeFactor = ::HDRDynamicRangeFactor,
				0.0f, 0.0f, 0.0f,
			};
			result = EnsureConstantBuffer(Device.Get(), pDeviceContext, CopyTextureToSwapChainHDR_ConstantBuffer, Values);
			if (FAILED(result)) {
				ForceBreakpoint();
			}

			ID3D11Buffer* ConstantBuffers[1] { CopyTextureToSwapChainHDR_ConstantBuffer.Get()};
			pDeviceContext->PSSetConstantBuffers(0, 1, ConstantBuffers);
		}
	}
}
static void DirectXHook_PreDraw_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawAuto_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawIndexed_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawIndexedInstanced_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawIndexedInstancedIndirect_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawInstanced_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}
static void DirectXHook_PreDrawInstancedIndirect_Hook(Arguments* Args)
{
	OnDrawCall(reinterpret_cast<ID3D11DeviceContext*>(Args->PPV));
}

static void HookCallback(Arguments* Args)
{
	switch (Args->VTableIndex)
	{
	case IDXGISwapChain_Present_VTableIndex:
		if (Args->IID == __uuidof(IDXGISwapChain)) {
			if (Args->Post) DirectXHook_PostPresent_Hook(Args);
			else DirectXHook_PrePresent_Hook(Args);
		}
		break;
	case IDXGISwapChain1_Present1_VTableIndex:
		if (Args->IID == __uuidof(IDXGISwapChain1)) {
			if (Args->Post) DirectXHook_PostPresent1_Hook(Args);
			else DirectXHook_PrePresent1_Hook(Args);
		}
		break;
	case ID3D11Device_CreateRenderTargetView_VTableIndex: // ID3D11DeviceContext_PSSetShader_VTableIndex
		if (Args->IID == __uuidof(ID3D11Device)) {
			if (Args->Post) DirectXHook_PostCreateRenderTargetView_Hook(Args);
		}
		else if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PrePSSetShader_Hook(Args);
		}
		break;
	case IDXGISwapChain_ResizeBuffers_VTableIndex: //ID3D11DeviceContext_Draw_VTableIndex:
		if (Args->IID == __uuidof(IDXGISwapChain)) {
			if (!Args->Post) DirectXHook_PreResizeBuffers_Hook(Args);
		}
		else if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDraw_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawAuto_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawAuto_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawIndexed_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawIndexed_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawIndexedInstanced_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawIndexedInstanced_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawIndexedInstancedIndirect_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawIndexedInstancedIndirect_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawInstanced_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawInstanced_Hook(Args);
		}
		break;
	case ID3D11DeviceContext_DrawInstancedIndirect_VTableIndex:
		if (Args->IID == __uuidof(ID3D11DeviceContext)) {
			if (!Args->Post) DirectXHook_PreDrawInstancedIndirect_Hook(Args);
		}
		break;
	}
}

HRESULT __stdcall InitNativeCallbacks(bool HDRTakeOverOutputFormat, float HDRDynamicRangeFactor)
{
	::HDRTakeOverOutputFormat = HDRTakeOverOutputFormat;
	::HDRDynamicRangeFactor = HDRDynamicRangeFactor;
	SetHookCallback2(HookCallback);
	return S_OK;
}

void __stdcall SetCallbacks(
	StringVariableCallback VertexShader, StringVariableCallback PixelShader)
{
	::VertexShader = VertexShader;
	::PixelShader = PixelShader;
}

void __stdcall SetIsHDR(bool IsHDR)
{
	::IsHDR = IsHDR;
}

void __stdcall SetWithinOverlayPass(bool WithinOverlayPass)
{
	::WithinOverlayPass = WithinOverlayPass;
}
