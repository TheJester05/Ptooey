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
    [Networked] private NetworkBool _lastInteractPressed { get; set; }
    [Networked] private NetworkBool _lastStealPressed { get; set; }

    [SerializeField] private float reachDistance = 2.5f;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Transform _rat1Mesh;

    [Header("Movement Settings")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 3f;

    private NetworkAnimatorData _lastVisibleData;

    public override void Spawned()
    {
        if (HasStateAuthority) PlayerColor = Random.ColorHSV();

        if (Object.HasInputAuthority)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camScript = mainCam.GetComponent<ThirdPersonCamera>();
                if (camScript != null) camScript.target = _rat1Mesh;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        var data = AnimationData;

        // 1. ROTATION & 2. MOVEMENT
        _yRotation += input.MouseX * mouseSensitivity;
        rb.rotation = Quaternion.Euler(0f, _yRotation, 0f);
        Vector3 moveDir = rb.rotation * new Vector3(input.InputVector.x, 0, input.InputVector.y);
        rb.linearVelocity = new Vector3(moveDir.normalized.x * moveSpeed, rb.linearVelocity.y, moveDir.normalized.z * moveSpeed);

        // 3. GRAVITY & JUMP
        if (IsGrounded())
        {
            if (input.JumpInput) { rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z); data.JumpCount++; }
            else rb.linearVelocity = new Vector3(rb.linearVelocity.x, -0.1f, rb.linearVelocity.z);
        }
        else rb.linearVelocity += Vector3.up * gravity * Runner.DeltaTime;

        // 4. INTERACTION & STEALING (The Fix)
        if (Object.HasInputAuthority && Runner.IsForward)
        {
            // E Key: Standard Pick/Drop/Pot
            bool interactJustPressed = input.InteractInput && !_lastInteractPressed;
            if (interactJustPressed)
            {
                NetworkObject target = FindNetworkObjectInFront();
                RPC_RequestInteraction(target, false);
            }

            // F Key: Steal
            bool stealJustPressed = input.StealInput && !_lastStealPressed;
            if (stealJustPressed)
            {
                NetworkObject target = FindNetworkObjectInFront();
                RPC_RequestInteraction(target, true);
            }

        }

        _lastInteractPressed = input.InteractInput;
        _lastStealPressed = input.StealInput;

        // 5. SYNC ANIMATIONS
        data.Speed = input.InputVector.magnitude;
        data.IsHoldingItem = HeldIngredient != null;
        AnimationData = data;
    }

    private NetworkObject FindNetworkObjectInFront()
    {
        // Sphere cast slightly in front of the rat
        Collider[] colliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, reachDistance);

        foreach (var col in colliders)
        {
            // FIX: Compare the root transform to ensure we aren't hitting any part of our own prefab
            if (col.transform.root == this.transform.root) continue;

            var no = col.GetComponent<NetworkObject>();
            if (no != null) return no;
        }
        return null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, TickAligned = true)]
    private void RPC_RequestInteraction(NetworkObject target, bool isStealAttempt)
    {
        // This runs on the SERVER (State Authority)

        if (isStealAttempt)
        {
            // STEAL LOGIC
            if (target == null || HeldIngredient != null) return;

            // Look for the NetworkPlayer component on the target
            var victim = target.GetComponent<NetworkPlayer>();
            if (victim != null && victim.HeldIngredient != null)
            {
                NetworkObject stolenItem = victim.HeldIngredient;

                // Perform the swap on the server
                victim.HeldIngredient = null;
                this.HeldIngredient = stolenItem;

                var ing = stolenItem.GetComponent<Core.Ingredient>();
                if (ing != null) ing.SetHeld(Object.InputAuthority, true);

                var d = AnimationData;
                d.PickupCount++;
                AnimationData = d;
            }
        }
        else
        {
            // STANDARD INTERACT (E)
            if (HeldIngredient != null)
            {
                // Try Pot
                if (target != null)
                {
                    var pot = target.GetComponent<Core.Pot>();
                    if (pot != null)
                    {
                        var ingredientComp = HeldIngredient.GetComponent<Core.Ingredient>();
                        if (pot.TryAddIngredient(ingredientComp.Type, Object.InputAuthority))
                        {
                            Runner.Despawn(HeldIngredient);
                            HeldIngredient = null;
                            return;
                        }
                    }
                }
                // Drop
                var ing = HeldIngredient.GetComponent<Core.Ingredient>();
                if (ing != null) ing.SetHeld(PlayerRef.None, false);
                HeldIngredient = null;
            }
            else if (target != null)
            {
                // Pickup from floor
                var floorIng = target.GetComponent<Core.Ingredient>();
                if (floorIng != null && !floorIng.IsHeld)
                {
                    HeldIngredient = target;
                    floorIng.SetHeld(Object.InputAuthority, true);
                    var d = AnimationData;
                    d.PickupCount++;
                    AnimationData = d;
                }
            }
        }
    }

    private bool IsGrounded() => Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.7f);

    public override void Render()
    {
        if (_meshRenderer != null) _meshRenderer.material.color = PlayerColor;

        _animator.SetFloat("Speed", AnimationData.Speed);
        _animator.SetBool("IsHolding", AnimationData.IsHoldingItem);

        if (AnimationData.JumpCount != _lastVisibleData.JumpCount) _animator.SetTrigger("Jump");
        if (AnimationData.ThrowCount != _lastVisibleData.ThrowCount) _animator.SetTrigger("Throw");
        if (AnimationData.PickupCount != _lastVisibleData.PickupCount) _animator.SetTrigger("PickUp");

        _lastVisibleData = AnimationData;

        if (HeldIngredient != null && holdPoint != null)
        {
            HeldIngredient.transform.position = holdPoint.position;
            HeldIngredient.transform.rotation = holdPoint.rotation;
        }
    }
}