using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowMapTest : MonoBehaviour
{
    private Light l;
    public RenderTexture _ShadowmapCopy;
    
    // Start is called before the first frame update
    void Start()
    {
        l = GetComponent<Light>();
        RenderTargetIdentifier shadowmapIdentifier = BuiltinRenderTextureType.Depth;
        _ShadowmapCopy = new RenderTexture(1024,1024,0);
        CommandBuffer cb = new CommandBuffer();
        cb.SetShadowSamplingMode(shadowmapIdentifier,ShadowSamplingMode.RawDepth);
        cb.Blit(shadowmapIdentifier,new RenderTargetIdentifier(_ShadowmapCopy));
        l.AddCommandBuffer(LightEvent.AfterShadowMap,cb);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
