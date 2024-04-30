#include "pch.h"

using namespace std;
using namespace D2D1;

struct Device;

extern "C" {
	__declspec(dllexport) HRESULT __stdcall InitOverlay();
	__declspec(dllexport) void __stdcall ReleaseOverlay();

	__declspec(dllexport) HRESULT __stdcall SwapChainBeginDraw(_In_ IDXGISwapChain* SwapChain, UINT Index,
		_Out_ Device** ppInstance);
	__declspec(dllexport) HRESULT __stdcall SurfaceBeginDraw(_In_ IDXGISurface* Surface,
		_Out_ Device** ppInstance);
	__declspec(dllexport) HRESULT __stdcall StereoSurfaceBeginDraw(_In_ IDXGISurface* SurfaceLeft, _In_ IDXGISurface* SurfaceRight,
		_Out_ Device** ppInstance);

	__declspec(dllexport) HRESULT __stdcall SwapChainEndDraw(_In_ IDXGISwapChain* SwapChain, UINT Index,
		_In_ Device* pInstance);
	__declspec(dllexport) HRESULT __stdcall SurfaceEndDraw(_In_ IDXGISurface* Surface,
		_In_ Device* pInstance);
	__declspec(dllexport) HRESULT __stdcall StereoSurfaceEndDraw(_In_ IDXGISurface* SurfaceLeft, _In_ IDXGISurface* SurfaceRight,
		_In_ Device* pInstance);

	__declspec(dllexport) HRESULT __stdcall SetColor(_In_ Device* pInstance, D2D1_COLOR_F Color);
	__declspec(dllexport) HRESULT __stdcall SetOpacity(_In_ Device* pInstance, float Opacity);
	__declspec(dllexport) HRESULT __stdcall SetFont(_In_ Device* pInstance, LPCWSTR FontFamily,
		DWRITE_FONT_WEIGHT FontWeight, DWRITE_FONT_STYLE FontStyle, DWRITE_FONT_STRETCH FontStretch,
		float FontSize, LPCWSTR FontLocale);
	__declspec(dllexport) HRESULT __stdcall SetFontParams(_In_ Device* pInstance,
		DWRITE_TEXT_ALIGNMENT TextAlignment, DWRITE_PARAGRAPH_ALIGNMENT ParagraphAlignment, DWRITE_WORD_WRAPPING WordWrapping, 
		DWRITE_READING_DIRECTION ReadingDirection, DWRITE_FLOW_DIRECTION FlowDirection, FLOAT IncrementalTabStop,
		DWRITE_LINE_SPACING_METHOD LineSpacingMethod, FLOAT LineSpacing, FLOAT Baseline);
	__declspec(dllexport) HRESULT __stdcall SetGDICompatibleText(_In_ Device* pInstance, LPCWSTR Str,
		float LayoutWidth, float LayoutHeight, float PixelsPerDip);

	__declspec(dllexport) void __stdcall DrawEllipse(_In_ Device* pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY, float StrokeWidth);
	__declspec(dllexport) void __stdcall DrawLine(_In_ Device* pInstance, D2D1_POINT_2F Src, D2D1_POINT_2F Dst, float StrokeWidth);
	__declspec(dllexport) void __stdcall DrawRectangle(_In_ Device* pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY, float StrokeWidth);
#undef DrawText // this is not DrawTextA or DrawTextW in GDI
	__declspec(dllexport) void __stdcall DrawPlainText(_In_ Device* pInstance, LPCWSTR Str, D2D1_RECT_F Rect);
	__declspec(dllexport) HRESULT __stdcall DrawGDICompatibleText(_In_ Device* pInstance, D2D1_POINT_2F Origin);
	__declspec(dllexport) HRESULT __stdcall DrawGDICompatibleTextMetrics(_In_ Device* pInstance, UINT Index, UINT Length, float OriginX, float OriginY);
	__declspec(dllexport) HRESULT __stdcall DrawGDICompatibleTextCaret(_In_ Device* pInstance, bool Trailing, float StrokeWidth);
	__declspec(dllexport) void __stdcall FillEllipse(_In_ Device* pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY);
	__declspec(dllexport) void __stdcall FillRectangle(_In_ Device* pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY);

	__declspec(dllexport) void __stdcall SetTransform(_In_ Device* pInstance, Matrix3x2F Matrix);
	__declspec(dllexport) void __stdcall SetDpi(_In_ Device* pInstance, float DpiX, float DpiY);
}

