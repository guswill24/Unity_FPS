using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float playerSpeed = 7.5f;
    public float gravity = 9.81f;
    public float jumpHeight = 3.0f;
    public Transform groundCheck;
    public LayerMask groundMask;

    private float horizontalInput;
    private float verticalInput;
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float groundDistance = 0.35f;
    private bool isAbleToJump; //Variable para abilitar el salto

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;
        ReadInput();
        CheckGround();
        Movement();
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        //Debug.Log(isGrounded);
    }

    private void ReadInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        //GetButtonDown: Detecta si tiene presionada una tecla
        if (Input.GetButtonDown("Jump"))
        {
            isAbleToJump = true;
        }
        else
        {
            isAbleToJump = false;
        }
    }

    private void Movement()
    {
        // Resetear velocidad vertical si está en el suelo
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // Movimiento en plano XZ
        Vector3 forwardMovement = transform.forward * verticalInput;
        Vector3 rightMovement = transform.right * horizontalInput;
        Vector3 movementDirection = Vector3.ClampMagnitude(forwardMovement + rightMovement, 1.0f);

        characterController.Move(movementDirection * playerSpeed * Time.deltaTime);

        // Saltar cuando isAbleToJump es verdadero y el personaje este en el suelo (isGrounded)
        if (isAbleToJump && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravity);
            Debug.Log("Saltar");
        }

        // Aplicar gravedad
        velocity.y -= gravity * Time.deltaTime;

        // Movimiento vertical
        characterController.Move(velocity * Time.deltaTime);
    }
}
