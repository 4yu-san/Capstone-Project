using UnityEngine;
using System.Collections.Generic;

public class GraffitiSystem : MonoBehaviour
{
    [Header("Spray Settings")]
    public KeyCode sprayKey = KeyCode.Mouse0;
    public float sprayRange = 3f;
    public float sprayRadius = 0.5f;
    public LayerMask sprayableLayer = 1;
    public Color sprayColor = Color.red;
    public int textureResolution = 512;
    
    [Header("Spray Effects")]
    public ParticleSystem sprayParticles;
    public AudioSource spraySound;
    public float sprayIntensity = 0.1f;
    
    private Camera playerCamera;
    private Dictionary<Collider, RenderTexture> graffitiTextures = new Dictionary<Collider, RenderTexture>();
    private Dictionary<Collider, Material> originalMaterials = new Dictionary<Collider, Material>();
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            //playerCamera = FindObjectOfTypeG<Camera>();
            
        SetupSprayParticles();
    }
    
    void Update()
    {
        if (Input.GetKey(sprayKey))
        {
            SprayPaint();
        }
        
        if (Input.GetKeyUp(sprayKey))
        {
            StopSprayEffects();
        }
    }
    
    void SprayPaint()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, sprayRange, sprayableLayer))
        {
            // Check if object is sprayable
            SprayableWall wall = hit.collider.GetComponent<SprayableWall>();
            if (wall != null && wall.canBeSprayPainted)
            {
                ApplyGraffiti(hit);
                PlaySprayEffects(hit.point, hit.normal);
            }
        }
    }
    
    void ApplyGraffiti(RaycastHit hit)
    {
        Collider wallCollider = hit.collider;
        
        // Get or create render texture for this wall
        if (!graffitiTextures.ContainsKey(wallCollider))
        {
            CreateGraffitiTexture(wallCollider);
        }
        
        // Convert world hit point to UV coordinates
        Vector2 uvCoord = GetUVCoordinate(hit);
        
        // Draw on the texture
        DrawOnTexture(graffitiTextures[wallCollider], uvCoord);
    }
    
    void CreateGraffitiTexture(Collider wallCollider)
    {
        // Create render texture
        RenderTexture graffitiTexture = new RenderTexture(textureResolution, textureResolution, 0);
        graffitiTexture.Create();
        
        // Clear to transparent
        RenderTexture.active = graffitiTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
        
        graffitiTextures[wallCollider] = graffitiTexture;
        
        // Store original material and create new one
        Renderer renderer = wallCollider.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterials[wallCollider] = renderer.material;
            
            // Create new material with graffiti overlay
            Material graffitiMaterial = new Material(renderer.material);
            graffitiMaterial.SetTexture("_DetailAlbedoMap", graffitiTexture);
            graffitiMaterial.SetFloat("_DetailNormalMapScale", 1f);
            graffitiMaterial.SetTextureScale("_DetailAlbedoMap", Vector2.one);
            graffitiMaterial.SetTextureOffset("_DetailAlbedoMap", Vector2.zero);
            
            renderer.material = graffitiMaterial;
        }
    }
    
    Vector2 GetUVCoordinate(RaycastHit hit)
    {
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            return hit.textureCoord;
        }
        
        // Fallback: project world position to UV
        Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);
        return new Vector2(
            (localHitPoint.x + 0.5f) % 1f,
            (localHitPoint.y + 0.5f) % 1f
        );
    }
    
    void DrawOnTexture(RenderTexture texture, Vector2 uvCoord)
    {
        // Convert UV to pixel coordinates
        int x = Mathf.FloorToInt(uvCoord.x * textureResolution);
        int y = Mathf.FloorToInt(uvCoord.y * textureResolution);
        
        // Create brush texture
        Texture2D brush = CreateBrushTexture();
        
        // Apply brush to render texture
        Graphics.CopyTexture(brush, 0, 0, 0, 0, brush.width, brush.height, 
                           texture, 0, 0, 
                           Mathf.Max(0, x - brush.width/2), 
                           Mathf.Max(0, y - brush.height/2));
        
        DestroyImmediate(brush);
    }
    
    Texture2D CreateBrushTexture()
    {
        int brushSize = Mathf.FloorToInt(sprayRadius * textureResolution / 10f);
        brushSize = Mathf.Max(1, brushSize);
        
        Texture2D brush = new Texture2D(brushSize, brushSize, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(brushSize / 2f, brushSize / 2f);
        
        for (int x = 0; x < brushSize; x++)
        {
            for (int y = 0; y < brushSize; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance / (brushSize / 2f)));
                alpha *= sprayIntensity;
                
                // Add some randomness for spray effect
                alpha *= Random.Range(0.3f, 1f);
                
                Color pixelColor = sprayColor;
                pixelColor.a = alpha;
                brush.SetPixel(x, y, pixelColor);
            }
        }
        
        brush.Apply();
        return brush;
    }
    
    void PlaySprayEffects(Vector3 position, Vector3 normal)
    {
        // Particle effects
        if (sprayParticles != null)
        {
            if (!sprayParticles.isPlaying)
                sprayParticles.Play();
                
            sprayParticles.transform.position = position;
            sprayParticles.transform.rotation = Quaternion.LookRotation(normal);
        }
        
        // Sound effects
        if (spraySound != null && !spraySound.isPlaying)
        {
            spraySound.Play();
        }
    }
    
    void StopSprayEffects()
    {
        if (sprayParticles != null && sprayParticles.isPlaying)
        {
            sprayParticles.Stop();
        }
        
        if (spraySound != null && spraySound.isPlaying)
        {
            spraySound.Stop();
        }
    }
    
    void SetupSprayParticles()
    {
        if (sprayParticles != null)
        {
            var main = sprayParticles.main;
            main.startColor = sprayColor;
            main.startSpeed = 2f;
            main.startSize = 0.1f;
            main.startLifetime = 0.5f;
            
            var emission = sprayParticles.emission;
            emission.rateOverTime = 50f;
            
            var shape = sprayParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
        }
    }
    
    public void ChangeSprayColor(Color newColor)
    {
        sprayColor = newColor;
        SetupSprayParticles();
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.green;
            Vector3 forward = playerCamera.transform.forward;
            Gizmos.DrawRay(playerCamera.transform.position, forward * sprayRange);
            
            Gizmos.color = Color.red;
            Vector3 endPoint = playerCamera.transform.position + forward * sprayRange;
            Gizmos.DrawWireSphere(endPoint, sprayRadius);
        }
    }
}

