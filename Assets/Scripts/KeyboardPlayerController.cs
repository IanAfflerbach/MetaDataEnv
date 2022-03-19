using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class KeyboardPlayerController : MonoBehaviour
{
    public GameObject head;

    private readonly float moveMod = 0.01f;
    private Vector2 m_Move = Vector2.zero;
    private readonly float rotMod = 0.5f;
    private Vector2 m_Look = Vector2.zero;
    private readonly float flyMod = 0.01f;
    private float m_Fly = 0.0f;

    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
    }

    public void Update()
    {
        Vector3 pos = transform.position;
        pos += head.transform.forward * m_Move.y * moveMod;
        pos += transform.right * m_Move.x * moveMod;
        transform.position = new Vector3(pos.x, pos.y + m_Fly * flyMod, pos.z);

        head.transform.Rotate(new Vector3(-m_Look.y, 0, 0) * rotMod);
        transform.Rotate(new Vector3(0, m_Look.x, 0) * rotMod);
    }

    public void OnMove(InputValue value)
    {
        m_Move = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        m_Look = value.Get<Vector2>();
    }

    public void OnFly(InputValue value)
    {
        m_Fly = value.Get<float>();
    }
}