HRESULT CreateRenderTargetFromSurface(IDXGISurface* Surface);

struct Device
{
	ComPtr<ID2D1Device> Device2D;
	ComPtr<ID2D1DeviceContext> Context2D;
	HRESULT Init(IDXGIDevice* DeviceDXGI);

	ComPtr<ID2D1SolidColorBrush> SolidColorBrush;
	ComPtr<IDWriteTextFormat> DWriteTextFormat;
	ComPtr<IDWriteTextLayout> DWriteLayoutGDICompatible;
	vector<DWRITE_HIT_TEST_METRICS> DWriteTextMetrics;
};
struct RenderTarget
{
	ComPtr<ID2D1Bitmap1> Target;
	HRESULT Init(Device& Device, IDXGISurface* Surface);
};

ComPtr<IDWriteFactory> DWriteFactory;
map<ComPtr<IDXGIDevice>, Device>					DeviceResources;
map<ComPtr<IDXGIDevice>, set<ComPtr<IDXGISurface>>>	DeviceSurfaces;
map<ComPtr<IDXGISurface>, RenderTarget>				RenderTargets;
map<ComPtr<IDXGISurface>, ComPtr<ID2D1CommandList>>	CommandLists;

HRESULT CreateRenderTargetFromSurface(IDXGISurface* Surface)
{
	HRESULT result = 0;
	ComPtr<IDXGIDevice> DeviceDXGI;
	ComPtr<IDXGISurface> SurfaceDXGI;
	result = Surface->GetDevice(IID_PPV_ARGS(&DeviceDXGI));
	if (FAILED(result)) return result;
	result = Surface->QueryInterface(IID_PPV_ARGS(&SurfaceDXGI));
	if (FAILED(result)) return result;

	if (DeviceSurfaces[DeviceDXGI].contains(SurfaceDXGI)) {
		return S_OK;
	}

	DeviceSurfaces[DeviceDXGI].insert(SurfaceDXGI);

	Device& Device = DeviceResources[DeviceDXGI];
	result = Device.Init(DeviceDXGI.Get());
	if (FAILED(result)) return result;
	result = RenderTargets[SurfaceDXGI].Init(Device, SurfaceDXGI.Get());
	if (FAILED(result)) return result;
	return result;
}

HRESULT __stdcall InitOverlay()
{
	HRESULT result = 0;

	result = CoInitialize(NULL);
	if (FAILED(result)) return result;
	result = DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), &DWriteFactory);
	if (FAILED(result)) return result;

	return result;
}

void __stdcall ReleaseOverlay()
{
	DWriteFactory.Reset();
	CoUninitialize();
}

HRESULT Device::Init(IDXGIDevice* DeviceDXGI)
{
	HRESULT result = 0;
	if (Device2D != nullptr && Context2D != nullptr) return S_OK;

	result = D2D1CreateDevice(DeviceDXGI, D2D1_CREATION_PROPERTIES{
		.threadingMode = D2D1_THREADING_MODE_SINGLE_THREADED, // D2D1_THREADING_MODE_MULTI_THREADED,
#ifdef _DEBUG
			.debugLevel = D2D1_DEBUG_LEVEL_WARNING,
#else
			.debugLevel = D2D1_DEBUG_LEVEL_NONE,
#endif
			.options = D2D1_DEVICE_CONTEXT_OPTIONS_NONE // D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS,
		}, &Device2D);
	if (FAILED(result)) return result;
//	result = Device2D->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS, &Context2D);
	result = Device2D->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &Context2D);
	if (FAILED(result)) return result;

	return result;
}

HRESULT RenderTarget::Init(Device& Device, IDXGISurface* Surface)
{
	HRESULT result = 0;
	if (Target != nullptr) return S_OK;

	result = Device.Context2D->CreateBitmapFromDxgiSurface(Surface, nullptr, &Target);
	if (FAILED(result)) return result;
	return result;
}

