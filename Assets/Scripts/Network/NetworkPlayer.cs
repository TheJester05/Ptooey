using System;
using Fusion;
using Network;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Renderer _meshRenderer;

    [Header("Networked Properties")]
    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkObject HeldIngredient { get; set; }

    [SerializeField] private float reachDistance = 2f;
    [SerializeField] private Transform holdPoint;

    #region Interpolation Variables
    private Vector3 _lastKnownPosition;
    [SerializeField]private float _lerpSpeed = 3f;
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
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        // Basic Movement
        this.transform.position +=
            new Vector3(input.InputVector.normalized.x,
                0,
                input.InputVector.normalized.y)
            * Runner.DeltaTime * 5f; // Added speed multiplier

        
        // Interaction Logic
        if (input.InteractInput)
        {
            if (HeldIngredient == null)
            {
                TryPickup();
            }
            else
            {
                TryDrop();
            }
        }

       
    }

    private void TryPickup()
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
                return;
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
                return;
            }
        }
    }

    public void TryDrop()
    {
        if (HeldIngredient == null) return;

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
                    return;
                }
            }
        }

        // Drop on ground if not added to pot
        var ingredientComp = HeldIngredient.GetComponent<Core.Ingredient>();
        if (ingredientComp != null)
        {
            ingredientComp.SetHeld(PlayerRef.None, false);
        }
        HeldIngredient = null;
    }

    public override void Render()
    {
        if (_meshRenderer != null && _meshRenderer.material.color != PlayerColor)
        {
            _meshRenderer.material.color = PlayerColor;
        }

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
