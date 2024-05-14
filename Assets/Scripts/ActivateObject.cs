using UnityEngine;

public class ActivateObject : MonoBehaviour
{
    public GameObject objectToActivate;
    public float number = .5f;

    private void Start()
    {
        // Generate a random number between 0 and 1
        float randomValue = Random.value;

        if (randomValue <= number)
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