HRESULT __stdcall SwapChainBeginDraw(_In_ IDXGISwapChain* SwapChain, UINT Index, _Out_ Device** ppInstance)
{
	HRESULT result = 0;
	ComPtr<IDXGIDevice> DeviceDXGI;
	ComPtr<IDXGISurface> SurfaceDXGI;

	result = SwapChain->GetDevice(IID_PPV_ARGS(&DeviceDXGI));
	if (FAILED(result)) return result;
	result = SwapChain->GetBuffer(Index, IID_PPV_ARGS(&SurfaceDXGI));
	if (FAILED(result)) { // doesn't implement IDXGISurface, might be a stereo buffer
		ComPtr<IDXGIResource1> ResourceDXGI;
		result = SwapChain->GetBuffer(Index, IID_PPV_ARGS(&ResourceDXGI));
		if (FAILED(result)) return result; // doesn't implement IDXGIResource1 either, might be created with D3D12, or a very low DirectX version
		else {
			// retrive left and right buffer from stereo buffer
			ComPtr<IDXGISurface2> Left, Right;
			result = ResourceDXGI->CreateSubresourceSurface(0, &Left);
			if (FAILED(result)) return result;
			result = ResourceDXGI->CreateSubresourceSurface(1, &Right);
			if (FAILED(result)) return result;
			return StereoSurfaceBeginDraw(Left.Get(), Right.Get(), ppInstance);
		}
	}

	return SurfaceBeginDraw(SurfaceDXGI.Get(), ppInstance);
}

HRESULT __stdcall SurfaceBeginDraw(_In_ IDXGISurface* Surface, _Out_ Device** ppInstance)
{
	HRESULT result = 0;
	ComPtr<IDXGIDevice> DeviceDXGI;

	result = Surface->GetDevice(IID_PPV_ARGS(&DeviceDXGI));
	if (FAILED(result)) return result;
	result = CreateRenderTargetFromSurface(Surface);
	if (FAILED(result)) return result;

	*ppInstance = &DeviceResources[DeviceDXGI];
	ComPtr<ID2D1DeviceContext> Context = (*ppInstance)->Context2D;

	/*
	ComPtr<ID2D1Factory> Factory;
	ComPtr<ID2D1Multithread> Multithread;
	Context->GetFactory(&Factory);
	result = Factory.As(&Multithread);
	if (FAILED(result)) return result;
	Multithread->Enter();
	*/

	Context->BeginDraw();
	Context->SetTarget(RenderTargets[Surface].Target.Get());

	return result;
}

HRESULT __stdcall StereoSurfaceBeginDraw(_In_ IDXGISurface* SurfaceLeft, _In_ IDXGISurface* SurfaceRight, _Out_ Device** ppInstance)
{
	HRESULT result = 0;
	ComPtr<IDXGIDevice> DeviceDXGI;

	result = SurfaceLeft->GetDevice(IID_PPV_ARGS(&DeviceDXGI));
	if (FAILED(result)) return result;
	result = CreateRenderTargetFromSurface(SurfaceLeft);
	if (FAILED(result)) return result;
	result = CreateRenderTargetFromSurface(SurfaceRight);
	if (FAILED(result)) return result;

	*ppInstance = &DeviceResources[DeviceDXGI];
	ComPtr<ID2D1DeviceContext> Context = (*ppInstance)->Context2D;

	ComPtr<ID2D1Factory> Factory;
	ComPtr<ID2D1Multithread> Multithread;
	ComPtr<ID2D1CommandList> CommandList;

	/*
	Context->GetFactory(&Factory);
	result = Factory.As(&Multithread);
	if (FAILED(result)) return result;
	Multithread->Enter();
	*/

	result = Context->CreateCommandList(&CommandList);
	if (FAILED(result)) return result;
	CommandLists[SurfaceLeft] = CommandList;

	Context->BeginDraw();
	Context->SetTarget(CommandList.Get());

	return result;
}

HRESULT __stdcall SwapChainEndDraw(_In_ IDXGISwapChain* SwapChain, UINT Index, _In_ Device* pInstance)
{
	HRESULT result = 0;
	ComPtr<IDXGIDevice> DeviceDXGI;
	ComPtr<IDXGISurface> SurfaceDXGI;

	result = SwapChain->GetDevice(IID_PPV_ARGS(&DeviceDXGI));
	if (FAILED(result)) return result;
	result = SwapChain->GetBuffer(Index, IID_PPV_ARGS(&SurfaceDXGI));
	if (FAILED(result)) { // doesn't implement IDXGISurface, might be a stereo buffer
		ComPtr<IDXGIResource1> ResourceDXGI;
		result = SwapChain->GetBuffer(Index, IID_PPV_ARGS(&ResourceDXGI));
		if (FAILED(result)) return result; // doesn't implement IDXGIResource1 either, might be created with D3D12, or a very low DirectX version
		else {
			// retrive left and right buffer from stereo buffer
			ComPtr<IDXGISurface2> Left, Right;
			result = ResourceDXGI->CreateSubresourceSurface(0, &Left);
			if (FAILED(result)) return result;
			result = ResourceDXGI->CreateSubresourceSurface(1, &Right);
			if (FAILED(result)) return result;
			return StereoSurfaceEndDraw(Left.Get(), Right.Get(), pInstance);
		}
	}

	return SurfaceEndDraw(SurfaceDXGI.Get(), pInstance);
}

