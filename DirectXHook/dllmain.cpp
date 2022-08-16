#include "pch.h"

BOOL APIENTRY DllMain(HMODULE hModule,
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

extern "C" __declspec(dllexport) int __stdcall InstallHook();
extern "C" __declspec(dllexport) bool __stdcall CanDraw();
extern "C" __declspec(dllexport) void __stdcall SetPlayer1(bool Tracked,
	bool LeftHolding, bool RightHolding,
	bool LeftOutOfBounds, bool RightOutOfBounds,
	bool A, bool B, bool Y, bool Menu,
	float ShoulderLeftX, float ShoulderLeftY, float ShoulderRightX, float ShoulderRightY,
	float OriginLeftX, float OriginLeftY, float OriginRightX, float OriginRightY,
	float CoordLeftX, float CoordLeftY, float CoordRightX, float CoordRightY);
extern "C" __declspec(dllexport) void __stdcall SetPlayer2(bool Tracked,
	bool LeftHolding, bool RightHolding,
	bool LeftOutOfBounds, bool RightOutOfBounds,
	bool A, bool B, bool Y, bool Menu,
	float ShoulderLeftX, float ShoulderLeftY, float ShoulderRightX, float ShoulderRightY,
	float OriginLeftX, float OriginLeftY, float OriginRightX, float OriginRightY,
	float CoordLeftX, float CoordLeftY, float CoordRightX, float CoordRightY);

typedef HRESULT(__stdcall* PresentProc)(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags);
typedef HRESULT(__stdcall* Present1Proc)(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
PresentProc OriginalPresentProc = nullptr;
PresentProc OriginalPresentProc1 = nullptr;
Present1Proc OriginalPresent1Proc1 = nullptr;

ComPtr<IDXGIFactory> Factory;
ComPtr<IDXGIFactory2> Factory2;
ComPtr<IDXGISwapChain> SwapChain;
ComPtr<IDXGISwapChain1> SwapChain1;
HWND hWndForTest = NULL;

HRESULT CreateHwndForTest();
LRESULT CALLBACK DummyWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
HRESULT CloseHwndForTest();
HRESULT __stdcall PresentOverride(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags);
HRESULT __stdcall PresentOverride1(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags);
HRESULT __stdcall Present1Override1(IDXGISwapChain1* Ptr, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);

int InstallHook()
{
	HRESULT result = 0;

	if (OriginalPresentProc != nullptr) return S_OK;

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

	result = CreateHwndForTest();
	if (result != S_OK) return result;

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

	DXGI_SWAP_CHAIN_DESC SwapChainDesc{};

	if (Factory2 != nullptr) {
		DXGI_SWAP_CHAIN_DESC1 SwapChainDesc{};
		SwapChainDesc.Width = 640;
		SwapChainDesc.Height = 360;
		SwapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
		SwapChainDesc.SampleDesc.Count = 1;
		SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		SwapChainDesc.BufferCount = 2;
		SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
		SwapChainDesc.Flags = 0;
		result = Factory2->CreateSwapChainForHwnd(Device.Get(), hWndForTest, &SwapChainDesc, nullptr, nullptr, &SwapChain1);
		if (FAILED(result)) return result;

		IDXGISwapChain1* Ptr = SwapChain1.Get();
		intptr_t VTableAddress = reinterpret_cast<intptr_t*>(Ptr)[0];
		intptr_t* VTable = reinterpret_cast<intptr_t*>(VTableAddress);
		OriginalPresentProc1 = reinterpret_cast<PresentProc>(VTable[8]);
		OriginalPresent1Proc1 = reinterpret_cast<Present1Proc>(VTable[22]);

		DWORD OldProtect;
		VirtualProtect(VTable + 8, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect);
		VTable[8] = reinterpret_cast<intptr_t>(PresentOverride1);
		VirtualProtect(VTable + 8, sizeof(intptr_t), OldProtect, &OldProtect);

		VirtualProtect(VTable + 22, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect);
		VTable[22] = reinterpret_cast<intptr_t>(Present1Override1);
		VirtualProtect(VTable + 22, sizeof(intptr_t), OldProtect, &OldProtect);

		result = SwapChain1.As(&SwapChain);
		if (FAILED(result)) return result;
		goto swap_chain;
	}

	SwapChainDesc.BufferDesc.Width = 640;
	SwapChainDesc.BufferDesc.Height = 320;
	SwapChainDesc.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	SwapChainDesc.SampleDesc.Count = 1;
	SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	SwapChainDesc.BufferCount = 2;
	SwapChainDesc.OutputWindow = hWndForTest;
	SwapChainDesc.Windowed = TRUE;
	SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
	SwapChainDesc.Flags = 0;
	result = Factory->CreateSwapChain(Device.Get(), &SwapChainDesc, &SwapChain);
	if (FAILED(result)) return result;

swap_chain:
	IDXGISwapChain* Ptr = SwapChain.Get();
	intptr_t VTableAddress = reinterpret_cast<intptr_t*>(Ptr)[0];
	intptr_t* VTable = reinterpret_cast<intptr_t*>(VTableAddress);
	OriginalPresentProc = reinterpret_cast<PresentProc>(VTable[8]);

	DWORD OldProtect;
	VirtualProtect(VTable + 8, sizeof(intptr_t), PAGE_EXECUTE_READWRITE, &OldProtect);
	VTable[8] = reinterpret_cast<intptr_t>(PresentOverride);
	VirtualProtect(VTable + 8, sizeof(intptr_t), OldProtect, &OldProtect);

	SwapChain.Reset();
	Device.Reset();
	CloseHwndForTest();

	return result;
}

HRESULT CreateHwndForTest()
{
	constexpr WCHAR ClassName[] = L"MAGIC";
	constexpr WCHAR WindowName[] = L"Magic";
	HINSTANCE Module = GetModuleHandleW(NULL);

	WNDCLASSEXW wcexw{};
	wcexw.cbSize = sizeof(WNDCLASSEXW);
	wcexw.style = CS_HREDRAW | CS_VREDRAW;
	wcexw.lpfnWndProc = DummyWndProc;
	wcexw.hInstance = Module;
	wcexw.hIcon = NULL;
	wcexw.hIconSm = NULL;
	wcexw.hCursor = LoadCursorW(nullptr, IDC_ARROW);
	wcexw.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wcexw.lpszClassName = ClassName;

	ATOM Class = RegisterClassExW(&wcexw);
	if (Class == NULL) return GetLastError();

	hWndForTest = CreateWindowW(ClassName, WindowName, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0,
		NULL, NULL, Module, nullptr);
	if (hWndForTest == NULL) return GetLastError();

	return S_OK;
}

LRESULT DummyWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message)
	{
	case WM_CLOSE:
		DestroyWindow(hWnd);
		return 0;
	case WM_DESTROY:
		PostQuitMessage(0);
		return 0;
	}
	return DefWindowProcW(hWnd, message, wParam, lParam);
}

