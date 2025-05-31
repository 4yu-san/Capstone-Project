using UnityEngine;

public class LaserBeamEffect : MonoBehaviour
{
    [Header("Laser Beam Materials")]
    public Material laserMaterial; // Drag your laser material here
    
    void Start()
    {
        SetupLaserBeam();
    }
    
    void SetupLaserBeam()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        
        if (lineRenderer != null)
        {
            // Basic settings
            lineRenderer.material = laserMaterial;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 2;
            
            // Make it glow
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 1;
            
            // Color gradient (bright to dim)
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.red, 0.0f), 
                    new GradientColorKey(Color.yellow, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.8f, 1.0f) 
                }
            );
            lineRenderer.colorGradient = gradient;
            
            // Start disabled
            lineRenderer.enabled = false;
        }
    }
}