HRESULT __stdcall SurfaceEndDraw(_In_ IDXGISurface* Surface, _In_ Device* pInstance)
{
	HRESULT result = 0;

	ComPtr<ID2D1DeviceContext> Context = pInstance->Context2D;

	Context->SetTarget(nullptr);
	result = Context->EndDraw();
	if (FAILED(result)) return result;

	/*
	ComPtr<ID2D1Factory> Factory;
	ComPtr<ID2D1Multithread> Multithread;
	Context->GetFactory(&Factory);
	result = Factory.As(&Multithread);
	if (FAILED(result)) return result;
	Multithread->Leave();
	*/

	return result;
}

HRESULT __stdcall StereoSurfaceEndDraw(_In_ IDXGISurface* SurfaceLeft, _In_ IDXGISurface* SurfaceRight, _In_ Device* pInstance)
{
	HRESULT result = 0;

	ComPtr<ID2D1DeviceContext> Context = pInstance->Context2D;

	Context->SetTarget(nullptr);
	result = Context->EndDraw();
	if (FAILED(result)) return result;

	Context->BeginDraw();

	ComPtr<ID2D1Bitmap1> Left = RenderTargets[SurfaceLeft].Target;
	ComPtr<ID2D1Bitmap1> Right = RenderTargets[SurfaceRight].Target;
	ComPtr<ID2D1CommandList> CommandList = CommandLists[SurfaceLeft];
	CommandLists.erase(SurfaceLeft);
	result = CommandList->Close();
	if (FAILED(result)) return result;
	Context->SetTarget(Left.Get());
	Context->DrawImage(CommandList.Get());
	Context->SetTarget(Right.Get());
	Context->DrawImage(CommandList.Get());
	Context->SetTarget(nullptr);

	result = Context->EndDraw();
	if (FAILED(result)) return result;

	/*
	ComPtr<ID2D1Factory> Factory;
	ComPtr<ID2D1Multithread> Multithread;
	Context->GetFactory(&Factory);
	result = Factory.As(&Multithread);
	if (FAILED(result)) return result;
	Multithread->Leave();
	*/

	return result;
}

HRESULT __stdcall SetColor(_In_ Device* pInstance, D2D1_COLOR_F Color)
{
	if (pInstance->SolidColorBrush == nullptr) {
		return pInstance->Context2D->CreateSolidColorBrush(Color, &pInstance->SolidColorBrush);
	}
	else {
		pInstance->SolidColorBrush->SetColor(Color);
		return S_OK;
	}
}

HRESULT __stdcall SetOpacity(_In_ Device* pInstance, float Opacity)
{
	if (pInstance->SolidColorBrush != nullptr) {
		pInstance->SolidColorBrush->SetOpacity(Opacity);
		return S_OK;
	}
	return E_POINTER;
}

HRESULT __stdcall SetFont(_In_ Device* pInstance,
	LPCWSTR FontFamily, DWRITE_FONT_WEIGHT FontWeight,
	DWRITE_FONT_STYLE FontStyle, DWRITE_FONT_STRETCH FontStretch,
	float FontSize, LPCWSTR FontLocale)
{
	return DWriteFactory->CreateTextFormat(FontFamily, nullptr, FontWeight,
		FontStyle, FontStretch, FontSize, FontLocale, &pInstance->DWriteTextFormat);
}

