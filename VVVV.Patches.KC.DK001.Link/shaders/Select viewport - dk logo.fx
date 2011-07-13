//
// shader, generally for spin cursor
// projector overlays within a specific viewport

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

#include "Bicubic.fxh"

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tP: PROJECTION;   //projection matrix as set via Renderer (EX9)
float4x4 tWVP: WORLDVIEWPROJECTION;
int      ViewIndex : VIEWPORTINDEX;

float4 cAmb : COLOR <String uiname="Color";>  = {1, 1, 1, 1};
int iViewPort = -1;

//texture
texture Tex <string uiname="Texture";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU = Border;
    AddressV = Border;
};

float4x4 tTex: TEXTUREMATRIX <string uiname="Texture Transform";>;
//the data structure: "vertexshader to pixelshader"
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos : POSITION;
    float4 PosP : TEXCOORD1;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------

vs2ps VS(
    float4 Pos : POSITION)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;

    //transform position
    Out.Pos = mul(Pos, tW);
    Out.PosP = mul(Pos, tWVP);
    
    //set size of object to 0 if not on this screen
    // we also set alpha to 0 later on
    Out.Pos *= abs(ViewIndex-iViewPort)<0.1;
    
    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): COLOR
{
    //In.TexCd = In.TexCd / In.TexCd.w; // for perpective texture projections (e.g. shadow maps) ps_2_0

	float4 texcd = In.PosP / In.PosP.w;
	texcd = mul(texcd, tTex);
	
	float4 texCol = tex2Dbicubic(Samp, texcd);
    float4 col;

    col = texCol;
    col *= cAmb;
    
    col.a *= abs(ViewIndex-iViewPort)<0.1;
    return col;
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TWithinProjection
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_1_1 VS();
        PixelShader = compile ps_2_0 PS();
    }
}
