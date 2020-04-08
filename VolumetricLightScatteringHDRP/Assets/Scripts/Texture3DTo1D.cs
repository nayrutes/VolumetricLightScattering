using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Texture3DTo1D : MonoBehaviour
{
    [SerializeField] private RenderTexture texture3D;
    [SerializeField] private RenderTexture texture1D;

    [SerializeField] private Vector2Int texel;

    [SerializeField] private ComputeShader computeShader;

    public bool adjust1D;
    public bool adjust3D;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(texture1D == null || texture3D == null || computeShader == null)
            return;
        
        Vector3Int size3D = new Vector3Int(texture3D.width, texture3D.height, texture3D.volumeDepth);
        if (adjust3D)
        {
            AdjustRenderTexture(ref texture3D,size3D);
        }
        if(adjust1D)
        {
            AdjustRenderTexture(ref texture1D,new Vector3Int(size3D.z,1,1),false);
        }
        Convert();
    }
    
    private void Convert()
    {
        Vector3Int size = new Vector3Int(texture3D.width, texture3D.height, texture3D.volumeDepth);
        
        computeShader.SetTexture(0,"Input",texture3D);
        computeShader.SetTexture(0,"Output",texture1D);
        //computeShader.SetVector("ratios", new Vector4(1.0f/texture3DToSlice.width,y: 1.0f/texture3DToSlice.height,z: 1.0f/texture3DToSlice.volumeDepth,w: 1));
        
        computeShader.SetVector("size", new Vector4(size.x,y: size.y,z: size.z,w: 1));
        int x =  Mathf.Clamp(texel.x, 0, size.x - 1);
        int y = Mathf.Clamp(texel.y, 0, size.y - 1);
        //Vector2Int textTmp = new Vector2Int(texel.x, texel.y);
        computeShader.SetInt("texelX", texel.x);
        computeShader.SetInt("texelY", texel.y);
        
        //computeShader.SetInt("toSlice", sliceInt);
        //computeShader.SetVector("singleFroxel", new Vector4(_froxels.singleFroxel.x,_froxels.singleFroxel.y,_froxels.singleFroxel.z,0));
        //computeShader.SetBool("markSingle", !_froxels.toggleSingleAll);
        
        computeShader.Dispatch(0, size.z/16,1, 1);
    }
    
    private static void AdjustRenderTexture(ref RenderTexture r, Vector3Int size, bool depth = true, bool randomwrite = true)
    {
        r.Release();
        r.width = size.x;
        r.height = size.y;
        if (depth)
        {
            r.dimension = TextureDimension.Tex3D;
            r.volumeDepth = size.z;
        }
        else
        {
            r.dimension = TextureDimension.Tex2D;
        }
        if(randomwrite)
            r.enableRandomWrite = true;
        r.Create();
    }
}
