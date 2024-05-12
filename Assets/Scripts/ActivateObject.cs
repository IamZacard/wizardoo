using UnityEngine;

public class ActivateObject : MonoBehaviour
{
    public GameObject objectToActivate;

    private void Start()
    {
        // Generate a random number between 0 and 1
        float randomValue = Random.value;

        if (randomValue <= 0.5f)
        {
            // Set the object to active
            objectToActivate.SetActive(true);
        }
        else
        {
            // Set the object to inactive
            objectToActivate.SetActive(false);
        }
    }
}
