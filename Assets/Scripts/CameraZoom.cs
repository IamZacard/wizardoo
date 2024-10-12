using UnityEngine;
using Cinemachine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomedInSize = 5.0f;
    [SerializeField] private float zoomedOutSize = 10.0f;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private float minZoom = 5.0f;
    [SerializeField] private float maxZoom = 10.0f;
    [SerializeField] private Vector3 overallViewPosition = new Vector3(3.5f, 3.5f, -10); // Center of the scene

    private GameObject player;
    private bool isFollowingPlayer = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        SetOverallView();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2)) // Mouse wheel button is pressed
        {
            ToggleCameraView();
        }

        ZoomCamera();
    }

    private void ToggleCameraView()
    {
        if (isFollowingPlayer)
        {
            // Switch to overall view
            SetOverallView();
        }
        else
        {
            // Switch to follow player view and zoom in
            SetFollowPlayerView();
        }

        isFollowingPlayer = !isFollowingPlayer;
    }

    private void SetOverallView()
    {
        virtualCamera.m_Lens.OrthographicSize = zoomedOutSize;
        virtualCamera.Follow = null;
        transform.position = overallViewPosition;
    }

    private void SetFollowPlayerView()
    {
        if (player != null)
        {
            virtualCamera.m_Lens.OrthographicSize = zoomedInSize;
            virtualCamera.Follow = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found. Make sure the player object is tagged correctly.");
        }
    }

    private void ZoomCamera()
    {
        float scrollData = Input.GetAxis("Mouse ScrollWheel");

        if (scrollData != 0)
        {
            virtualCamera.m_Lens.OrthographicSize -= scrollData * zoomSpeed;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize, minZoom, maxZoom);
        }
    }
}
