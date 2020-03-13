using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightCamera : MonoBehaviour
{
    [HideInInspector]public Camera cam;
    public Light l;
    [HideInInspector]public RenderTexture depthTexture;
    public Vector3 pointInScene;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        //l = GetComponentInParent<Light>();
        depthTexture = cam.targetTexture;
    }

    // Update is called once per frame
    void Update()
    {
        SetCamSettings();
        //TestProjection();
    }

    private void SetCamSettings()
    {
        //not needed?
        cam.transform.SetPositionAndRotation(l.transform.position, l.transform.rotation);
        cam.fieldOfView = l.spotAngle;
        cam.nearClipPlane = l.shadowNearPlane;
        cam.farClipPlane = l.range;
        cam.aspect = 1;
        Matrix4x4 camP = cam.projectionMatrix;
        Matrix4x4 camPNonJ = cam.nonJitteredProjectionMatrix;
        Matrix4x4 lightShadowP = l.shadowMatrixOverride;
        return;
    }

//    public void TestProjection()
//    {
//        Matrix4x4 camP = cam.projectionMatrix;
//        Vector4 pointInScene = this.pointInScene;
//        pointInScene.w = 1;
//        Matrix4x4 scaleMatrix = Matrix4x4.identity;
//        scaleMatrix.m22 = -1.0f;
//        Matrix4x4 v = scaleMatrix * cam.transform.worldToLocalMatrix;
//        Matrix4x4 vp = camP * v;
//        Vector4 pointProjected = vp * pointInScene;
//        Vector3 pointProjectedInWorld = cam.transform.localToWorldMatrix * (pointProjected/pointProjected.w);
//        
//        Froxels.DrawPointCross(pointInScene,0.5f,Color.green);
//        Froxels.DrawPointCross(pointProjectedInWorld,0.5f,Color.red);
//    }
    
}