HRESULT __stdcall SetFontParams(_In_ Device* pInstance,
	DWRITE_TEXT_ALIGNMENT TextAlignment, DWRITE_PARAGRAPH_ALIGNMENT ParagraphAlignment, DWRITE_WORD_WRAPPING WordWrapping,
	DWRITE_READING_DIRECTION ReadingDirection, DWRITE_FLOW_DIRECTION FlowDirection, FLOAT IncrementalTabStop,
	DWRITE_LINE_SPACING_METHOD LineSpacingMethod, FLOAT LineSpacing, FLOAT Baseline)
{
	HRESULT result = 0;
	if (pInstance->DWriteTextFormat != nullptr) {
		if (TextAlignment != -1) {
			result = pInstance->DWriteTextFormat->SetTextAlignment(TextAlignment);
			if (FAILED(result)) return result;
		}
		if (ParagraphAlignment != -1) {
			result = pInstance->DWriteTextFormat->SetParagraphAlignment(ParagraphAlignment);
			if (FAILED(result)) return result;
		}
		if (WordWrapping != -1) {
			result = pInstance->DWriteTextFormat->SetWordWrapping(WordWrapping);
			if (FAILED(result)) return result;
		}
		if (ReadingDirection != -1) {
			result = pInstance->DWriteTextFormat->SetReadingDirection(ReadingDirection);
			if (FAILED(result)) return result;
		}
		if (FlowDirection != -1) {
			result = pInstance->DWriteTextFormat->SetFlowDirection(FlowDirection);
			if (FAILED(result)) return result;
		}
		if (IncrementalTabStop != -1) {
			result = pInstance->DWriteTextFormat->SetIncrementalTabStop(IncrementalTabStop);
			if (FAILED(result)) return result;
		}
		if (LineSpacingMethod != -1 || !isnan(LineSpacing) || !isnan(Baseline))
		{
			if (LineSpacingMethod != -1) {
				float _;
				pInstance->DWriteTextFormat->GetLineSpacing(&LineSpacingMethod, &_, &_);
			}
			if (isnan(LineSpacing)) {
				float _;
				DWRITE_LINE_SPACING_METHOD discard;
				pInstance->DWriteTextFormat->GetLineSpacing(&discard, &LineSpacing, &_);
			}
			if (isnan(Baseline)) {
				float _;
				DWRITE_LINE_SPACING_METHOD discard;
				pInstance->DWriteTextFormat->GetLineSpacing(&discard, &_, &Baseline);
			}
			result = pInstance->DWriteTextFormat->SetLineSpacing(LineSpacingMethod, LineSpacing, Baseline);
			if (FAILED(result)) return result;
		}
		return S_OK;
	}
	return E_POINTER;
}

HRESULT __stdcall SetGDICompatibleText(_In_ Device* pInstance, LPCWSTR Str, float LayoutWidth, float LayoutHeight, float PixelsPerDip)
{
	HRESULT result = 0;
	if (pInstance->DWriteTextFormat != nullptr) {
		result = DWriteFactory->CreateGdiCompatibleTextLayout(Str, static_cast<UINT32>(lstrlenW(Str)),
			pInstance->DWriteTextFormat.Get(), LayoutWidth, LayoutHeight, PixelsPerDip, nullptr, FALSE, &pInstance->DWriteLayoutGDICompatible);
		if (FAILED(result)) return result;
		return S_OK;
	}
	else return E_POINTER;
}

void __stdcall DrawEllipse(_In_ Device* pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY, float StrokeWidth)
{
	pInstance->Context2D->DrawEllipse(D2D1_ELLIPSE{
		.point = Point, .radiusX = RadiusX, .radiusY = RadiusY },
		pInstance->SolidColorBrush.Get(), StrokeWidth);
}

void __stdcall DrawLine(_In_ Device* pInstance, D2D1_POINT_2F Src, D2D1_POINT_2F Dst, float StrokeWidth)
{
	pInstance->Context2D->DrawLine(Src, Dst, pInstance->SolidColorBrush.Get(), StrokeWidth);
}

void __stdcall DrawRectangle(_In_ Device* pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY, float StrokeWidth)
{
	if (RoundedRadiusX == 0.0f && RoundedRadiusY == 0.0f) {
		pInstance->Context2D->DrawRectangle(Rect, pInstance->SolidColorBrush.Get(), StrokeWidth);
	}
	else {
		pInstance->Context2D->DrawRoundedRectangle(D2D1_ROUNDED_RECT{
			.rect = Rect, .radiusX = RoundedRadiusY, .radiusY = RoundedRadiusY },
			pInstance->SolidColorBrush.Get(), StrokeWidth);
	}
}