HRESULT CloseHwndForTest()
{
	if (!CloseWindow(hWndForTest)) {
		return GetLastError();
	}
	else return S_OK;
	MSG msg{};
	while (GetMessageW(&msg, hWndForTest, 0, 0)) {
		TranslateMessage(&msg);
		DispatchMessageW(&msg);
	}
	return S_OK;
}

ComPtr<IDXGIDevice> DXGIDevice;
ComPtr<IDXGISurface> Surface;

HRESULT ProcessDevice(IDXGIDevice* Device, IDXGISwapChain* SwapChain);
HRESULT FlushDraw();

HRESULT __stdcall PresentOverride(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags)
{
	ComPtr<IDXGIDevice> Device;
	Ptr->GetDevice(IID_PPV_ARGS(&Device));
	if (DXGIDevice != Device || SwapChain.Get() != Ptr) {
		ProcessDevice(Device.Get(), Ptr);
		DXGIDevice = Device;
		SwapChain = Ptr;
	}
	FlushDraw();
	return OriginalPresentProc(Ptr, SyncInterval, Flags);
}

HRESULT __stdcall PresentOverride1(IDXGISwapChain* Ptr, UINT SyncInterval, UINT Flags)
{
	ComPtr<IDXGIDevice> Device;
	Ptr->GetDevice(IID_PPV_ARGS(&Device));
	if (DXGIDevice != Device || SwapChain.Get() != Ptr) {
		ProcessDevice(Device.Get(), Ptr);
		DXGIDevice = Device;
		SwapChain = Ptr;
	}
	FlushDraw();
	return OriginalPresentProc1(Ptr, SyncInterval, Flags);
}

