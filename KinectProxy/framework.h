#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "C:/Program Files/Microsoft SDKs/Kinect/v2.0_1409/inc/Kinect.h"
#pragma comment(lib, "C:/Program Files/Microsoft SDKs/Kinect/v2.0_1409/Lib/x64/Kinect20.lib")

#include <wrl.h>
using namespace Microsoft::WRL;

#include <DirectXMath.h>
using namespace DirectX;

#include <array>
using namespace std;