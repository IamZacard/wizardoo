using UnityEngine;

public class ShowOnMobile : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(Application.isMobilePlatform);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
