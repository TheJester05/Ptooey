using System;
using Fusion;
using Network;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Animator _animator;

    [Header("Networked Properties")]
    [Networked] public NetworkAnimatorData AnimationData { get; set; }
    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkObject HeldIngredient { get; set; }
    [Networked] private float _yRotation { get; set; }

    [SerializeField] private float reachDistance = 2f;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Transform _rat1Mesh;

    [Header("Movement Settings")]
    private float _verticalVelocity;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 3f;

    #region Interpolation Variables
    private Vector3 _lastKnownPosition;
    [SerializeField] private float _lerpSpeed = 3f;
    private NetworkAnimatorData _lastVisibleData;
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            PlayerColor = Random.ColorHSV();
        }

        if (Object.HasInputAuthority)
        {
            // Find the main camera in the scene automatically
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                // Activate it (though it's usually always on in the scene)
                mainCam.gameObject.SetActive(true);

                // Link the camera script to this specific rat's interpolated mesh
                var camScript = mainCam.GetComponent<ThirdPersonCamera>();
                if (camScript != null)
                {
                    camScript.target = _rat1Mesh; // Assigned in your prefab edit mode
                }
            }

            // Lock cursor for your kitchen navigation
           // Cursor.lockState = CursorLockMode.Locked;
           // Cursor.visible = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Basic Checks
        if (!HasStateAuthority && !HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        var data = AnimationData;
        Rigidbody rb = GetComponent<Rigidbody>(); // Reference to the standard Rigidbody

        // 2. ROTATION: Update the rat's horizontal facing based on mouse movement
        _yRotation += input.MouseX * mouseSensitivity;

        // Apply the rotation to the Rigidbody so it syncs with physics
        Quaternion targetRotation = Quaternion.Euler(0f, _yRotation, 0f);
        rb.MoveRotation(targetRotation);

        // Also keep the transform in sync for the Interpolation Target
        transform.rotation = targetRotation;

        // 3. MOVEMENT DIRECTION: Calculate 'Forward' relative to where the rat is facing
        Vector3 moveDir = new Vector3(input.InputVector.x, 0, input.InputVector.y);
        moveDir = transform.TransformDirection(moveDir);
        moveDir.Normalize();

        // 4. ANIMATION DATA: Update speed for the animator
        float speed = input.InputVector.magnitude;
        data.Speed = speed;

        // 5. JUMPING & VERTICAL VELOCITY
        if (IsGrounded() && input.JumpInput)
        {
            // Apply a direct upward burst. 
            // We keep the current X and Z velocity so you can jump while running.
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            // Increment the networked jump count to trigger the animation in Render()
            data.JumpCount++;
        }

        // 6. APPLY MOVEMENT
        // We set horizontal velocity but let the Y velocity (gravity/jump) stay as it is.
        Vector3 moveVelocity = moveDir * moveSpeed;
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);

        // 6. APPLY PHYSICS VELOCITY: This is the most important change
        // We set velocity instead of adding to transform.position
        Vector3 velocity = moveDir * moveSpeed;
        rb.linearVelocity = new Vector3(velocity.x, _verticalVelocity, velocity.z);

        // 7. INTERACTION LOGIC
        if (input.InteractInput)
        {
            if (HeldIngredient == null)
            {
                if (TryPickup()) data.PickupCount++;
            }
            else
            {
                if (TryDrop()) data.ThrowCount++;
            }
        }

        data.IsHoldingItem = HeldIngredient != null;
        AnimationData = data;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.1f);
    }

    private bool TryPickup()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, reachDistance);
        foreach (var col in colliders)
        {
            var ingredient = col.GetComponent<Core.Ingredient>();
            if (ingredient != null && !ingredient.IsHeld)
            {
                HeldIngredient = ingredient.Object;
                ingredient.SetHeld(Object.InputAuthority, true);
                return true;
            }

            var otherPlayer = col.GetComponent<NetworkPlayer>();
            if (otherPlayer != null && otherPlayer != this && otherPlayer.HeldIngredient != null)
            {
                HeldIngredient = otherPlayer.HeldIngredient;
                var ingredientBeingStolen = HeldIngredient.GetComponent<Core.Ingredient>();
                if (ingredientBeingStolen != null)
                    ingredientBeingStolen.SetHeld(Object.InputAuthority, true);
                otherPlayer.HeldIngredient = null;
                return true;
            }
        }
        return false;
    }

    public bool TryDrop()
    {
        if (HeldIngredient == null) return false;

        Collider[] colliders = Physics.OverlapSphere(transform.position, reachDistance);
        foreach (var col in colliders)
        {
            var pot = col.GetComponent<Core.Pot>();
            if (pot != null)
            {
                var ingredient = HeldIngredient.GetComponent<Core.Ingredient>();
                if (pot.TryAddIngredient(ingredient.Type, Object.InputAuthority))
                {
                    Runner.Despawn(HeldIngredient);
                    HeldIngredient = null;
                    return true;
                }
            }
        }

        var ingredientComp = HeldIngredient.GetComponent<Core.Ingredient>();
        if (ingredientComp != null) ingredientComp.SetHeld(PlayerRef.None, false);

        HeldIngredient = null;
        return true;
    }

    public override void Render()
    {
        if (_meshRenderer != null && _meshRenderer.material.color != PlayerColor)
        {
            _meshRenderer.material.color = PlayerColor;
        }

        var data = AnimationData;

        _animator.SetFloat("Speed", data.Speed);
        _animator.SetBool("IsHolding", data.IsHoldingItem);
        _animator.SetBool("IsCrouching", data.IsCrouching);

        if (data.JumpCount != _lastVisibleData.JumpCount) _animator.SetTrigger("Jump");
        if (data.ThrowCount != _lastVisibleData.ThrowCount) _animator.SetTrigger("Throw");
        if (data.PickupCount != _lastVisibleData.PickupCount) _animator.SetTrigger("PickUp");

        _lastVisibleData = data;

        if (HeldIngredient != null && holdPoint != null)
        {
            HeldIngredient.transform.position = holdPoint.position;
            HeldIngredient.transform.rotation = holdPoint.rotation;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerColor(Color color)
    {
        if (HasStateAuthority)
        {
            PlayerColor = color;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string color)
    {
        if (HasStateAuthority)
        {
            PlayerName = color;
        }
    }
    #endregion

    #region Unity Callbacks
    private void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            var randColor = Random.ColorHSV();
            RPC_SetPlayerColor(randColor);
        }
    }
    #endregion
}