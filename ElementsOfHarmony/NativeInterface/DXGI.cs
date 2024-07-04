using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace ElementsOfHarmony.NativeInterface
{
    public static class DXGI
	{
		public const int IDXGIFactory_CreateSwapChain_VTableIndex = 10;
		public const int IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex = 15;
		public const int IDXGISwapChain_GetDevice_VTableIndex = 7;
		public const int IDXGISwapChain_Present_VTableIndex = 8;
		public const int IDXGISwapChain_GetBuffer_VTableIndex = 9;
		public const int IDXGISwapChain_ResizeBuffers_VTableIndex = 13;
		public const int IDXGISwapChain_ResizeTarget_VTableIndex = 14;
		public const int IDXGISwapChain1_Present1_VTableIndex = 22;
		public const int IDXGISwapChain3_SetColorSpace1_VTableIndex = 38;
		public const int IDXGISwapChain4_SetHDRMetaData_VTableIndex = 40;
		public static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
		public static readonly Guid IID_IDXGIFactory = new Guid("7b7166ec-21c7-44ae-b21a-c9ae321ae369");
		public static readonly Guid IID_IDXGIFactory2 = new Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
		public static readonly Guid IID_IDXGISwapChain = new Guid("310d36a0-d2e7-4c0a-aa04-6a9d23b8886a");
		public static readonly Guid IID_IDXGISwapChain1 = new Guid("790a45f7-0d42-4876-983a-0a55cfe6f4aa");
		public static readonly Guid IID_IDXGISwapChain3 = new Guid("94D99BDB-F1F8-4AB0-B236-7DA0170EDAB1");
		public static readonly Guid IID_IDXGISwapChain4 = new Guid("3D585D5A-BD4A-489E-B1F4-3DBCB6452FFB");

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int IDXGISwapChain_GetBuffer_Proc(IntPtr pInstance, uint Buffer, Guid riid, IntPtr* ppSurface);
        
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int IDXGISwapChain3_SetColorSpace1_Proc(IntPtr pInstance, DXGI_COLOR_SPACE_TYPE ColorSpace);

		public static bool CanCastTo(this DXGI_FORMAT From, DXGI_FORMAT To)
        {
            return FormatCastingMap.TryGetValue(From, out SortedSet<DXGI_FORMAT> Compatible) && Compatible.Contains(To);
		}
        public static bool CanCastFrom(this DXGI_FORMAT To, DXGI_FORMAT From)
		{
			return FormatCastingMap.TryGetValue(From, out SortedSet<DXGI_FORMAT> Compatible) && Compatible.Contains(To);
		}

		public enum DXGI_FORMAT : uint
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,

            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,

            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,

            DXGI_FORMAT_A4B4G4R4_UNORM = 191,
        }

		/// <summary>
		/// this map is generated by ChatGPT,<br/>
		/// but I changed `Dictionary&lt;DXGI_FORMAT, List&lt;DXGI_FORMAT&gt;&gt;`<br/>
		/// to `SortedDictionary&lt;DXGI_FORMAT, SortedSet&lt;DXGI_FORMAT&gt;&gt;`,<br/>
		/// because why use hash when it's already a int type?
		/// </summary>
		private static readonly SortedDictionary<DXGI_FORMAT, SortedSet<DXGI_FORMAT>> FormatCastingMap = new SortedDictionary<DXGI_FORMAT, SortedSet<DXGI_FORMAT>>
        {
            { DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT, DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT, DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R32G32_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT, DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT, DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS, DXGI_FORMAT.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM, DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R16G16_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM, DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT, DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM, DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT, DXGI_FORMAT.DXGI_FORMAT_R32_UINT, DXGI_FORMAT.DXGI_FORMAT_R32_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT, DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS, DXGI_FORMAT.DXGI_FORMAT_X24_TYPELESS_G8_UINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R8G8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM, DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT, DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM, DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT, DXGI_FORMAT.DXGI_FORMAT_D16_UNORM, DXGI_FORMAT.DXGI_FORMAT_R16_UNORM, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, DXGI_FORMAT.DXGI_FORMAT_R16_SNORM, DXGI_FORMAT.DXGI_FORMAT_R16_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R8_UNORM, DXGI_FORMAT.DXGI_FORMAT_R8_UINT, DXGI_FORMAT.DXGI_FORMAT_R8_SNORM, DXGI_FORMAT.DXGI_FORMAT_R8_SINT } },
            { DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM } },
            { DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM } },
            { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM, DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16, DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16 } },
            { DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM, DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB } },
            { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, new SortedSet<DXGI_FORMAT> { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB } }
        };

		public enum DXGI_MODE_SCANLINE_ORDER : uint
        {
            DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED = 0,
            DXGI_MODE_SCANLINE_ORDER_PROGRESSIVE = 1,
            DXGI_MODE_SCANLINE_ORDER_UPPER_FIELD_FIRST = 2,
            DXGI_MODE_SCANLINE_ORDER_LOWER_FIELD_FIRST = 3
        }

        public enum DXGI_MODE_SCALING : uint
        {
            DXGI_MODE_SCALING_UNSPECIFIED = 0,
            DXGI_MODE_SCALING_CENTERED = 1,
            DXGI_MODE_SCALING_STRETCHED = 2
        }

        [Flags]
        public enum DXGI_USAGE : uint
        {
            DXGI_USAGE_SHADER_INPUT = 0x00000010,
            DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020,
            DXGI_USAGE_BACK_BUFFER = 0x00000040,
            DXGI_USAGE_SHARED = 0x00000080,
            DXGI_USAGE_READ_ONLY = 0x00000100,
            DXGI_USAGE_DISCARD_ON_PRESENT = 0x00000200,
            DXGI_USAGE_UNORDERED_ACCESS = 0x00000400
        }

        public enum DXGI_SWAP_EFFECT : uint
        {
            DXGI_SWAP_EFFECT_DISCARD = 0,
            DXGI_SWAP_EFFECT_SEQUENTIAL = 1,
            DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL = 3,
            DXGI_SWAP_EFFECT_FLIP_DISCARD = 4
        }

        [Flags]
        public enum DXGI_SWAP_CHAIN_FLAG : uint
        {
            DXGI_SWAP_CHAIN_FLAG_NONPREROTATED = 0x1,
            DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH = 0x2,
            DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE = 0x4,
            DXGI_SWAP_CHAIN_FLAG_RESTRICTED_CONTENT = 0x8,
            DXGI_SWAP_CHAIN_FLAG_RESTRICT_SHARED_RESOURCE_DRIVER = 0x10,
            DXGI_SWAP_CHAIN_FLAG_DISPLAY_ONLY = 0x20,
            DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT = 0x40,
            DXGI_SWAP_CHAIN_FLAG_FOREGROUND_LAYER = 0x80,
            DXGI_SWAP_CHAIN_FLAG_FULLSCREEN_VIDEO = 0x100,
            DXGI_SWAP_CHAIN_FLAG_YUV_VIDEO = 0x200,
            DXGI_SWAP_CHAIN_FLAG_HW_PROTECTED = 0x400,
            DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING = 0x800,
            DXGI_SWAP_CHAIN_FLAG_RESTRICTED_TO_ALL_HOLOGRAPHIC_DISPLAYS = 0x1000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_MODE_DESC
        {
            public uint Width;
            public uint Height;
            public DXGI_RATIONAL RefreshRate;
            public DXGI_FORMAT Format;
            public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
            public DXGI_MODE_SCALING Scaling;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_SAMPLE_DESC
        {
            public uint Count;
            public uint Quality;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_SWAP_CHAIN_DESC
        {
            public DXGI_MODE_DESC BufferDesc;
            public DXGI_SAMPLE_DESC SampleDesc;
            public DXGI_USAGE BufferUsage;
            public uint BufferCount;
            public IntPtr OutputWindow;
            [MarshalAs(UnmanagedType.Bool)]
            public bool Windowed;
            public DXGI_SWAP_EFFECT SwapEffect;
            public uint Flags;
        }

        public enum DXGI_SCALING : int
        {
            DXGI_SCALING_STRETCH = 0,
            DXGI_SCALING_NONE = 1,
            DXGI_SCALING_ASPECT_RATIO_STRETCH = 2
        }

        public enum DXGI_ALPHA_MODE : int
        {
            DXGI_ALPHA_MODE_UNSPECIFIED = 0,
            DXGI_ALPHA_MODE_PREMULTIPLIED = 1,
            DXGI_ALPHA_MODE_STRAIGHT = 2,
            DXGI_ALPHA_MODE_IGNORE = 3,
            DXGI_ALPHA_MODE_FORCE_DWORD = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_SWAP_CHAIN_DESC1
        {
            public uint Width;
            public uint Height;
            public DXGI_FORMAT Format;
            [MarshalAs(UnmanagedType.Bool)]
            public bool Stereo;
            public DXGI_SAMPLE_DESC SampleDesc;
            public DXGI_USAGE BufferUsage;
            public uint BufferCount;
            public DXGI_SCALING Scaling;
            public DXGI_SWAP_EFFECT SwapEffect;
            public DXGI_ALPHA_MODE AlphaMode;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_SWAP_CHAIN_FULLSCREEN_DESC
        {
            public DXGI_RATIONAL RefreshRate;
            public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
            public DXGI_MODE_SCALING Scaling;
            [MarshalAs(UnmanagedType.Bool)]
            public bool Windowed;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width
            {
                readonly get => Right - Left;
                set => Right = Left + value;
            }
            public int Height
            {
                readonly get => Bottom - Top;
                set => Bottom = Top + value;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_PRESENT_PARAMETERS
        {
            public uint DirtyRectsCount;
            public IntPtr pDirtyRects;
            public IntPtr pScrollRect;
            public IntPtr pScrollOffset;
            public NativeArrayAccess<RECT>? DirtyRects
            {
                readonly get
                {
                    if (pDirtyRects != IntPtr.Zero)
                    {
                        return new NativeArrayAccess<RECT>(pDirtyRects, (int)DirtyRectsCount);
                    }
                    else
                    {
                        return null;
                    }
                }
                set
                {
                    if (value == null)
                    {
                        DirtyRectsCount = 0;
                        pDirtyRects = IntPtr.Zero;
                    }
                    else
                    {
                        DirtyRectsCount = (uint)value.Length;
                        pDirtyRects = value.FixedAddress;
                    }
                }
            }
            public RECT? ScrollRect
            {
                readonly get
                {
                    if (pScrollRect != IntPtr.Zero)
                    {
                        return Marshal.PtrToStructure<RECT>(pScrollRect);
                    }
                    else
                    {
                        return null;
                    }
                }
                set
                {
                    if (value == null) pScrollRect = IntPtr.Zero;
                    else if (pScrollRect == IntPtr.Zero) throw new NullReferenceException();
                    else Marshal.StructureToPtr(value.Value, pScrollRect, false);
                }
            }
            public POINT? ScrollOffset
            {
                readonly get
                {
                    if (pScrollRect != IntPtr.Zero)
                    {
                        return Marshal.PtrToStructure<POINT>(pScrollRect);
                    }
                    else
                    {
                        return null;
                    }
                }
                set
                {
                    if (value == null) pScrollOffset = IntPtr.Zero;
                    else if (pScrollOffset == IntPtr.Zero) throw new NullReferenceException();
                    else Marshal.StructureToPtr(value.Value, pScrollOffset, false);
                }
            }
        }

		public enum DXGI_COLOR_SPACE_TYPE
		{
			DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709 = 0,
			DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709 = 1,
			DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P709 = 2,
			DXGI_COLOR_SPACE_RGB_STUDIO_G22_NONE_P2020 = 3,
			DXGI_COLOR_SPACE_RESERVED = 4,
			DXGI_COLOR_SPACE_YCBCR_FULL_G22_NONE_P709_X601 = 5,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P601 = 6,
			DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P601 = 7,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P709 = 8,
			DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P709 = 9,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_LEFT_P2020 = 10,
			DXGI_COLOR_SPACE_YCBCR_FULL_G22_LEFT_P2020 = 11,
			DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020 = 12,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_LEFT_P2020 = 13,
			DXGI_COLOR_SPACE_RGB_STUDIO_G2084_NONE_P2020 = 14,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G22_TOPLEFT_P2020 = 15,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G2084_TOPLEFT_P2020 = 16,
			DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P2020 = 17,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_GHLG_TOPLEFT_P2020 = 18,
			DXGI_COLOR_SPACE_YCBCR_FULL_GHLG_TOPLEFT_P2020 = 19,
			DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P709 = 20,
			DXGI_COLOR_SPACE_RGB_STUDIO_G24_NONE_P2020 = 21,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P709 = 22,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_LEFT_P2020 = 23,
			DXGI_COLOR_SPACE_YCBCR_STUDIO_G24_TOPLEFT_P2020 = 24,
			DXGI_COLOR_SPACE_CUSTOM = -1,
		}

		public enum DXGI_HDR_METADATA_TYPE
		{
			DXGI_HDR_METADATA_TYPE_NONE = 0,
			DXGI_HDR_METADATA_TYPE_HDR10 = 1,
			DXGI_HDR_METADATA_TYPE_HDR10PLUS = 2
		}

        [StructLayout(LayoutKind.Sequential)]
		public unsafe struct DXGI_HDR_METADATA_HDR10
		{
			public fixed ushort RedPrimary[2];
			public fixed ushort GreenPrimary[2];
			public fixed ushort BluePrimary[2];
			public fixed ushort WhitePoint[2];
			public uint MaxMasteringLuminance;
			public uint MinMasteringLuminance;
			public ushort MaxContentLightLevel;
			public ushort MaxFrameAverageLightLevel;
		}

        [StructLayout(LayoutKind.Sequential)]
		public unsafe struct DXGI_HDR_METADATA_HDR10PLUS
		{
			public fixed byte Data[72];
		}
	}
}