HRESULT __stdcall Present1Override1(IDXGISwapChain1* Ptr, UINT SyncInterval, UINT Flags, _In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters)
{
	ComPtr<IDXGIDevice> Device;
	Ptr->GetDevice(IID_PPV_ARGS(&Device));
	if (DXGIDevice != Device || SwapChain1.Get() != Ptr) {
		ProcessDevice(Device.Get(), Ptr);
		DXGIDevice = Device;
		SwapChain1 = Ptr;
	}
	FlushDraw();
	return OriginalPresent1Proc1(Ptr, SyncInterval, Flags, pPresentParameters);
}

ComPtr<ID2D1Factory> D2DFactory;
ComPtr<ID2D1Device> D2DDevice;
ComPtr<ID2D1DeviceContext> D2DContext;
ComPtr<ID2D1Bitmap1> Bitmap;
UINT Width = 0, Height = 0;

void ReleaseBrushes();

HRESULT ProcessDevice(IDXGIDevice* Device, IDXGISwapChain* SwapChain)
{
	HRESULT result = 0;

	if (D2DFactory == nullptr) {
		result = D2D1CreateFactory<ID2D1Factory>(D2D1_FACTORY_TYPE_MULTI_THREADED, &D2DFactory);
		if (FAILED(result)) return result;
	}

	ReleaseBrushes();

	result = SwapChain->GetBuffer(0, IID_PPV_ARGS(&Surface));
	if (FAILED(result)) return result;

	DXGI_SURFACE_DESC Desc;
	result = Surface->GetDesc(&Desc);
	if (FAILED(result)) return result;

	Width = Desc.Width;
	Height = Desc.Height;

	D2D1_CREATION_PROPERTIES D2DDeviceProperties{};
	D2DDeviceProperties.threadingMode = D2D1_THREADING_MODE_MULTI_THREADED;
	D2DDeviceProperties.debugLevel = D2D1_DEBUG_LEVEL_NONE;
	D2DDeviceProperties.options = D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS;
	result = D2D1CreateDevice(Device, D2DDeviceProperties, &D2DDevice);
	if (FAILED(result)) return result;

	result = D2DDevice->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS, &D2DContext);
	if (FAILED(result)) return result;

	D2D1_BITMAP_PROPERTIES1 BitmapProperties{};
	BitmapProperties.pixelFormat.format = Desc.Format;
	BitmapProperties.pixelFormat.alphaMode = D2D1_ALPHA_MODE_PREMULTIPLIED;
	BitmapProperties.dpiX = 96.0f;
	BitmapProperties.dpiY = 96.0f;
	BitmapProperties.bitmapOptions = D2D1_BITMAP_OPTIONS_TARGET;
	result = D2DContext->CreateBitmapFromDxgiSurface(Surface.Get(), BitmapProperties, &Bitmap);
	if (FAILED(result)) return result;

	return result;
}

bool CanDraw()
{
	return Surface != nullptr && D2DDevice != nullptr && D2DContext != nullptr && Bitmap != nullptr;
}

ComPtr<ID2D1SolidColorBrush> White;
ComPtr<ID2D1SolidColorBrush> WhiteTransparent;
ComPtr<ID2D1SolidColorBrush> Gray;
ComPtr<ID2D1SolidColorBrush> GrayTransparent;
ComPtr<ID2D1SolidColorBrush> Black;
ComPtr<ID2D1SolidColorBrush> BlackTransparent;
ComPtr<ID2D1SolidColorBrush> ButtonA;
ComPtr<ID2D1SolidColorBrush> ButtonATransparent;
ComPtr<ID2D1SolidColorBrush> ButtonB;
ComPtr<ID2D1SolidColorBrush> ButtonBTransparent;
ComPtr<ID2D1SolidColorBrush> ButtonY;
ComPtr<ID2D1SolidColorBrush> ButtonYTransparent;
ComPtr<ID2D1SolidColorBrush> Player1;
ComPtr<ID2D1SolidColorBrush> Player1Transparent;
ComPtr<ID2D1SolidColorBrush> Player1Transparenter;
ComPtr<ID2D1SolidColorBrush> Player2;
ComPtr<ID2D1SolidColorBrush> Player2Transparent;
ComPtr<ID2D1SolidColorBrush> Player2Transparenter;

