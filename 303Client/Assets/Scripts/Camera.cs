using UnityEngine;

public class Camera : MonoBehaviour
{
    public Player player;
    public float sensitivity = 100f;
    public float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;


    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.localEulerAngles.y;
    }

    private void Update()
    {
        //Allows user to unlock thier mouse to type in chat
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorMode();
        }

        //Dont move the camera if the user is trying to use UI elements
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Look();
        }
    }

    private void Look()
    {
        //update camera rotation based on mouse position
        float mouseY = -Input.GetAxis("Mouse Y");
        float mouseX = Input.GetAxis("Mouse X");

        verticalRotation += mouseY * sensitivity * Time.deltaTime;
        horizontalRotation += mouseX * sensitivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    private void ToggleCursorMode()
    {
        Cursor.visible = !Cursor.visible;

        if (Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
