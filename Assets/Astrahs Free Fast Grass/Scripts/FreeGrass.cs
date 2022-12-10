using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astrah
{

    public class FreeGrass : MonoBehaviour
    {

         // - [ Serialized Values ]
            // - [ Splat Map Grass Threshold ]
            [Range(0.6f,0.95f)]
            public float SplatMapGrassThreshold = 0.8f;
            // - [ Grass Blades Per Row ]
            public int bladesPerRow = 20;
            // - [ Mesh Scale ]
            public float meshScale = 0.125f;
            // - [ Terrain ]
            public Terrain terrain;
            // - [ Shadowmap Values ]
            public Light mainLight;
            // - [ Main Cam ]
            public Camera mainCam;
            // - [ Bounds Multiplier ]
            public Vector3 boundsMulti = new Vector3(1f, 1f, 1f);

        // - [ Private Values ]
        private Mesh mesh;
        private Material material;
        private Bounds bounds;
        private ComputeBuffer cB_meshProperties;
        private ComputeBuffer cB_Args;
        private int ObjCount;
        private RenderTexture shadowMapRenderTexture;
        private CommandBuffer mainLightCommandBuffer;
      
        // - [ Get 'MG_Camera' ]
        private bool SetMG_Camera()
        {
            if (mainCam.cameraType != CameraType.Game) { Debug.LogError("Camera is not a game camera! Please make it into a game camera!"); return false; };
            if (mainCam.orthographic == true) { Debug.LogError("Your camera is set to orthographic! This isn't supported!"); return false; };
            return true;
        }
        // - [ Get Main Light ]
        private bool SetMainLight()  {
            if (mainLight == null) { Debug.LogError("Couldn't get component of 'Light' from 'MainLight' game Object! Please make sure your main light game object has a light component on it!"); return false; }
            if (mainLight.type != LightType.Directional) { Debug.LogError("No directional light on main light component in 'MainLight' game object! Please make sure it's a directional light!");  }
            return true;
        }
        // - [ Null Value Check ]
        private bool AValueIsNullOrInvalid()  {
            if (mesh == null) { Debug.LogError("mesh = NULL!"); return true; }
            if (material == null) { Debug.LogError("material = NULL!"); return true; }
            if (bounds == null) { Debug.LogError("bounds = NULL!"); return true; }
            if (cB_Args == null) { Debug.LogError("cB_Args = NULL!"); return true; }
            return false;
        }
        // - [ Draw Mesh Instanced Indirect ]
        private void DrawMeshInstancedIndirect() {
            // - Send the cB_Args to the GPU!
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, cB_Args, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, 0, null); 
        }
        // - [ Get Mesh ]
        private Mesh GetMesh() { return gameObject.transform.Find("MeshToInstance").GetComponent<MeshFilter>().mesh; }
        // - [ Get Material ]
        private Material GetMeshMaterial() { return gameObject.transform.Find("MeshToInstance").GetComponent<MeshRenderer>().material; }
        // - [ Get Bounds ]
        private Bounds GetBounds() { return new Bounds(Vector3.zero, new Vector3(1000f*boundsMulti.x, 1000f*boundsMulti.y, 1000f*boundsMulti.z)); }
        // - [ Is Square Number ]
        private bool isSquareNumber(int number) {return Mathf.Sqrt(number) % 1 == 0; }
        // - [ Init Variables and Compute Buffers ]
        private IEnumerator Init() {
            if (SetMG_Camera() == false) { Debug.LogError("No main camera set! You need to set a main camera!"); yield break; }
            if (SetMainLight() == false) { Debug.LogError("Terminating operation."); yield break; }

            // - Global texture can be grabbed from shaders!
                // The idea is that we grab the value from our shaders '_SunCascadedShadowMap'. 
                // We can use this to get a global world position of the shadow map. This allows
                // us to color the blades of grass individually by their worldPos in the fragment function of 
                // our grass shader!
            mainLightCommandBuffer = new CommandBuffer();
            RenderTargetIdentifier shadowMapRenderTextureIdentifier = BuiltinRenderTextureType.CurrentActive;
            mainLightCommandBuffer.SetGlobalTexture("_SunCascadedShadowMap", shadowMapRenderTextureIdentifier);
            mainLight.AddCommandBuffer(LightEvent.AfterShadowMap, mainLightCommandBuffer);
           
            ObjCount        = bladesPerRow * bladesPerRow; //squared for box...
            mesh            = GetMesh();
            material        = GetMeshMaterial();
            bounds          = GetBounds();

            // - Initialize Buffers to be sent to the GPU.
            Buffers.InitializeMeshPropertiesBuffer_Terr(ref cB_meshProperties, ObjCount, ref material, terrain, meshScale, this);
            Buffers.InitializeArgsBuffer(ref cB_Args, mesh, ObjCount);

            if (AValueIsNullOrInvalid() == false)
                Inited = true;

            yield break; }
        // - [ Feed In Camera World Pos ]
        private void FeedInCameraWorldPosToMaterial() {
            material.SetVector("_CamWorldPos", new Vector4(mainCam.transform.position.x, mainCam.transform.position.y, mainCam.transform.position.z, 1));
        }
        // - [ Update ]
        private bool Inited = false;
        private void Update() {
            if (Inited  == false) { return; }
            if (mainCam == null) { return; }

            bounds      = GetBounds();

            DrawMeshInstancedIndirect();
            FeedInCameraWorldPosToMaterial();
        }
        // - [ Release Compute Buffers ]
        private void OnDisable() { 
            try {
                mainLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, mainLightCommandBuffer);
                mainLightCommandBuffer.Clear();
            } catch {
                Debug.LogWarning("Couldn't remove command buffer?");
            } }
        // - [ Entry Point ]
        private void OnEnable() { StartCoroutine(Init()); }

    }

}