ComPtr<IDWriteFactory> DWriteFactory;
ComPtr<IDWriteTextFormat> TextNormal, TextBold;
ComPtr<ID2D1Mesh> Mesh;

HRESULT InitBrushes()
{
	HRESULT result = 0;

	D2D1_COLOR_F Color{};

	if (White == nullptr) {
		Color = { 1.0f, 1.0f, 1.0f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &White);
		if (FAILED(result)) return result;
	}
	if (WhiteTransparent == nullptr) {
		Color = { 1.0f, 1.0f, 1.0f, 0.3f };
		result = D2DContext->CreateSolidColorBrush(Color, &WhiteTransparent);
		if (FAILED(result)) return result;
	}
	if (Gray == nullptr) {
		Color = { 0.5f, 0.5f, 0.5f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &Gray);
		if (FAILED(result)) return result;
	}
	if (GrayTransparent == nullptr) {
		Color = { 0.5f, 0.5f, 0.5f, 0.3f };
		result = D2DContext->CreateSolidColorBrush(Color, &GrayTransparent);
		if (FAILED(result)) return result;
	}

	if (ButtonA == nullptr) {
		Color = { 0.0f, 1.0f, 0.0f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonA);
		if (FAILED(result)) return result;
	}
	if (ButtonATransparent == nullptr) {
		Color = { 0.0f, 1.0f, 0.0f, 0.2f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonATransparent);
		if (FAILED(result)) return result;
	}
	if (ButtonB == nullptr) {
		Color = { 1.0f, 0.0f, 0.0f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonB);
		if (FAILED(result)) return result;
	}
	if (ButtonBTransparent == nullptr) {
		Color = { 1.0f, 0.0f, 0.0f, 0.2f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonBTransparent);
		if (FAILED(result)) return result;
	}
	if (ButtonY == nullptr) {
		Color = { 1.0f, 1.0f, 0.0f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonY);
		if (FAILED(result)) return result;
	}
	if (ButtonYTransparent == nullptr) {
		Color = { 1.0f, 1.0f, 0.0f, 0.2f };
		result = D2DContext->CreateSolidColorBrush(Color, &ButtonYTransparent);
		if (FAILED(result)) return result;
	}
	if (Black == nullptr) {
		Color = { 0.0f, 0.0f, 0.0f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &Black);
		if (FAILED(result)) return result;
	}
	if (BlackTransparent == nullptr) {
		Color = { 0.1f, 0.1f, 0.1f, 0.2f };
		result = D2DContext->CreateSolidColorBrush(Color, &BlackTransparent);
		if (FAILED(result)) return result;
	}

	if (Player1 == nullptr) {
		Color = { 0.92f, 0.0f, 0.57f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player1);
		if (FAILED(result)) return result;
	}
	if (Player1Transparent == nullptr) {
		Color = { 0.92f, 0.0f, 0.57f, 0.3f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player1Transparent);
		if (FAILED(result)) return result;
	}
	if (Player1Transparenter == nullptr) {
		Color = { 0.92f, 0.0f, 0.57f, 0.1f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player1Transparenter);
		if (FAILED(result)) return result;
	}
	if (Player2 == nullptr) {
		Color = { 0.19f, 0.66f, 0.92f, 1.0f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player2);
		if (FAILED(result)) return result;
	}
	if (Player2Transparent == nullptr) {
		Color = { 0.19f, 0.66f, 0.92f, 0.3f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player2Transparent);
		if (FAILED(result)) return result;
	}
	if (Player2Transparenter == nullptr) {
		Color = { 0.19f, 0.66f, 0.92f, 0.1f };
		result = D2DContext->CreateSolidColorBrush(Color, &Player2Transparenter);
		if (FAILED(result)) return result;
	}

	if (DWriteFactory == nullptr) {
		result = DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), &DWriteFactory);
		if (FAILED(result)) return result;
	}

	result = DWriteFactory->CreateTextFormat(L"Segoe UI", nullptr,
		DWRITE_FONT_WEIGHT_NORMAL, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL,
		72.0f, L"en-US", &TextNormal);
	if (FAILED(result)) return result;
	result = TextNormal->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_CENTER);
	if (FAILED(result)) return result;
	result = TextNormal->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_CENTER);
	if (FAILED(result)) return result;

	result = DWriteFactory->CreateTextFormat(L"Segoe UI", nullptr,
		DWRITE_FONT_WEIGHT_BOLD, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL,
		72.0f, L"en-US", &TextBold);
	if (FAILED(result)) return result;
	result = TextBold->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_CENTER);
	if (FAILED(result)) return result;
	result = TextBold->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_CENTER);
	if (FAILED(result)) return result;

	return result;
}

