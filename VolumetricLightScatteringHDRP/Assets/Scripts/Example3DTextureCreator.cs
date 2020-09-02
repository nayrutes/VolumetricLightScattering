using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Example3DTextureCreator : MonoBehaviour
{
    private Texture3D texture;
    public RenderTexture renderTexture;
    public int size = 8;
    void Start ()
    {
        texture = CreateTexture3D (size);
        renderTexture.Release();
        renderTexture.dimension = TextureDimension.Tex3D;
        renderTexture.enableRandomWrite = true;
        renderTexture.volumeDepth = size;
        //        renderTexture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32)
//        {
//            dimension = TextureDimension.Tex3D, volumeDepth = size, enableRandomWrite = true
//        };
        renderTexture.Create();
        Graphics.Blit(texture,renderTexture);
        //Graphics.Blit(texture, renderTextureResult);
        //FindObjectOfType<DebugSlice>().texture3DToSlice = renderTexture;
    }

    Texture3D CreateTexture3D (int size)
    {
        Color[] colorArray = new Color[size * size * size];
        texture = new Texture3D (size, size, size, TextureFormat.RGBA32, true);
        float r = 1.0f / (size - 1.0f);
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                for (int z = 0; z < size; z++) {
                    Color c = new Color (x * r, y * r, z * r, 1.0f);
                    colorArray[x + (y * size) + (z * size * size)] = c;
                }
            }
        }
        texture.SetPixels (colorArray);
        texture.Apply ();
        return texture;
    }
}
