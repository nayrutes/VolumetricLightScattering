using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

[ExecuteInEditMode,RequireComponent(typeof(CustomPassVolume))]
public class DebugSlice : MonoBehaviour
{

    //public static DebugSlice _DebugSlice;
    [SerializeField] public RenderTexture texture3DToSlice;
    [SerializeField] private RenderTexture debugRenderTexture;
    //[SerializeField] private Shader debugShader;

    [Range(0.0f,1.0f)]
    [SerializeField] private float size;
    [Range(0.0f,1.0f)]
    [SerializeField] private float slice;
    //[Range(0,256)]
    //[SerializeField] private int slice;
    [SerializeField] private Material debugMaterial;

    [SerializeField] private ComputeShader slicer;

    private int sliceInt = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Slice()
    {
        
        
        slicer.SetTexture(0,"Input",texture3DToSlice);
        slicer.SetTexture(0,"Output",debugRenderTexture);
        slicer.SetVector("ratios", new Vector4(1.0f/texture3DToSlice.width,y: 1.0f/texture3DToSlice.height,z: 1.0f/texture3DToSlice.volumeDepth,w: 1));
        slicer.SetVector("size", new Vector4(texture3DToSlice.width,y: texture3DToSlice.height,z: texture3DToSlice.volumeDepth,w: 1));
        
        slice = Mathf.Clamp(slice, 0, texture3DToSlice.volumeDepth);
        slicer.SetInt("toSlice", sliceInt);
        //int threadGroupsX = texture3DToSlice.volumeDepth / 256;
        slicer.Dispatch(0, 1,1,1);
    }

    private int sliceTmp = -1;
    
    // Update is called once per frame
    void Update()
    {
        if (!texture3DToSlice.enableRandomWrite)
        {
            texture3DToSlice.Release();
            texture3DToSlice.enableRandomWrite = true;
            texture3DToSlice.Create();
        }
        if (debugRenderTexture == null)
        {
            debugRenderTexture = new RenderTexture(texture3DToSlice.descriptor)
            {
                dimension = TextureDimension.Tex2D, enableRandomWrite = true
            };
            debugRenderTexture.Create();
        }
        if (!debugRenderTexture.enableRandomWrite)
        {
            debugRenderTexture.Release();
            debugRenderTexture.enableRandomWrite = true;
            debugRenderTexture.Create();
        }

        sliceInt = (int) (slice * texture3DToSlice.volumeDepth);
        debugMaterial.SetFloat("size",size);
        debugMaterial.SetTexture("debugTexture", debugRenderTexture);
        debugMaterial.SetVector("debugImageSize", new Vector4(debugRenderTexture.width,debugRenderTexture.height,0,0));
        if (sliceTmp != sliceInt)
        {
            Slice();
            sliceTmp = sliceInt;
        }
    }
}