void ReleaseBrushes()
{
	White.Reset();
	WhiteTransparent.Reset();
	Gray.Reset();
	GrayTransparent.Reset();
	Black.Reset();
	BlackTransparent.Reset();
	ButtonA.Reset();
	ButtonATransparent.Reset();
	ButtonB.Reset();
	ButtonBTransparent.Reset();
	ButtonY.Reset();
	ButtonYTransparent.Reset();
	Player1.Reset();
	Player1Transparent.Reset();
	Player1Transparenter.Reset();
	Player2.Reset();
	Player2Transparent.Reset();
	Player2Transparenter.Reset();
}

bool hasPlayer1 = false, hasPlayer2 = false;
bool Player1Left = false, Player1Right = false, Player2Left = false, Player2Right = false;
bool Player1LeftOutOfBounds = false, Player1RightOutOfBounds = false, Player2LeftOutOfBounds = false, Player2RightOutOfBounds = false;
float Player1LeftShoulder[2]{ 0.0f }, Player1RightShoulder[2]{ 0.0f }, Player2LeftShoulder[2]{ 0.0f }, Player2RightShoulder[2]{ 0.0f };
float Player1LeftOrigin[2]{ 0.0f }, Player1RightOrigin[2]{ 0.0f }, Player2LeftOrigin[2]{ 0.0f }, Player2RightOrigin[2]{ 0.0f };
float Player1LeftCoord[2]{ 0.0f }, Player1RightCoord[2]{ 0.0f }, Player2LeftCoord[2]{ 0.0f }, Player2RightCoord[2]{ 0.0f };
bool Player1A = false, Player1B = false, Player1Y = false, Player1Menu = false;
bool Player2A = false, Player2B = false, Player2Y = false, Player2Menu = false;

D2D1_POINT_2F ConvertToCoordinate(float x, float y)
{
	x = (x + 1.0f) * 0.5f * float(Width);
	y = (1.0f - ((y + 1.0f) * 0.5f)) * float(Height);
	return { x, y };
}

D2D1_RECT_F ConvertToRect(float x, float y, float deviationX, float deviationY)
{
	D2D1_POINT_2F center = ConvertToCoordinate(x, y);
	center.x += deviationX;
	center.y += deviationY;
	return { center.x - 400.0f, center.y - 400.0f, center.x + 400.0f, center.y + 400.0f };
}

#undef DrawText

