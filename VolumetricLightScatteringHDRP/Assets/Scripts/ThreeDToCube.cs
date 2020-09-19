using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class ThreeDToCube : MonoBehaviour
{
    [SerializeField] private ComputeShader slicer;
    [SerializeField] public RenderTexture texture3DToSlice;
    [SerializeField] public RenderTexture front;
    [SerializeField] public RenderTexture top;
    [SerializeField] public RenderTexture back;
    [SerializeField] public RenderTexture bottom;
    [SerializeField] public RenderTexture left;
    [SerializeField] public RenderTexture right;
    [SerializeField] public GameObject cube;
    [SerializeField] public RenderTexture cubeTexture;
    [SerializeField] public Material blitMat;
    [Range(0f,1f)]
    public float debugSlider;

    [SerializeField] public bool setColor = true;
    [Range(0f,1f)]
    [SerializeField] public float SizeX =1;
    [Range(0f,1f)]
    [SerializeField] public float SizeY=1;
    [Range(0f,1f)]
    public float SizeZ=1;
    
    private Mesh mesh;
    public Vector3Int amount;
    
    private enum Dimension
    {
        x,
        y,
        z
    }
    
    void OnEnable()
    {
        mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        AdjustRenderTexture(texture3DToSlice,true,true);
        AdjustRenderTexture(front,false,true);
        AdjustRenderTexture(top,false,true);
        AdjustRenderTexture(back,false,true);
        AdjustRenderTexture(bottom,false,true);
        AdjustRenderTexture(left,false,true);
        AdjustRenderTexture(right,false,true);

        int max = Mathf.Max(amount.x, amount.y, amount.z);
        AdjustRenderTexture(cubeTexture, new Vector3Int(max*3, max*3,max*3), false, true);
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale=new Vector3(SizeX,SizeY,SizeZ);
        
        Vector3 sizeRelative = MeasureCube(cube);
        SliceAndBlit(sizeRelative);
        CubeAtlas(mesh, sizeRelative);
    }

    private Vector3 MeasureCube(GameObject o)
    {
        Vector3 size = o.transform.localScale;
        float max = Mathf.Max(new float[] {size.x, size.y, size.z});
        Vector3 sizeRelative = new Vector3(max/size.x,max/size.y,max/size.z);
        return sizeRelative;
    }

    private void Slice(int sliceIndex, RenderTexture resultTexture, Dimension d, bool flipX=false, bool flipY=false, bool flipZ=false, bool switchXY=false)
    {
        slicer.SetTexture(0,"Input",texture3DToSlice);
        slicer.SetTexture(1,"Input",texture3DToSlice);
        slicer.SetTexture(2,"Input",texture3DToSlice);
        slicer.SetTexture(3,"Input",texture3DToSlice);
        slicer.SetTexture(0,"Output",resultTexture);
        slicer.SetTexture(1,"Output",resultTexture);
        slicer.SetTexture(2,"Output",resultTexture);
        slicer.SetVector("ratios", new Vector4(1.0f/texture3DToSlice.width,y: 1.0f/texture3DToSlice.height,z: 1.0f/texture3DToSlice.volumeDepth,w: 1));
        slicer.SetVector("size", new Vector4(texture3DToSlice.width,y: texture3DToSlice.height,z: texture3DToSlice.volumeDepth,w: 1));
        slicer.SetBool("dimensionX", d==Dimension.x);
        slicer.SetBool("dimensionY", d==Dimension.y);
        slicer.SetBool("dimensionZ", d==Dimension.z);
        slicer.SetBool("flipX", flipX);
        slicer.SetBool("flipY", flipY);
        slicer.SetBool("flipZ", flipZ);
        slicer.SetBool("switchXY", switchXY);
        
        //slice = Mathf.Clamp(slice, 0, texture3DToSlice.volumeDepth);
        slicer.SetInt("toSlice", sliceIndex);
        //slicer.SetVector("singleFroxel", new Vector4(_froxels.singleFroxel.x,_froxels.singleFroxel.y,_froxels.singleFroxel.z,0));
        //slicer.SetBool("markSingle", !_froxels.toggleSingleAll);
        //int threadGroupsX = texture3DToSlice.volumeDepth / 256;
        if (setColor)
        {
            Vector3Int dispatchesMain = new Vector3Int(Froxels.CeilDispatch(texture3DToSlice.width,8),Froxels.CeilDispatch(texture3DToSlice.height,8),1);
            slicer.Dispatch(3,dispatchesMain.x,dispatchesMain.y,dispatchesMain.z);
        }
        
        if(d==Dimension.z)
        {
            Vector3Int dispatches = new Vector3Int(Froxels.CeilDispatch(texture3DToSlice.width,8),Froxels.CeilDispatch(texture3DToSlice.height,8),1);
            slicer.Dispatch(0,dispatches.x,dispatches.y,dispatches.z);
        }
        else if(d==Dimension.x)
        {
            Vector3Int dispatches = new Vector3Int(1,Froxels.CeilDispatch(texture3DToSlice.height,8),Froxels.CeilDispatch(texture3DToSlice.volumeDepth,8));
            slicer.Dispatch(1,dispatches.x,dispatches.y,dispatches.z);
        }
        else if(d==Dimension.y)
        {
            Vector3Int dispatches = new Vector3Int(Froxels.CeilDispatch(texture3DToSlice.width,8),1,Froxels.CeilDispatch(texture3DToSlice.volumeDepth,8));
            slicer.Dispatch(2,dispatches.x,dispatches.y,dispatches.z);
        }
        //slicer.Dispatch(0, texture3DToSlice.width/8,texture3DToSlice.height/8,1);
    }

    private void CubeAtlas(Mesh mesh, Vector3 sizeRelative)
    {
        if (mesh == null)
            return;
        Vector2[] uVs = new Vector2[mesh.vertices.Length];

        float o3 = 1.0f / 3.0f;
        float t3 = 2.0f / 3.0f;
        Vector3 oDivsizeRel = new Vector3(1/sizeRelative.x,1/sizeRelative.y,1/sizeRelative.z);
        
        // Front
        uVs[0] = new Vector2(0.0f, 0.0f);
        uVs[1] = new Vector2(o3*oDivsizeRel.x, 0.0f);
        uVs[2] = new Vector2(0.0f, o3*oDivsizeRel.y);
        uVs[3] = new Vector2(o3*oDivsizeRel.x, o3*oDivsizeRel.y);
        // Top
        uVs[4] = new Vector2(0.334f, o3*oDivsizeRel.z);
        uVs[5] = new Vector2(o3+o3*oDivsizeRel.x, o3*oDivsizeRel.z);
        uVs[8] = new Vector2(0.334f, 0.0f);
        uVs[9] = new Vector2(o3+o3*oDivsizeRel.x, 0.0f);
        // Back
        uVs[6] = new Vector2(1.0f, 0.0f);
        uVs[7] = new Vector2(1 -(o3*oDivsizeRel.x), 0.0f);
        uVs[10] = new Vector2(1.0f, o3*oDivsizeRel.y);
        uVs[11] = new Vector2(1 -(o3*oDivsizeRel.x), o3*oDivsizeRel.y);
        // Bottom
        uVs[12] = new Vector2(0.0f, t3-o3*oDivsizeRel.z);
        uVs[13] = new Vector2(0.0f, 0.666f);
        uVs[14] = new Vector2(o3*oDivsizeRel.x, 0.666f);
        uVs[15] = new Vector2(o3*oDivsizeRel.x, t3-o3*oDivsizeRel.z);
        // Right
        uVs[16] = new Vector2(0.667f, 0.334f);
        uVs[17] = new Vector2(0.667f, o3+o3*oDivsizeRel.y);
        uVs[18] = new Vector2(t3+o3*oDivsizeRel.z, o3+o3*oDivsizeRel.y);
        uVs[19] = new Vector2(t3+o3*oDivsizeRel.z, 0.334f);
        // Left        
        uVs[20] = new Vector2(t3-o3*oDivsizeRel.z, 0.334f);
        uVs[21] = new Vector2(t3-o3*oDivsizeRel.z, o3+o3*oDivsizeRel.y);
        uVs[22] = new Vector2(0.666f, o3+o3*oDivsizeRel.y);
        uVs[23] = new Vector2(0.666f, 0.334f);
        mesh.uv = uVs;
    }

    private void SliceAndBlit(Vector3 sizeRelative)
    {
        Vector3 oDivsizeRel = new Vector3(1/sizeRelative.x,1/sizeRelative.y,1/sizeRelative.z);
        Vector3Int indexes = new Vector3Int((int) (oDivsizeRel.x*texture3DToSlice.width), (int) (oDivsizeRel.y*texture3DToSlice.height), (int) (oDivsizeRel.z*texture3DToSlice.volumeDepth));
        Slice(0, front, Dimension.z);
        Slice(indexes.z, back, Dimension.z, true);
        Slice(0, left, Dimension.x, false,false,true, false);
        Slice(indexes.x, right, Dimension.x);
        Slice(0, bottom, Dimension.y, flipZ:true);
        Slice(indexes.y, top, Dimension.y);
        
        
        float o3 = 1 / 3.0f;
        Vector2 scale = new Vector2(1,1);//new Vector2(o3, o3);
        Graphics.Blit(front, cubeTexture, blitMat, 0);// scale, new Vector2(0, 0));
        
        //Graphics.BlitMultiTap(front,cubeTexture, blitMat, new Vector2(0,0));
        //Graphics.Blit(top, cubeTexture, scale, new Vector2(o3*cubeTexture.width, 0));
        //Graphics.BlitMultiTap(top,cubeTexture,blitMat,new Vector2(o3*cubeTexture.width,0));
        //Graphics.Blit(back, cubeTexture, scale, new Vector2(2*o3, 0));
        //Graphics.Blit(bottom, cubeTexture, scale, new Vector2(0, o3));
        //Graphics.Blit(left, cubeTexture, scale, new Vector2(o3, o3));
        //Graphics.Blit(right, cubeTexture, scale, new Vector2(2*o3, o3));
    }
    
    private void AdjustRenderTexture(RenderTexture r, Vector3Int amount, bool depth = true, bool randomwrite = true)
    {
        r.Release();
        r.width = amount.x;
        r.height = amount.y;
        if(depth)
            r.volumeDepth = amount.z;
        if(randomwrite)
            r.enableRandomWrite = true;
        r.Create();
    }
    
    private void AdjustRenderTexture(RenderTexture r, bool depth = true, bool randomwrite = true)
    {
        AdjustRenderTexture(r,amount,depth, randomwrite);
    }
}
