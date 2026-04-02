using UnityEngine;
using UnityEngine.InputSystem;

public class mouse1Controller : MonoBehaviour
{
public Camera mainCamera;
public float heightOffset = 0.5f;

void Update()
{
if (mainCamera == null)
{
mainCamera = Camera.main;
}

if (Mouse.current == null)
{
Debug.Log("No mouse detected.");
return;
}

if (mainCamera == null)
{
Debug.Log("No camera assigned.");
return;
}

Vector2 mousePosition = Mouse.current.position.ReadValue();
Debug.Log("Mouse position: " + mousePosition);

Ray ray = mainCamera.ScreenPointToRay(mousePosition);

if (Physics.Raycast(ray, out RaycastHit hit, 100f))
{
Debug.Log("Ray hit: " + hit.collider.name);

Vector3 targetPosition = hit.point;
targetPosition.y = heightOffset;
transform.position = targetPosition;
}
else
{
Debug.Log("Ray hit nothing.");
}
}
}