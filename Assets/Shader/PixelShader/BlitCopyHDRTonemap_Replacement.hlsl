// this shader replaces the Unity shader called "Hidden/BlitCopyHDRTonemap"

// what the original shader end up achieved
// was deducting the color values range from [0, 1] to about around [0, 0.55]
// to produce the same peak lumimance level as SDR content (which looks very dim) on HDR screen
// (a white pixel has same brightness level as SDR white pixel in another program on screen)
// but some UI elements however were not effected, they were still bright (inconsistancy!)

// in conclusion,
// it didn't make use of the high dynamic range of the HDR screen,
// it did the opposite, it reverse mapped the original picture back into the poor SDR range;
// in other words, instead of making the picture brighter, it made it darker.

// after re-analyzing everything, I think I've gathered what's been happening:

// 1. The industry standards says that, SDR content should only look like SDR on HDR screen,
//    HDR content is allowed to be up to super bright (like basically a flashbang), while SDR is not.

//    They aim to make all legacy SDR content on a HDR screen
//    still look the same (same low brightness) as they were on SDR screens.

// 2. The idea on HDR at this point was that, the screen would only go super bright when it needed to,
//    when HDR content is being displayed.

// 3. So Unity had configured games that did not feature HDR content
//    to look the same as SDR, complying to the industry standard

// 4. What they must not have expected was how HDR were actually handled by the enterprises

// 5. HDR TVs like mine were "entry level HDRs" (and I regret buying it),
//    they only had peak luminance of about 400 nits or so,
//    far from expectations of a true HDR (1000 nits at least)

// 6. The manufacturers are like:
//	  "What are you going to do with that many nits anyway? Blind youself?"
//    and they certainly do know how to trick costumers into believing it,

// 7. what they did was this one simple little thing:
//    introduced a thing called "HDR mode" (as opposed to SDR mode) and stays on SDR mode by default;
//    while on SDR mode, it will map (upscale) SDR content into its maximum HDR brightness range
//    and they don't need to worry about flashbang because entry level HDRs wasn't that bright to begin with;
//    this makes sense at first,
//    but it's giving you the illusion that turning on HDR has opposite effect of HDR
//    Windows looks dim in HDR because SDR content doesn't get upscaled while it's in HDR mode.

//    this misinformation has cause some people
//    (like the amazing visual artist / animator Minty Root)
//    to not wanting to adapt making of HDR content (at least not yet according to my knowledge)

// 8. it didn't help that these HDR TVs may be ignoring all the HDR10 metadata
//    which the HDR applications are sending to them via IDXGISwapChain4::SetHDRMetaData method;
//    these TVs probably only allow users themselves to adjust the gamma curve
//    via TV remote, this could further cause misinformation
//    since users can forget that they have adjusted the gamma curve,
//    which may unintentionaly cause different content to look wrong

// 9. to mitigate these issues, Microsoft has deprecated the
//    IDXGISwapChain4::SetHDRMetaData method (which Unity 2020 is still calling on),
//    and recommend applications to use scRGB instead
//    by using the DXGI_FORMAT_R16B16G16A16_FLOAT swap chain format
//    and calling IDXGISwapChain3::SetColorSpace1, to set color space to DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709
//    (which means linear mapping of pixel color signals, no gamma curve is applied by the OS)
//    and adjust (tonemap) their own output signals (pixel shader output values)
//    to fit the monitors' color gamut and brightness capabilities (which can be obtained from IDXGIOutput6::GetDesc1);

//    HOWEVER, monitor info obtained from IDXGIOutput6::GetDesc1 is NOT RELIABLE because I've tested it,
//    it says my HDR TV has 1499 nits of maximum luminance, A BLATANT LIE!
//    either the TV is lying, or the system failed to be get the real values,
//    either way we shouldn't rely on IDXGIOutput6::GetDesc1 to tonemap our applications.

//    This is probably why games like BF2042 are still asking you
//    to adjust the brightness youself by looking at comparison pictures even on HDR
//    it's because the games literally can't get any real info themselves

// 10. in scRGB, value [0, 12.5] is mapped linearly to the HDR screen's range,
//     value (12.5, 12.5, 12.5) effectively means means the brightest white color that the screen is able to display (while in HDR mode).
//     and if HDR is turned off while the application is in scRGB format DXGI_FORMAT_R16B16G16A16_FLOAT,
//     (1.0, 1.0, 1.0) will be the brightest value instead.

// 11. in addition, DXGI_FORMAT_R16B16G16A16_FLOAT is the only HDR format
//     that is compatible with Direct2D, the API which I was using to implement overlay

SamplerState s0 : register(s0);
Texture2D<float4> t0 : register(t0);

float4 main(float4 Position : SV_Position, float2 UV : TEXCOORD) : SV_Target
{
	// shouldn't do any tonemapping here because
	// 1. this is not the final render pass
	// 2. there is another shader "UI/Default" within the same render pass
	//    which is mapping values linearly in [0, 1]
	//    mapping values differently here could cause some stuff to
	//    look too dim or too bright (it depends on later render passes)
	
	// so we output original values directly to the output
	return t0.Sample(s0, UV);
}