HRESULT FlushDraw()
{
	HRESULT result = 0;

	if (!CanDraw()) return E_ABORT;

	result = InitBrushes();
	if (FAILED(result)) return result;

	D2DContext->SetUnitMode(D2D1_UNIT_MODE_PIXELS);
	D2DContext->SetTarget(Bitmap.Get());
	D2DContext->BeginDraw();

	D2D1_ELLIPSE Ellipse{}, Buffer{};
	if (hasPlayer1) {
		D2DContext->DrawLine(ConvertToCoordinate(Player1LeftShoulder[0], Player1LeftShoulder[1]), ConvertToCoordinate(Player1LeftCoord[0], Player1LeftCoord[1]), Player1LeftOutOfBounds ? Player1Transparenter.Get() : Player1Transparent.Get(), 10.0f);
		D2DContext->DrawLine(ConvertToCoordinate(Player1RightShoulder[0], Player1RightShoulder[1]), ConvertToCoordinate(Player1RightCoord[0], Player1RightCoord[1]), Player1RightOutOfBounds ? Player1Transparenter.Get() : Player1Transparent.Get(), 10.0f);
		if (Player1Left) {
			Ellipse = { ConvertToCoordinate(Player1LeftOrigin[0], Player1LeftOrigin[1]), 150.0f, 150.0f };
			D2DContext->FillEllipse(&Ellipse, GrayTransparent.Get());

			D2DContext->DrawText(L"▼", 1, TextNormal.Get(), ConvertToRect(Player1LeftOrigin[0], Player1LeftOrigin[1], 0.0f, 100.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"▶", 1, TextNormal.Get(), ConvertToRect(Player1LeftOrigin[0], Player1LeftOrigin[1], 100.0f, 0.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"▲", 1, TextNormal.Get(), ConvertToRect(Player1LeftOrigin[0], Player1LeftOrigin[1], 0.0f, -100.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"◀", 1, TextNormal.Get(), ConvertToRect(Player1LeftOrigin[0], Player1LeftOrigin[1], -100.0f, 0.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);

			Ellipse = { ConvertToCoordinate(Player1LeftCoord[0], Player1LeftCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player1.Get());
		}
		else {
			Ellipse = { ConvertToCoordinate(Player1LeftCoord[0], Player1LeftCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player1LeftOutOfBounds ? Player1Transparenter.Get() : Player1Transparent.Get());
		}
		if (Player1Right) {
			Ellipse = { ConvertToCoordinate(Player1RightOrigin[0], Player1RightOrigin[1]), 150.0f, 150.0f };
			D2DContext->FillEllipse(&Ellipse, GrayTransparent.Get());

			Ellipse = { ConvertToCoordinate(Player1RightOrigin[0], Player1RightOrigin[1]), 50.0f, 50.0f };
			if (!Player1A) {
				Buffer = Ellipse;
				Buffer.point.y += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1A ? ButtonA.Get() : ButtonATransparent.Get());
				D2DContext->DrawText(L"A", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, 95.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"A", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, 95.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player1B) {
				Buffer = Ellipse;
				Buffer.point.x += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1B ? ButtonB.Get() : ButtonBTransparent.Get());
				D2DContext->DrawText(L"B", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"B", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player1Y) {
				Buffer = Ellipse;
				Buffer.point.y -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1Y ? ButtonY.Get() : ButtonYTransparent.Get());
				D2DContext->DrawText(L"Y", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, -105.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"Y", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, -105.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player1Menu) {
				Buffer = Ellipse;
				Buffer.point.x -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1Menu ? Black.Get() : BlackTransparent.Get());
				D2DContext->DrawText(L"≡", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], -100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"≡", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], -100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}

			Ellipse = { ConvertToCoordinate(Player1RightCoord[0], Player1RightCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player1.Get());

			Ellipse = { ConvertToCoordinate(Player1RightOrigin[0], Player1RightOrigin[1]), 50.0f, 50.0f };
			if (Player1A) {
				Buffer = Ellipse;
				Buffer.point.y += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1A ? ButtonA.Get() : ButtonATransparent.Get());
				D2DContext->DrawText(L"A", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, 95.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"A", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, 95.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player1B) {
				Buffer = Ellipse;
				Buffer.point.x += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1B ? ButtonB.Get() : ButtonBTransparent.Get());
				D2DContext->DrawText(L"B", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"B", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player1Y) {
				Buffer = Ellipse;
				Buffer.point.y -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1Y ? ButtonY.Get() : ButtonYTransparent.Get());
				D2DContext->DrawText(L"Y", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, -105.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"Y", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], 0.0f, -105.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player1Menu) {
				Buffer = Ellipse;
				Buffer.point.x -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player1Menu ? Black.Get() : BlackTransparent.Get());
				D2DContext->DrawText(L"≡", 1, TextBold.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], -100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"≡", 1, TextNormal.Get(), ConvertToRect(Player1RightOrigin[0], Player1RightOrigin[1], -100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
		}
		else {
			Ellipse = { ConvertToCoordinate(Player1RightCoord[0], Player1RightCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player1RightOutOfBounds ? Player1Transparenter.Get() : Player1Transparent.Get());
		}
	}
	if (hasPlayer2) {
		D2DContext->DrawLine(ConvertToCoordinate(Player2LeftShoulder[0], Player2LeftShoulder[1]), ConvertToCoordinate(Player2LeftCoord[0], Player2LeftCoord[1]), Player2LeftOutOfBounds ? Player2Transparenter.Get() : Player2Transparent.Get(), 10.0f);
		D2DContext->DrawLine(ConvertToCoordinate(Player2RightShoulder[0], Player2RightShoulder[1]), ConvertToCoordinate(Player2RightCoord[0], Player2RightCoord[1]), Player2RightOutOfBounds ? Player2Transparenter.Get() : Player2Transparent.Get(), 10.0f);
		if (Player2Left) {
			Ellipse = { ConvertToCoordinate(Player2LeftOrigin[0], Player2LeftOrigin[1]), 150.0f, 150.0f };
			D2DContext->FillEllipse(&Ellipse, GrayTransparent.Get());

			D2DContext->DrawText(L"▼", 1, TextNormal.Get(), ConvertToRect(Player2LeftOrigin[0], Player2LeftOrigin[1], 0.0f, 100.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"▶", 1, TextNormal.Get(), ConvertToRect(Player2LeftOrigin[0], Player2LeftOrigin[1], 100.0f, 0.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"▲", 1, TextNormal.Get(), ConvertToRect(Player2LeftOrigin[0], Player2LeftOrigin[1], 0.0f, -100.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			D2DContext->DrawText(L"◀", 1, TextNormal.Get(), ConvertToRect(Player2LeftOrigin[0], Player2LeftOrigin[1], -100.0f, 0.0f), WhiteTransparent.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);

			Ellipse = { ConvertToCoordinate(Player2LeftCoord[0], Player2LeftCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player2.Get());
		}
		else {
			Ellipse = { ConvertToCoordinate(Player2LeftCoord[0], Player2LeftCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player2LeftOutOfBounds ? Player2Transparenter.Get() : Player2Transparent.Get());
		}
		if (Player2Right) {
			Ellipse = { ConvertToCoordinate(Player2RightOrigin[0], Player2RightOrigin[1]), 150.0f, 150.0f };
			D2DContext->FillEllipse(&Ellipse, GrayTransparent.Get());

			Ellipse = { ConvertToCoordinate(Player2RightOrigin[0], Player2RightOrigin[1]), 50.0f, 50.0f };
			if (!Player2A) {
				Buffer = Ellipse;
				Buffer.point.y += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2A ? ButtonA.Get() : ButtonATransparent.Get());
				D2DContext->DrawText(L"A", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, 95.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"A", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, 95.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player2B) {
				Buffer = Ellipse;
				Buffer.point.x += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2B ? ButtonB.Get() : ButtonBTransparent.Get());
				D2DContext->DrawText(L"B", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"B", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player2Y) {
				Buffer = Ellipse;
				Buffer.point.y -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2Y ? ButtonY.Get() : ButtonYTransparent.Get());
				D2DContext->DrawText(L"Y", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, -105.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"Y", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, -105.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (!Player2Menu) {
				Buffer = Ellipse;
				Buffer.point.x -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2Menu ? Black.Get() : BlackTransparent.Get());
				D2DContext->DrawText(L"≡", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], -100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"≡", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], -100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}

			Ellipse = { ConvertToCoordinate(Player2RightCoord[0], Player2RightCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player2.Get());

			Ellipse = { ConvertToCoordinate(Player2RightOrigin[0], Player2RightOrigin[1]), 50.0f, 50.0f };
			if (Player2A) {
				Buffer = Ellipse;
				Buffer.point.y += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2A ? ButtonA.Get() : ButtonATransparent.Get());
				D2DContext->DrawText(L"A", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, 95.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"A", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, 95.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player2B) {
				Buffer = Ellipse;
				Buffer.point.x += 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2B ? ButtonB.Get() : ButtonBTransparent.Get());
				D2DContext->DrawText(L"B", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"B", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player2Y) {
				Buffer = Ellipse;
				Buffer.point.y -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2Y ? ButtonY.Get() : ButtonYTransparent.Get());
				D2DContext->DrawText(L"Y", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, -105.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"Y", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], 0.0f, -105.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
			if (Player2Menu) {
				Buffer = Ellipse;
				Buffer.point.x -= 100.0f;
				D2DContext->FillEllipse(&Buffer, Player2Menu ? Black.Get() : BlackTransparent.Get());
				D2DContext->DrawText(L"≡", 1, TextBold.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], -100.0f, -5.0f), Black.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
				D2DContext->DrawText(L"≡", 1, TextNormal.Get(), ConvertToRect(Player2RightOrigin[0], Player2RightOrigin[1], -100.0f, -5.0f), White.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
			}
		}
		else {
			Ellipse = { ConvertToCoordinate(Player2RightCoord[0], Player2RightCoord[1]), 40.0f, 40.0f };
			D2DContext->FillEllipse(&Ellipse, Player2RightOutOfBounds ? Player2Transparenter.Get() : Player2Transparent.Get());
		}
	}

	result = D2DContext->EndDraw();
	if (FAILED(result)) return result;

	return result;
}

void SetPlayer1(bool Tracked,
	bool LeftHolding, bool RightHolding,
	bool LeftOutOfBounds, bool RightOutOfBounds,
	bool A, bool B, bool Y, bool Menu,
	float ShoulderLeftX, float ShoulderLeftY, float ShoulderRightX, float ShoulderRightY,
	float OriginLeftX, float OriginLeftY, float OriginRightX, float OriginRightY,
	float CoordLeftX, float CoordLeftY, float CoordRightX, float CoordRightY)
{
	hasPlayer1 = Tracked;
	Player1Left = LeftHolding;
	Player1Right = RightHolding;
	Player1LeftOutOfBounds = LeftOutOfBounds;
	Player1RightOutOfBounds = RightOutOfBounds;
	Player1A = A;
	Player1B = B;
	Player1Y = Y;
	Player1Menu = Menu;
	Player1LeftShoulder[0] = ShoulderLeftX;
	Player1LeftShoulder[1] = ShoulderLeftY;
	Player1RightShoulder[0] = ShoulderRightX;
	Player1RightShoulder[1] = ShoulderRightY;
	Player1LeftOrigin[0] = OriginLeftX;
	Player1LeftOrigin[1] = OriginLeftY;
	Player1RightOrigin[0] = OriginRightX;
	Player1RightOrigin[1] = OriginRightY;
	Player1LeftCoord[0] = CoordLeftX;
	Player1LeftCoord[1] = CoordLeftY;
	Player1RightCoord[0] = CoordRightX;
	Player1RightCoord[1] = CoordRightY;
}

void SetPlayer2(bool Tracked,
	bool LeftHolding, bool RightHolding,
	bool LeftOutOfBounds, bool RightOutOfBounds,
	bool A, bool B, bool Y, bool Menu,
	float ShoulderLeftX, float ShoulderLeftY, float ShoulderRightX, float ShoulderRightY,
	float OriginLeftX, float OriginLeftY, float OriginRightX, float OriginRightY,
	float CoordLeftX, float CoordLeftY, float CoordRightX, float CoordRightY)
{
	hasPlayer2 = Tracked;
	Player2Left = LeftHolding;
	Player2Right = RightHolding;
	Player2LeftOutOfBounds = LeftOutOfBounds;
	Player2RightOutOfBounds = RightOutOfBounds;
	Player2A = A;
	Player2B = B;
	Player2Y = Y;
	Player2Menu = Menu;
	Player2LeftShoulder[0] = ShoulderLeftX;
	Player2LeftShoulder[1] = ShoulderLeftY;
	Player2RightShoulder[0] = ShoulderRightX;
	Player2RightShoulder[1] = ShoulderRightY;
	Player2LeftOrigin[0] = OriginLeftX;
	Player2LeftOrigin[1] = OriginLeftY;
	Player2RightOrigin[0] = OriginRightX;
	Player2RightOrigin[1] = OriginRightY;
	Player2LeftCoord[0] = CoordLeftX;
	Player2LeftCoord[1] = CoordLeftY;
	Player2RightCoord[0] = CoordRightX;
	Player2RightCoord[1] = CoordRightY;
}
