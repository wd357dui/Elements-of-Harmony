#pragma once

struct Arguments;

typedef void(__stdcall* CallbackProc)(Arguments* Args);
typedef void(__stdcall* LogCallbackProc)(LPCWSTR Message);

constexpr size_t IDXGIFactory_CreateSwapChain_VTableIndex = 10;
constexpr size_t IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex = 15;
constexpr size_t IDXGISwapChain_Present_VTableIndex = 8;
constexpr size_t IDXGISwapChain_ResizeBuffers_VTableIndex = 13;
constexpr size_t IDXGISwapChain_ResizeTarget_VTableIndex = 14;
constexpr size_t IDXGISwapChain1_Present1_VTableIndex = 22;
constexpr size_t ID3D11Device_CreateShaderResourceView_VTableIndex = 7;
constexpr size_t ID3D11Device_CreateRenderTargetView_VTableIndex = 9;
constexpr size_t ID3D11Device_CreatePixelShader_VTableIndex = 15;
constexpr size_t ID3D11DeviceContext_PSSetShader_VTableIndex = 9;
constexpr size_t ID3D11DeviceContext_DrawIndexed_VTableIndex = 12;
constexpr size_t ID3D11DeviceContext_Draw_VTableIndex = 13;
constexpr size_t ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex = 16;
constexpr size_t ID3D11DeviceContext_DrawIndexedInstanced_VTableIndex = 20;
constexpr size_t ID3D11DeviceContext_DrawInstanced_VTableIndex = 21;
constexpr size_t ID3D11DeviceContext_OMSetRenderTargets_VTableIndex = 33;
constexpr size_t ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex = 34;
constexpr size_t ID3D11DeviceContext_DrawAuto_VTableIndex = 38;
constexpr size_t ID3D11DeviceContext_DrawIndexedInstancedIndirect_VTableIndex = 39;
constexpr size_t ID3D11DeviceContext_DrawInstancedIndirect_VTableIndex = 40;
constexpr size_t ID3D11DeviceContext_OMGetRenderTargets_VTableIndex = 89;
constexpr size_t IDXGISwapChain3_SetColorSpace1_VTableIndex = 38;
constexpr size_t IDXGISwapChain4_SetHDRMetaData_VTableIndex = 40;

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
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain_ResizeBuffers_Proc)(IDXGISwapChain* This,
	UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain_ResizeTarget_Proc)(IDXGISwapChain* This,
	_In_ const DXGI_MODE_DESC* pNewTargetParameters);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain_Present_Proc)(IDXGISwapChain* This, UINT SyncInterval, UINT Flags);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain1_Present1_Proc)(IDXGISwapChain1* This, UINT SyncInterval, UINT Flags,
	_In_ const DXGI_PRESENT_PARAMETERS* pPresentParameters);
typedef HRESULT(STDMETHODCALLTYPE* ID3D11Device_CreateShaderResourceView_Proc)(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_SHADER_RESOURCE_VIEW_DESC* pDesc,
	ID3D11ShaderResourceView** ppRTView);
typedef HRESULT(STDMETHODCALLTYPE* ID3D11Device_CreateRenderTargetView_Proc)(ID3D11Device* This,
	_In_  ID3D11Resource* pResource,
	_In_opt_  const D3D11_RENDER_TARGET_VIEW_DESC* pDesc,
	ID3D11RenderTargetView** ppRTView);
typedef HRESULT(STDMETHODCALLTYPE* ID3D11Device_CreatePixelShader_Proc)(ID3D11Device* This,
	_In_reads_(BytecodeLength)  const void* pShaderBytecode, _In_  SIZE_T BytecodeLength,
	_In_opt_  ID3D11ClassLinkage* pClassLinkage,
	ID3D11PixelShader** ppPixelShader);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_PSSetShader_Proc)(ID3D11DeviceContext* This,
	_In_opt_  ID3D11PixelShader* pPixelShader,
	_In_reads_opt_(NumClassInstances)  ID3D11ClassInstance* const* ppClassInstances,
	UINT NumClassInstances);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawIndexed_Proc)(ID3D11DeviceContext* This,
	_In_  UINT IndexCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_Draw_Proc)(ID3D11DeviceContext* This,
	_In_  UINT VertexCount,
	_In_  UINT StartVertexLocation);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_PSSetConstantBuffers_Proc)(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1)  UINT StartSlot,
	_In_range_(0, D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - StartSlot)  UINT NumBuffers,
	_In_reads_opt_(NumBuffers)  ID3D11Buffer* const* ppConstantBuffers);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawIndexedInstanced_Proc)(ID3D11DeviceContext* This,
	_In_  UINT IndexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartIndexLocation,
	_In_  INT BaseVertexLocation,
	_In_  UINT StartInstanceLocation);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawInstanced_Proc)(ID3D11DeviceContext* This,
	_In_  UINT VertexCountPerInstance,
	_In_  UINT InstanceCount,
	_In_  UINT StartVertexLocation,
	_In_  UINT StartInstanceLocation);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_OMSetRenderTargets_Proc)(ID3D11DeviceContext* This,
	_In_range_(0, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)  UINT NumViews,
	_In_reads_opt_(NumViews)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_Proc)(ID3D11DeviceContext* This,
	_In_  UINT NumRTVs,
	_In_reads_opt_(NumRTVs)  ID3D11RenderTargetView* const* ppRenderTargetViews,
	_In_opt_  ID3D11DepthStencilView* pDepthStencilView,
	_In_range_(0, D3D11_1_UAV_SLOT_COUNT - 1)  UINT UAVStartSlot,
	_In_  UINT NumUAVs,
	_In_reads_opt_(NumUAVs)  ID3D11UnorderedAccessView* const* ppUnorderedAccessViews,
	_In_reads_opt_(NumUAVs)  const UINT* pUAVInitialCounts);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawAuto_Proc)(ID3D11DeviceContext* This);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawIndexedInstancedIndirect_Proc)(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs);
