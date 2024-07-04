#pragma once

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>

#pragma push_macro("DrawText")
#undef DrawText
#include <DXGI.h>
#include <DXGI1_2.h>
#include <DXGI1_3.h>
#include <DXGI1_4.h>
#include <DXGI1_5.h>
#include <DXGI1_6.h>
#include <D3D11.h>
#include <D3D11_1.h>
#include <D3D11_2.h>
#include <D3D11_3.h>
#include <D3D11_4.h>
#include <D2D1.h>
#include <D2D1_1.h>
#include <D2D1_2.h>
#include <D2D1_3.h>
#include <DWrite.h>
#include <D3DCompiler.h>
#pragma pop_macro("DrawText")

#include <wrl.h>
using Microsoft::WRL::ComPtr;

#include <PSApi.h> // process status api

#include <atomic>
#include <algorithm>
#include <map>
#include <optional>
#include <set>
#include <sstream>
#include <string>
#include <vector>

#pragma comment(lib, "DXGI.lib")
#pragma comment(lib, "D3D11.lib")
#pragma comment(lib, "D2D1.lib")
#pragma comment(lib, "D2D1.lib")
#pragma comment(lib, "DWrite.lib")
#pragma comment(lib, "D3DCompiler.lib")
#pragma comment(lib, "User32.lib")
#pragma comment(lib, "PSApi.lib")