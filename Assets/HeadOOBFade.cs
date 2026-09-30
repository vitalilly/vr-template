using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class VRHeadClipFade : MonoBehaviour
{
    [Header("Layer Configuration")]
    [SerializeField] private LayerMask obstacleLayers; // Set to SubmarineHull layer

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Color fadeColor = Color.black;

    private int overlappingColliders = 0;
    private float currentAlpha = 0f;
    private Material fadeMaterial;

    private void Awake()
    {
        // Create a simple unlit material for full-screen camera overlay
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        fadeMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fadeMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        fadeMaterial.SetInt("_ZWrite", 0);
        fadeMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object belongs to our obstacle layer
        if (((1 << other.gameObject.layer) & obstacleLayers) != 0)
        {
            overlappingColliders++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayers) != 0)
        {
            overlappingColliders = Mathf.Max(0, overlappingColliders - 1);
        }
    }

    private void Update()
    {
        float targetAlpha = (overlappingColliders > 0) ? 1.0f : 0.0f;
        
        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, (1.0f / fadeDuration) * Time.deltaTime);
        }
    }

    private void OnPostRender()
    {
        if (currentAlpha <= 0f) return;

        // Render full-screen quad over camera view
        fadeMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        
        Color renderColor = fadeColor;
        renderColor.a = currentAlpha;
        GL.Color(renderColor);

        GL.Vertex3(0, 0, 0);
        GL.Vertex3(0, 1, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(1, 0, 0);

        GL.End();
        GL.PopMatrix();
    }
}