using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Example3DTextureCreator : MonoBehaviour
{
    Texture3D texture;

    void Start ()
    {
        texture = CreateTexture3D (256);
        RenderTexture renderTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32)
        {
            dimension = TextureDimension.Tex3D, volumeDepth = 256, enableRandomWrite = true
        };
        renderTexture.Create();
        Graphics.Blit(texture,renderTexture);
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
