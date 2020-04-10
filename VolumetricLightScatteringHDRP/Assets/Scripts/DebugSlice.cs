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
    [SerializeField] private RenderTexture texture2D;
    [SerializeField] private bool switchTo2D;
    private RenderTexture debugRenderTexture;
    //[SerializeField] private Shader debugShader;

    [Range(0.0f,1.0f)]
    [SerializeField] private float size;
    [Range(0.0f,1.0f)]
    [SerializeField] private float slice;

    [SerializeField] private bool syncSliceWithFroxel;
    [SerializeField] private bool alphaToWhite;
    
    [Header("Feedback")] [SerializeField] private int slice_;
    //[Range(0,256)]
    //[SerializeField] private int slice;
    [SerializeField] private Material debugMaterial;

    [SerializeField] private ComputeShader slicer;

    private int sliceInt = 0;

    private Froxels _froxels;
    // Start is called before the first frame update
    void Start()
    {
        _froxels = FindObjectOfType<Froxels>();
    }

    private void Slice()
    {
        
        
        slicer.SetTexture(0,"Input",texture3DToSlice);
        slicer.SetTexture(0,"Output",debugRenderTexture);
        slicer.SetVector("ratios", new Vector4(1.0f/texture3DToSlice.width,y: 1.0f/texture3DToSlice.height,z: 1.0f/texture3DToSlice.volumeDepth,w: 1));
        slicer.SetVector("size", new Vector4(texture3DToSlice.width,y: texture3DToSlice.height,z: texture3DToSlice.volumeDepth,w: 1));
        
        slice = Mathf.Clamp(slice, 0, texture3DToSlice.volumeDepth);
        slicer.SetInt("toSlice", sliceInt);
        slicer.SetVector("singleFroxel", new Vector4(_froxels.singleFroxel.x,_froxels.singleFroxel.y,_froxels.singleFroxel.z,0));
        slicer.SetBool("markSingle", !_froxels.toggleSingleAll);
        //int threadGroupsX = texture3DToSlice.volumeDepth / 256;
        Vector3Int dispatches = new Vector3Int(Froxels.CeilDispatch(texture3DToSlice.width,8),Froxels.CeilDispatch(texture3DToSlice.height,8),1);
        slicer.Dispatch(0,dispatches.x,dispatches.y,dispatches.z);
        //slicer.Dispatch(0, texture3DToSlice.width/8,texture3DToSlice.height/8,1);
    }

    //private int sliceTmp = -1;
    
    // Update is called once per frame
    void Update()
    {

        if (alphaToWhite)
        {
            debugMaterial.SetFloat("alphaToWhite", 1);
        }
        else
        {
            debugMaterial.SetFloat("alphaToWhite", 0);
        }
        
        
        
        if (switchTo2D)
        {
            debugRenderTexture = texture2D;
        }
        else
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

            debugRenderTexture.filterMode = FilterMode.Point;
        
            if (syncSliceWithFroxel)
            {
                sliceInt = _froxels.singleFroxel.z;
                slice = (sliceInt / (float)texture3DToSlice.volumeDepth);
            }
            else
            {
                sliceInt = (int)(slice * (float)texture3DToSlice.volumeDepth);
            }
            //sliceInt = (int) ((1-slice) * texture3DToSlice.volumeDepth);
            slice_ = sliceInt;
            
            Slice();
        }
    
        debugMaterial.SetFloat("size",size);
        debugMaterial.SetTexture("debugTexture", debugRenderTexture);
        debugMaterial.SetVector("debugImageSize", new Vector4(debugRenderTexture.width,debugRenderTexture.height,0,0));
    }
}