typedef void (STDMETHODCALLTYPE* ID3D11DeviceContext_DrawInstancedIndirect_Proc)(ID3D11DeviceContext* This,
	_In_  ID3D11Buffer* pBufferForArgs,
	_In_  UINT AlignedByteOffsetForArgs);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain3_SetColorSpace1_Proc)(IDXGISwapChain3* This,
	_In_  DXGI_COLOR_SPACE_TYPE ColorSpace);
typedef HRESULT(STDMETHODCALLTYPE* IDXGISwapChain4_SetHDRMetaData_Proc)(IDXGISwapChain4* This,
	_In_  DXGI_HDR_METADATA_TYPE Type,
	_In_  UINT Size,
	_In_reads_opt_(Size)  void* pMetaData);

struct Arguments
{
	static_assert(sizeof(void*) == sizeof(int64_t), "target platform (pointer size) must be the same (64-bit) between here and DirectXHook.cs");

	GUID IID; // uuid of the interface
	void* PPV; // pointer of the interface
	UINT VTableIndex; // vtable index of the function
	BOOL Stop = FALSE; // set this to true if you do not want the original function to be called
	BOOL Post = FALSE; // determines whether if this is called after the original function is called
	HRESULT Result = S_OK; // the HRESULT return value of the function, if Stop is TRUE, set this value to desired return value
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

