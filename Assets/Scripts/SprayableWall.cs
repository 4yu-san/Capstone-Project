using UnityEngine;

// Component to mark walls as sprayable
public class SprayableWall1 : MonoBehaviour
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