// Component to mark walls as sprayable
public class SprayableWall : MonoBehaviour
{
    [Header("Sprayable Settings")]
    public bool canBeSprayPainted = true;
    public float maxGraffitiCoverage = 1f; // 0-1, how much of the wall can be covered
    
    private float currentCoverage = 0f;
    
    public bool CanAcceptMoreGraffiti()
    {
        return canBeSprayPainted && currentCoverage < maxGraffitiCoverage;
    }
    
    public void AddCoverage(float amount)
    {
        currentCoverage = Mathf.Min(currentCoverage + amount, maxGraffitiCoverage);
    }
}

// Color picker UI for spray paint
public class GraffitiColorPicker : MonoBehaviour
{
    [Header("Color Options")]
    public Color[] availableColors = {
        Color.red, Color.blue, Color.green, Color.yellow,
        Color.magenta, Color.cyan, Color.white, Color.black
    };
    
    public KeyCode colorPickerKey = KeyCode.C;
    public GameObject colorPickerUI;
    
    private GraffitiSystem graffitiSystem;
    private bool isColorPickerOpen = false;
    
    void Start()
    {
        graffitiSystem = GetComponent<GraffitiSystem>();
        
        if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(colorPickerKey))
        {
            ToggleColorPicker();
        }
    }
    
    void ToggleColorPicker()
    {
        isColorPickerOpen = !isColorPickerOpen;
        
        if (colorPickerUI != null)
            colorPickerUI.SetActive(isColorPickerOpen);
            
        // Pause/unpause game or lock cursor
        Time.timeScale = isColorPickerOpen ? 0f : 1f;
        Cursor.lockState = isColorPickerOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    public void SelectColor(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < availableColors.Length)
        {
            graffitiSystem.ChangeSprayColor(availableColors[colorIndex]);
            ToggleColorPicker();
        }
    }
}