using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        if (FindObjectsOfType<CameraController>().Length > 1)
        {
            Destroy(gameObject); // Only one camera exists
            return;
        }

        cam = GetComponent<Camera>();
        DontDestroyOnLoad(gameObject);
    }

    public void FocusOnGrid(int width, int height)
    {
        // Center the camera
        Vector3 center = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10f);
        transform.position = center;

        // Adjust orthographic size to fit the grid
        float aspect = (float)Screen.width / Screen.height;
        float sizeBasedOnHeight = height / 2f + 1f;
        float sizeBasedOnWidth = (width / 2f + 1f) / aspect;

        cam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth);
    }
}
