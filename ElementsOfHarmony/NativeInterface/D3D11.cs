using static ElementsOfHarmony.NativeInterface.DXGI;
using System.Runtime.InteropServices;
using System;

namespace ElementsOfHarmony.NativeInterface
{
    public static class D3D11
	{
		public const int ID3D11Device_CreateShaderResourceView_VTableIndex = 7;
		public const int ID3D11Device_CreateRenderTargetView_VTableIndex = 9;
		public const int ID3D11Device_CreatePixelShader_VTableIndex = 15;
		public const int ID3D11Device_GetImmediateContext_VTableIndex = 40;
		public const int ID3D11DeviceChild_GetDevice_VTableIndex = 3;
		public const int ID3D11DeviceContext_PSSetShader_VTableIndex = 9;
		public const int ID3D11DeviceContext_DrawIndexed_VTableIndex = 12;
		public const int ID3D11DeviceContext_Draw_VTableIndex = 13;
		public const int ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex = 16;
		public const int ID3D11DeviceContext_DrawIndexedInstanced_VTableIndex = 20;
		public const int ID3D11DeviceContext_DrawInstanced_VTableIndex = 21;
		public const int ID3D11DeviceContext_OMSetRenderTargets_VTableIndex = 33;
		public const int ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex = 34;
		public const int ID3D11DeviceContext_DrawAuto_VTableIndex = 38;
		public const int ID3D11DeviceContext_DrawIndexedInstancedIndirect_VTableIndex = 39;
		public const int ID3D11DeviceContext_DrawInstancedIndirect_VTableIndex = 40;
		public const int ID3D11DeviceContext_OMGetRenderTargets_VTableIndex = 89;
        public const int ID3D11Texture2D_GetDesc_VTableIndex = 10;
		public static readonly Guid ID3D11Device_IID = new Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140");
		public static readonly Guid ID3D11DeviceContext_IID = new Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da");
		public static readonly Guid ID3D11Texture2D_IID = new Guid("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11Device_GetImmediateContext_Proc(IntPtr pInstance, IntPtr* DeviceContext);
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11DeviceChild_GetDevice_Proc(IntPtr pInstance, IntPtr* Device);
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11DeviceContext_PSSetShader_Proc(IntPtr pInstance, IntPtr pPixelShader, IntPtr* ppClassInstances, uint NumClassInstances);
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11DeviceCOntext_PSSetConstantBuffers_Proc(IntPtr pInstance, uint StartSlot, uint NumBuffers, IntPtr* ppConstantBuffers);
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11DeviceCOntext_OMGetRenderTargets_Proc(IntPtr pInstance, uint NumViews, IntPtr* ppRenderTargetViews, IntPtr* ppDepthStencilView);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ID3D11Texture2D_GetDesc_Proc(IntPtr pInstance, D3D11_TEXTURE2D_DESC* pDesc);

		[StructLayout(LayoutKind.Sequential)]
        public unsafe struct D3D11_SHADER_RESOURCE_VIEW_DESC
		{
			public DXGI_FORMAT Format;
			public D3D11_SRV_DIMENSION ViewDimension;
			public Anonymous_Union Anonymous;

			[StructLayout(LayoutKind.Explicit)]
			public struct Anonymous_Union
			{
				[FieldOffset(0)]
				public D3D11_BUFFER_SRV Buffer;

				[FieldOffset(0)]
				public D3D11_TEX1D_SRV Texture1D;

				[FieldOffset(0)]
				public D3D11_TEX1D_ARRAY_SRV Texture1DArray;

				[FieldOffset(0)]
				public D3D11_TEX2D_SRV Texture2D;

				[FieldOffset(0)]
				public D3D11_TEX2D_ARRAY_SRV Texture2DArray;

				[FieldOffset(0)]
				public D3D11_TEX2DMS_SRV Texture2DMS;

				[FieldOffset(0)]
				public D3D11_TEX2DMS_ARRAY_SRV Texture2DMSArray;

				[FieldOffset(0)]
				public D3D11_TEX3D_SRV Texture3D;

				[FieldOffset(0)]
				public D3D11_TEXCUBE_SRV TextureCube;

				[FieldOffset(0)]
				public D3D11_TEXCUBE_ARRAY_SRV TextureCubeArray;

				[FieldOffset(0)]
				public D3D11_BUFFEREX_SRV BufferEx;
			}
		}

		public enum D3D11_SRV_DIMENSION
		{
			D3D11_SRV_DIMENSION_UNKNOWN = 0,
			D3D11_SRV_DIMENSION_BUFFER = 1,
			D3D11_SRV_DIMENSION_TEXTURE1D = 2,
			D3D11_SRV_DIMENSION_TEXTURE1DARRAY = 3,
			D3D11_SRV_DIMENSION_TEXTURE2D = 4,
			D3D11_SRV_DIMENSION_TEXTURE2DARRAY = 5,
			D3D11_SRV_DIMENSION_TEXTURE2DMS = 6,
			D3D11_SRV_DIMENSION_TEXTURE2DMSARRAY = 7,
			D3D11_SRV_DIMENSION_TEXTURE3D = 8,
			D3D11_SRV_DIMENSION_TEXTURECUBE = 9,
			D3D11_SRV_DIMENSION_TEXTURECUBEARRAY = 10,
			D3D11_SRV_DIMENSION_BUFFEREX = 11
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_BUFFER_SRV
		{
			public Anonymous1_Union Anonymous1;
			public Anonymous2_Union Anonymous2;

			[StructLayout(LayoutKind.Explicit)]
			public struct Anonymous1_Union
			{
				[FieldOffset(0)]
				public uint FirstElement;

				[FieldOffset(0)]
				public uint ElementOffset;
			}

			[StructLayout(LayoutKind.Explicit)]
			public struct Anonymous2_Union
			{
				[FieldOffset(0)]
				public uint NumElements;

				[FieldOffset(0)]
				public uint ElementWidth;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX1D_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX1D_ARRAY_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
			public uint FirstArraySlice;
			public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2D_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2D_ARRAY_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
			public uint FirstArraySlice;
			public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2DMS_SRV
		{
			public uint UnusedField_NothingToDefine;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2DMS_ARRAY_SRV
		{
			public uint FirstArraySlice;
			public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX3D_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEXCUBE_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEXCUBE_ARRAY_SRV
		{
			public uint MostDetailedMip;
			public uint MipLevels;
			public uint First2DArrayFace;
			public uint NumCubes;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_BUFFEREX_SRV
		{
			public uint FirstElement;
			public uint NumElements;
			public uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
        public unsafe struct D3D11_RENDER_TARGET_VIEW_DESC
        {
            public DXGI_FORMAT Format;
            public D3D11_RTV_DIMENSION ViewDimension;
            public Anonymous_Union Anonymous;

            [StructLayout(LayoutKind.Explicit)]
            public struct Anonymous_Union
            {
                [FieldOffset(0)]
                public D3D11_BUFFER_RTV Buffer;

                [FieldOffset(0)]
                public D3D11_TEX1D_RTV Texture1D;

                [FieldOffset(0)]
                public D3D11_TEX1D_ARRAY_RTV Texture1DArray;

                [FieldOffset(0)]
                public D3D11_TEX2D_RTV Texture2D;

                [FieldOffset(0)]
                public D3D11_TEX2D_ARRAY_RTV Texture2DArray;

                [FieldOffset(0)]
                public D3D11_TEX2DMS_RTV Texture2DMS;

                [FieldOffset(0)]
                public D3D11_TEX2DMS_ARRAY_RTV Texture2DMSArray;

                [FieldOffset(0)]
                public D3D11_TEX3D_RTV Texture3D;
            }
        }

        public enum D3D11_RTV_DIMENSION
        {
            D3D11_RTV_DIMENSION_UNKNOWN = 0,
            D3D11_RTV_DIMENSION_BUFFER = 1,
            D3D11_RTV_DIMENSION_TEXTURE1D = 2,
            D3D11_RTV_DIMENSION_TEXTURE1DARRAY = 3,
            D3D11_RTV_DIMENSION_TEXTURE2D = 4,
            D3D11_RTV_DIMENSION_TEXTURE2DARRAY = 5,
            D3D11_RTV_DIMENSION_TEXTURE2DMS = 6,
            D3D11_RTV_DIMENSION_TEXTURE2DMSARRAY = 7,
            D3D11_RTV_DIMENSION_TEXTURE3D = 8
        }

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_BUFFER_RTV
        {
            public Anonymous1_Union Anonymous1;
            public Anonymous2_Union Anonymous2;

            [StructLayout(LayoutKind.Explicit)]
            public struct Anonymous1_Union
            {
                [FieldOffset(0)]
                public uint FirstElement;

                [FieldOffset(0)]
                public uint ElementOffset;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct Anonymous2_Union
            {
                [FieldOffset(0)]
                public uint NumElements;

                [FieldOffset(0)]
                public uint ElementWidth;
            }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX1D_RTV
        {
            public uint MipSlice;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX1D_ARRAY_RTV
        {
            public uint MipSlice;
            public uint FirstArraySlice;
            public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2D_RTV
        {
            public uint MipSlice;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2D_ARRAY_RTV
        {
            public uint MipSlice;
            public uint FirstArraySlice;
            public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2DMS_RTV
        {
            public uint UnusedField_NothingToDefine;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX2DMS_ARRAY_RTV
        {
            public uint FirstArraySlice;
            public uint ArraySize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEX3D_RTV
        {
            public uint MipSlice;
            public uint FirstWSlice;
            public uint WSize;
        }

		public enum D3D11_USAGE
		{
			D3D11_USAGE_DEFAULT,
			D3D11_USAGE_IMMUTABLE,
			D3D11_USAGE_DYNAMIC,
			D3D11_USAGE_STAGING
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEXTURE2D_DESC
		{
			public uint Width;
			public uint Height;
			public uint MipLevels;
			public uint ArraySize;
			public DXGI_FORMAT Format;
			public DXGI_SAMPLE_DESC SampleDesc;
			public D3D11_USAGE Usage;
			public uint BindFlags;
			public uint CPUAccessFlags;
			public uint MiscFlags;
		}
	}
}
