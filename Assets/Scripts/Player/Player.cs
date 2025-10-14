using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public InputSystem_Actions inputActions;
    public PlayerMovement playerMovement;
    public PlayerAnimationController playerAnimationController; 

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();   
        playerMovement = GetComponent<PlayerMovement>();   
        playerAnimationController = GetComponentInChildren<PlayerAnimationController>();  
    }
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }
    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();  
    }
}

