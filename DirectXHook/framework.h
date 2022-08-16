#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#pragma push_macro("DrawText")
#undef DrawText
#include <dxgi.h>
#include <dxgi1_2.h>
#include <d3d11.h>
#include <d2d1.h>
#include <d2d1_1.h>
#include <dwrite.h>
#pragma pop_macro("DrawText")

#pragma comment(lib, "DXGI.lib")
#pragma comment(lib, "D3D11.lib")
#pragma comment(lib, "D2D1.lib")
#pragma comment(lib, "D2D1.lib")
#pragma comment(lib, "DWrite.lib")

#include <wrl.h>
using namespace Microsoft::WRL;

#include <DirectXMath.h>
using namespace DirectX;
