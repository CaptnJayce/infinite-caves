using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 25f;

    void Update() {
        HandleInput();
    }

    void HandleInput() {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(moveX, moveY, 0).normalized;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}