void __stdcall DrawPlainText(_In_ Device* pInstance, LPCWSTR Str, D2D1_RECT_F Rect)
{
	pInstance->Context2D->DrawText(Str, static_cast<UINT32>(lstrlenW(Str)),
		pInstance->DWriteTextFormat.Get(), Rect,
		pInstance->SolidColorBrush.Get(),
		D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT, DWRITE_MEASURING_MODE_NATURAL);
}

HRESULT __stdcall DrawGDICompatibleText(_In_ Device* pInstance, D2D1_POINT_2F Origin)
{
	HRESULT result = 0;
	if (pInstance->DWriteTextFormat != nullptr && pInstance->DWriteLayoutGDICompatible != nullptr) {
		pInstance->Context2D->DrawTextLayout(Origin, pInstance->DWriteLayoutGDICompatible.Get(), pInstance->SolidColorBrush.Get(), D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
		return S_OK;
	}
	else return E_POINTER;
}

HRESULT __stdcall DrawGDICompatibleTextMetrics(_In_ Device* pInstance, UINT Index, UINT Length, float OriginX, float OriginY)
{
	HRESULT result = 0;
	UINT32 MetricsCount;
	if (pInstance->DWriteLayoutGDICompatible->HitTestTextRange(Index, Length, OriginX, OriginY,
		pInstance->DWriteTextMetrics.data(), static_cast<UINT32>(pInstance->DWriteTextMetrics.size()), &MetricsCount) == E_NOT_SUFFICIENT_BUFFER) {
		pInstance->DWriteTextMetrics.resize(MetricsCount);
		result = pInstance->DWriteLayoutGDICompatible->HitTestTextRange(Index, Length, OriginX, OriginY,
			pInstance->DWriteTextMetrics.data(), static_cast<UINT32>(pInstance->DWriteTextMetrics.size()), &MetricsCount);
		if (FAILED(result)) return result;
	}
	for (size_t n = 0; n < pInstance->DWriteTextMetrics.size(); n++) {
		auto& Current = pInstance->DWriteTextMetrics[n];
		FillRectangle(pInstance, D2D1_RECT_F{
			.left = Current.left, .top = Current.top,
			.right = Current.left + Current.width,
			.bottom = Current.top + Current.height}, 0.0f, 0.0f);
	}
	return result;
}

HRESULT __stdcall DrawGDICompatibleTextCaret(_In_ Device* pInstance, bool Trailing, float StrokeWidth)
{
	if (pInstance->DWriteTextMetrics.empty()) return E_NOT_SET;
	if (Trailing) {
		auto& Last = pInstance->DWriteTextMetrics.back();
		DrawLine(pInstance,
			D2D1_POINT_2F{ .x = Last.left + Last.width, .y = Last.top },
			D2D1_POINT_2F{ .x = Last.left + Last.width, .y = Last.top + Last.height },
			StrokeWidth);
	}
	else {
		auto& First = pInstance->DWriteTextMetrics.front();
		DrawLine(pInstance,
			D2D1_POINT_2F{ .x = First.left, .y = First.top },
			D2D1_POINT_2F{ .x = First.left, .y = First.top + First.height },
			StrokeWidth);
	}
	return S_OK;
}

void __stdcall FillEllipse(_In_ Device* pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY)
{
	pInstance->Context2D->FillEllipse(D2D1_ELLIPSE{
		.point = Point, .radiusX = RadiusX, .radiusY = RadiusY },
		pInstance->SolidColorBrush.Get());
}

void __stdcall FillRectangle(_In_ Device* pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY)
{
	if (RoundedRadiusX == 0.0f && RoundedRadiusY == 0.0f) {
		pInstance->Context2D->FillRectangle(Rect, pInstance->SolidColorBrush.Get());
	}
	else {
		pInstance->Context2D->FillRoundedRectangle(D2D1_ROUNDED_RECT{
			.rect = Rect, .radiusX = RoundedRadiusY, .radiusY = RoundedRadiusY },
			pInstance->SolidColorBrush.Get());
	}
}

void __stdcall SetTransform(_In_ Device* pInstance, Matrix3x2F Matrix)
{
	pInstance->Context2D->SetTransform(Matrix);
}

void __stdcall SetDpi(_In_ Device* pInstance, float DpiX, float DpiY)
{
	pInstance->Context2D->SetDpi(DpiX, DpiY);
}
