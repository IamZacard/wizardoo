using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PortalBehaviour : MonoBehaviour
{
    public float rotationSpeedZ = 180f;
    public float rotationSpeedXY = 60f;

    public float elapsedTime = 0f;
    public float totalDuration = 2f;
    public float xyRotationStart = 1f;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(TeleportCoroutine(other.gameObject));
        }
    }

    private IEnumerator TeleportCoroutine(GameObject player)
    {
        AudioManager.Instance.PlaySound(AudioManager.SoundType.PortalSound, 1f);

        while (elapsedTime < totalDuration)
        {
            if (elapsedTime < xyRotationStart)
            {
                player.transform.Rotate(0, 0, rotationSpeedZ * Time.deltaTime);
            }
            else
            {
                player.transform.Rotate(rotationSpeedXY * Time.deltaTime, rotationSpeedXY * Time.deltaTime, rotationSpeedZ * Time.deltaTime);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }
}
