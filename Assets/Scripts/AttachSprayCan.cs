using UnityEngine;

public class AttachSprayCan : MonoBehaviour
{
    [SerializeField] public GameObject sprayCan;
    [SerializeField] public Transform handTransform;

    void Start()
    {
        sprayCan.transform.SetParent(handTransform);
        sprayCan.transform.localPosition = Vector3.zero;
        sprayCan.transform.localRotation = Quaternion.identity;
        // Then apply offset if needed
        sprayCan.transform.localPosition = new Vector3(0f, 0f, 0f); // Example
    }
}