	inline static Arguments PreResizeBuffers(IDXGISwapChain* This, UINT& BufferCount, UINT& Width, UINT& Height, DXGI_FORMAT& NewFormat, UINT& SwapChainFlags)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain_ResizeBuffers_VTableIndex;
		Result.Args[0] = &BufferCount;
		Result.Args[1] = &Width;
		Result.Args[2] = &Height;
		Result.Args[3] = &NewFormat;
		Result.Args[4] = &SwapChainFlags;
		return Result;
	}
	inline static void PostResizeBuffers(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreResizeTarget(IDXGISwapChain* This, DXGI_MODE_DESC& NewTargetParameters)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain_ResizeTarget_VTableIndex;
		Result.Args[0] = &NewTargetParameters;
		return Result;
	}
	inline static void PostResizeTarget(Arguments& Previous, HRESULT result)
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

	inline static Arguments PreCreateShaderResourceView(ID3D11Device* This,
		ID3D11Resource*& pResource, OptionalStruct<D3D11_SHADER_RESOURCE_VIEW_DESC>& Desc)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11Device);
		Result.PPV = This;
		Result.VTableIndex = ID3D11Device_CreateShaderResourceView_VTableIndex;
		Result.Args[0] = &pResource;
		Result.Args[1] = &Desc;
		return Result;
	}
	inline static void PostCreateShaderResourceView(Arguments& Previous, ID3D11ShaderResourceView** ppSRView, HRESULT result)
	{
		Previous.Args[2] = ppSRView;
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreCreateRenderTargetView(ID3D11Device* This,
		ID3D11Resource*& pResource, OptionalStruct<D3D11_RENDER_TARGET_VIEW_DESC>& Desc)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11Device);
		Result.PPV = This;
		Result.VTableIndex = ID3D11Device_CreateRenderTargetView_VTableIndex;
		Result.Args[0] = &pResource;
		Result.Args[1] = &Desc;
		return Result;
	}
	inline static void PostCreateRenderTargetView(Arguments& Previous, ID3D11RenderTargetView** ppRTView, HRESULT result)
	{
		Previous.Args[2] = ppRTView;
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreCreatePixelShader(ID3D11Device* This,
		const void*& pShaderBytecode, SIZE_T& BytecodeLength,
		ID3D11ClassLinkage*& pClassLinkage)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11Device);
		Result.PPV = This;
		Result.VTableIndex = ID3D11Device_CreatePixelShader_VTableIndex;
		Result.Args[0] = &pShaderBytecode;
		Result.Args[1] = &BytecodeLength;
		Result.Args[2] = &pClassLinkage;
		return Result;
	}
	inline static void PostCreatePixelShader(Arguments& Previous, ID3D11PixelShader** ppPixelShader, HRESULT result)
	{
		Previous.Args[3] = ppPixelShader;
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PrePSSetShader(ID3D11DeviceContext* This,
		ID3D11PixelShader*& pPixelShader, ID3D11ClassInstance**& ppClassInstances, UINT& NumClassInstances)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_PSSetShader_VTableIndex;
		Result.Args[0] = &pPixelShader;
		Result.Args[1] = &ppClassInstances;
		Result.Args[2] = &NumClassInstances;
		return Result;
	}
	inline static void PostPSSetShader(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDrawIndexed(ID3D11DeviceContext* This,
		UINT IndexCount, UINT StartIndexLocation, INT BaseVertexLocation)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_DrawIndexed_VTableIndex;
		Result.Args[0] = &IndexCount;
		Result.Args[1] = &StartIndexLocation;
		Result.Args[2] = &BaseVertexLocation;
		return Result;
	}
	inline static void PostDrawIndexed(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDraw(ID3D11DeviceContext* This,
		UINT VertexCount, UINT StartVertexLocation)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_DrawIndexed_VTableIndex;
		Result.Args[0] = &VertexCount;
		Result.Args[1] = &StartVertexLocation;
		return Result;
	}
	inline static void PostDraw(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PrePSSetConstantBuffers(ID3D11DeviceContext* This,
		UINT& StartSlot, UINT& NumBuffers, ID3D11Buffer**& ppConstantBuffers)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex;
		Result.Args[0] = &StartSlot;
		Result.Args[1] = &NumBuffers;
		Result.Args[2] = &ppConstantBuffers;
		return Result;
	}
	inline static void PostPSSetConstantBuffers(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDrawIndexedInstanced(ID3D11DeviceContext* This,
		UINT& IndexCountPerInstance, UINT& InstanceCount, UINT& StartIndexLocation, INT& BaseVertexLocation, UINT& StartInstanceLocation)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex;
		Result.Args[0] = &IndexCountPerInstance;
		Result.Args[1] = &InstanceCount;
		Result.Args[2] = &StartIndexLocation;
		Result.Args[3] = &BaseVertexLocation;
		Result.Args[4] = &StartInstanceLocation;
		return Result;
	}
	inline static void PostDrawIndexedInstanced(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDrawInstanced(ID3D11DeviceContext* This,
		UINT VertexCountPerInstance, UINT InstanceCount, UINT StartVertexLocation, UINT StartInstanceLocation)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.VTableIndex = ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex;
		Result.Args[0] = &VertexCountPerInstance;
		Result.Args[1] = &InstanceCount;
		Result.Args[2] = &StartVertexLocation;
		Result.Args[3] = &StartInstanceLocation;
		return Result;
	}
	inline static void PostDrawInstanced(Arguments& Previous, HRESULT result)
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

	inline static Arguments PreDrawAuto(ID3D11DeviceContext* This)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		return Result;
	}
	inline static void PostDrawAuto(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDrawIndexedInstancedIndirect(ID3D11DeviceContext* This,
		ID3D11Buffer*& pBufferForArgs, UINT& AlignedByteOffsetForArgs)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.Args[0] = &pBufferForArgs;
		Result.Args[1] = &AlignedByteOffsetForArgs;
		return Result;
	}
	inline static void PostDrawIndexedInstancedIndirect(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreDrawInstancedIndirect(ID3D11DeviceContext* This,
		ID3D11Buffer*& pBufferForArgs, UINT& AlignedByteOffsetForArgs)
	{
		Arguments Result;
		Result.IID = __uuidof(ID3D11DeviceContext);
		Result.PPV = This;
		Result.Args[0] = &pBufferForArgs;
		Result.Args[1] = &AlignedByteOffsetForArgs;
		return Result;
	}
	inline static void PostDrawInstancedIndirect(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreSetColorSpace1(IDXGISwapChain3* This,
		DXGI_COLOR_SPACE_TYPE& ColorSpace)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain3);
		Result.PPV = This;
		Result.Args[0] = &ColorSpace;
		return Result;
	}
	inline static void PostSetColorSpace1(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}

	inline static Arguments PreSetHDRMetaData(IDXGISwapChain4* This, DXGI_HDR_METADATA_TYPE& Type, UINT& Size,
		Arguments::OptionalStruct<DXGI_HDR_METADATA_HDR10>& MetaData10,
		Arguments::OptionalStruct<DXGI_HDR_METADATA_HDR10PLUS>& MetaData10Plus)
	{
		Arguments Result;
		Result.IID = __uuidof(IDXGISwapChain4);
		Result.PPV = This;
		Result.VTableIndex = IDXGISwapChain4_SetHDRMetaData_VTableIndex;
		Result.Args[0] = &Type;
		Result.Args[1] = &Size;
		Result.Args[2] = &MetaData10;
		Result.Args[3] = &MetaData10Plus;
		return Result;
	}
	inline static void PostSetHDRMetaData(Arguments& Previous, HRESULT result)
	{
		Previous.Post = TRUE;
		Previous.Result = result;
	}
};
