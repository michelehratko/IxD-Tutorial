using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardWandController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float fixedY = 0.5f;

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move.z += 1f;
        if (Keyboard.current.sKey.isPressed) move.z -= 1f;
        if (Keyboard.current.aKey.isPressed) move.x -= 1f;
        if (Keyboard.current.dKey.isPressed) move.x += 1f;

        move = move.normalized;

        transform.position += move * moveSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.y = fixedY;
        transform.position = pos;
    }
}