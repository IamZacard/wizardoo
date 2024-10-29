using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] public float destructionDelay = 5f;

    private void Start()
    {
        Invoke("DestroyObject", destructionDelay);
    }

    private void DestroyObject()
    {
        Destroy(gameObject);
    }
}
