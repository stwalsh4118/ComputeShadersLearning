using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//must add RayTracingMaster to the scene's camera for the OnRenderImage to be called
public class RayTracingMaster : MonoBehaviour
{
    
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    private Camera _camera;
    public Texture SkyboxTexture;
    private uint _currentSample = 0;
    private Material _addMaterial;
    public Light DirectionalLight;

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    private void Update() {
        
        //if our camera has changed in anyway, reset the progressive sampling
        if(transform.hasChanged) {
            _currentSample = 0;
            transform.hasChanged = false;
        }

        if(DirectionalLight.transform.hasChanged) {
            _currentSample = 0;
            DirectionalLight.transform.hasChanged = false;
        }
    }

    private void SetShaderParameters() {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
    }

    //OnRenderImage function is automatically called by Unity whenever the camera has finished rendering
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {

        SetShaderParameters();
        Render(destination);
        
    }

    private void Render(RenderTexture destination) {
        //Make sure we have a current render target
        InitRenderTexture();

        //Set the target and dispatch the compute shader
        //Dispatching the shader entails telling the GPU to get busy with a number of thread groups executing our shader code
        //Each thread group consists of a number of threads whichis set in the shader itself
        //size and number of thread groups can be specified in up to three dimensions, which makes it easy to apply compute shaders to problems of either dimensionality

        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //Blit the result texture to the screen
        //This writes the result to the screen

        //progressive sampling, with our AddShader makes each successive sample less impactful if the view hasn't changed
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;

    }

    private void InitRenderTexture() {

        if(_target == null || _target.width != Screen.width || _target.height != Screen.height) {
            
            //Release render texture if we already have one
            if(_target != null) {
                _target.Release();
            }

            //Get a render target for Ray Tracing

            //create a render target of appropriate dimensions, the 0 is the index of the compute shader's kernel function
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();

        }
    }
}
