using System;
using Fusion;
using Network;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Animator _animator;
    [SerializeField] private Camera playerCamera;


    [Header("Networked Properties")]
    [Networked] public NetworkAnimatorData AnimationData { get; set; }
    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkObject HeldIngredient { get; set; }

    [SerializeField] private float reachDistance = 2f;
    [SerializeField] private Transform holdPoint;

    #region Interpolation Variables
    private Vector3 _lastKnownPosition;
    [SerializeField]private float _lerpSpeed = 3f;

    // This tracks the 'previous' networked state para ma detect 
    // when a trigger counter (like JumpCount) has increased.
    private NetworkAnimatorData _lastVisibleData;
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        if (HasInputAuthority) // client
        {
            
        }

        if (HasStateAuthority) // server
        {
            PlayerColor = Random.ColorHSV();
        }

        if (Object.HasInputAuthority)
        {
            playerCamera.gameObject.SetActive(true);
        }

        else
        {
            playerCamera.gameObject.SetActive(false);
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        // 1. Handle Movement
        float speed = input.InputVector.magnitude;
        Vector3 moveDir = new Vector3(input.InputVector.normalized.x, 0, input.InputVector.normalized.y);
        this.transform.position += moveDir * Runner.DeltaTime * 5f;

        // 2. Local copy of animation data to update
        var data = AnimationData;
        data.Speed = speed;

        // 3. Refined Interaction Logic
        if (input.InteractInput)
        {
            if (HeldIngredient == null)
            {
                // If pickup is successful, increment the counter
                if (TryPickup())
                {
                    data.PickupCount++;
                }
            }
            else
            {
                // If drop/throw is successful, increment the counter
                if (TryDrop())
                {
                    data.ThrowCount++;
                }
            }
        }

        // 4. Update the "Holding" state every tick
        data.IsHoldingItem = HeldIngredient != null;

        // 5. Sync it back to the network
        AnimationData = data;
    }

    private bool TryPickup()
    {
        // Simple sphere cast to find ingredients or players to steal from
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
            
            // Stealing mechanic logic
            var otherPlayer = col.GetComponent<NetworkPlayer>();
            if (otherPlayer != null && otherPlayer != this && otherPlayer.HeldIngredient != null)
            {
                HeldIngredient = otherPlayer.HeldIngredient;
                var ingredientBeingStolen = HeldIngredient.GetComponent<Core.Ingredient>();
                if (ingredientBeingStolen != null)
                {
                    ingredientBeingStolen.SetHeld(Object.InputAuthority, true);
                }
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
                if (pot.TryAddIngredient(ingredient.Type, Object.InputAuthority)) // <-- pass correct player
                {
                    Runner.Despawn(HeldIngredient);
                    HeldIngredient = null;
                    return true;
                }
            }
        }

        // Drop on ground if not added to pot
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

        if (data.JumpCount != _lastVisibleData.JumpCount)
        {
            _animator.SetTrigger("Jump");
        }

        if (data.ThrowCount != _lastVisibleData.ThrowCount)
        {
            _animator.SetTrigger("Throw");
        }

        if (data.PickupCount != _lastVisibleData.PickupCount)
        {
            _animator.SetTrigger("PickUp");
        }

        _lastVisibleData = data;

        // Update held ingredient position locally for smooth movement
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
            this.PlayerColor = color;
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string color)
    {
        if (HasStateAuthority)
        {
            this.PlayerName = color;
        }
        //example of how to use string
        //this.PlayerName.ToString();
    }

    #endregion
    
    #region Unity Callbacks

    private void Update()
    {
        if(!HasInputAuthority) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            var randColor = Random.ColorHSV();
            RPC_SetPlayerColor(randColor);
        }
    }
    
    #endregion
    
}
