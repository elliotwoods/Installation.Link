//
// shader, generally for spin cursor
// projector overlays within a specific viewport

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tP: PROJECTION;   //projection matrix as set via Renderer (EX9)
float4x4 tWVP: WORLDVIEWPROJECTION;
int      ViewIndex : VIEWPORTINDEX;

float4 cAmb : COLOR <String uiname="Color";>  = {1, 1, 1, 1};
int iViewPort = -1;
bool enableAlpha = false;

//texture
texture Tex <string uiname="Texture";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = NONE;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
	
};

float4x4 tTex: TEXTUREMATRIX <string uiname="Texture Transform";>;
float4x4 tTex2;
//the data structure: "vertexshader to pixelshader"
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos : POSITION;
    float4 TexCd : TEXCOORD0;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------

vs2ps VS(
    float4 Pos : POSITION,
    float4 TexCd : TEXCOORD0)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;

    //transform position
    Out.Pos = mul(Pos, tWVP);
    
    //set size of object to 0 if not on this screen
    // we also set alpha to 0 later on
    Out.Pos *= abs(ViewIndex-iViewPort)<0.1;


    //transform texturecoordinates
    Out.TexCd = mul(TexCd, tTex);
    
    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): COLOR
{
    //In.TexCd = In.TexCd / In.TexCd.w; // for perpective texture projections (e.g. shadow maps) ps_2_0

	float4 texcd = In.TexCd;
	texcd /= texcd.w;
	texcd = mul(texcd, tTex2);
	
	//float4 texCol = tex2Dbicubic(Samp, texcd);
	float4 texCol = tex2D(Samp, texcd);
	
	float4 col;
    
    col.a = 1;
    if (enableAlpha)
	{
    	col = texCol;
		col.a *= texcd.x > 0 && texcd.x < 1 && texcd.y > 0 && texcd.y < 1;
    } else
    	col.rgb = texCol.rgb * texCol.a;
    
    if (cAmb.a != 0) //specific condition for colour add (e.g. indicate)
    	col *= cAmb;
    else
    	col += cAmb;
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
