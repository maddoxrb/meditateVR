using UnityEngine;
using System.Collections.Generic;
using Fusion;
using TMPro;
using Oculus.Interaction;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(InteractableUnityEventWrapper))]
public class NetworkInteractableUnityEventWrapper : NetworkBehaviour
{
    private InteractableUnityEventWrapper interactable;
    [Header("Reference to Ray Interactable object")]
    public RayInteractable rayInteractable;
    [SerializeField] private MeshRenderer targetRenderer;
    [SerializeField] private TMP_Text txtInfo;
    private Color _originalColor;
    private static readonly Dictionary<PlayerRef, Color> _playerColors = new();

    private void Awake()
    {
        interactable = GetComponent<InteractableUnityEventWrapper>();
        interactable.InjectInteractableView(rayInteractable);

        if (targetRenderer != null)
            _originalColor = targetRenderer.material.color;
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.WhenHover.AddListener(OnHoverEntered);
            interactable.WhenUnhover.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.WhenHover.RemoveListener(OnHoverEntered);
            interactable.WhenUnhover.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered()
    {
        // Get the local player's PlayerRef
        var localPlayer = Runner.LocalPlayer;
        if (localPlayer == PlayerRef.None) return;

        // Make sure this player has a random color assigned
        if (!_playerColors.ContainsKey(localPlayer))
            _playerColors[localPlayer] = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);

        // Request StateAuthority so this player temporarily owns the cube
        if (!Object.HasStateAuthority)
            Object.RequestStateAuthority();

        // Broadcast this player's color to everyone
        RpcSetColor(_playerColors[localPlayer]);
    }

    private void OnHoverExited()
    {
        // Only the current owner should reset the color
        if (Object.HasStateAuthority)
        {
            RpcSetColor(_originalColor);
            // Optional: relinquish authority back to the host
            Object.ReleaseStateAuthority();
        }
    }

    // Runs on all peers to update the material color
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcSetColor(Color c)
    {
        if (targetRenderer) targetRenderer.material.color = c;
        if (txtInfo) txtInfo.text = c.Equals(_originalColor) ? "Touch me!" : "";
    }
}
