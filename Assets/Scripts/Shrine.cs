using UnityEngine;

public class Shrine : MonoBehaviour
{
    public GameObject[] orbs; // Array to store multiple orbs
    public GameObject activateKey;
    public GameObject explanationBanner;
    public GameObject particles;

    public bool usingShrine = false;
    public bool shrineCellSelection = false;
    public bool shrineEmpty = false;

    public int maxCharges = 3; // Maximum number of charges
    public int currentCharges;

    void Start()
    {
        currentCharges = maxCharges; // Initialize current charges

        if (activateKey != null)
        {
            activateKey.SetActive(false);
        }

        if (explanationBanner != null)
        {
            explanationBanner.SetActive(false);
        }

        SetOrbsActive(true); // Ensure all orbs are active at the start
    }

    void Update()
    {
        if (shrineEmpty)
        {
            SetOrbsActive(false);
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            SetOrbsActive(true);
            CancelShrineSelection();

            currentCharges = maxCharges;

            shrineEmpty = false;
            usingShrine = false;
            shrineCellSelection = false;

            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }

            if (explanationBanner != null)
            {
                explanationBanner.SetActive(false);
            }
        }

        if (usingShrine && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelShrineSelection();
        }

        if (currentCharges <= 0)
        {
            shrineEmpty = true;
        }

        if (shrineEmpty)
        {
            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }

            if (explanationBanner != null)
            {
                explanationBanner.SetActive(false);
            }

            if (particles != null)
            {
                particles.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            usingShrine = true;

            Debug.Log("CollidingPlayer");
            if (activateKey != null)
            {
                activateKey.SetActive(true);
            }

            if (explanationBanner != null)
            {
                explanationBanner.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            usingShrine = false;

            Debug.Log("ExitCollidingPlayer");
            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }

            if (explanationBanner != null)
            {
                explanationBanner.SetActive(false);
            }
        }
    }

    public bool HasCharges()
    {
        return currentCharges > 0;
    }

    public void UseCharge()
    {
        if (currentCharges > 0)
        {
            currentCharges--;
            UpdateOrbs();
        }
    }

    private void SetOrbsActive(bool active)
    {
        foreach (GameObject orb in orbs)
        {
            orb.SetActive(active);
        }
    }

    private void UpdateOrbs()
    {
        for (int i = 0; i < orbs.Length; i++)
        {
            if (i < currentCharges)
            {
                orbs[i].SetActive(true);
            }
            else
            {
                orbs[i].SetActive(false);
            }
        }
    }

    public void CancelShrineSelection()
    {
        shrineCellSelection = false;

        if (activateKey != null)
        {
            activateKey.SetActive(false);
        }

        if (explanationBanner != null)
        {
            explanationBanner.SetActive(false);
        }

        if (particles != null)
        {
            particles.SetActive(false);
        }

        Debug.Log("Shrine cell selection cancelled");
    }
}
