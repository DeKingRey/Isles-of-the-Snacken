using Unity.Netcode;
using UnityEngine;

public class PlayerAppearance : NetworkBehaviour
{
    [SerializeField] private Texture[] textures;

    private Renderer[] renderers;

    public NetworkVariable<int> TextureIndex = new(-1);

    public override void OnNetworkSpawn()
    {
        renderers = GetComponentsInChildren<Renderer>();
        TextureIndex.OnValueChanged += OnTextureChanged;

        // Assigns texture based on player ID
        if (IsServer)
        {
            int index = (int)(OwnerClientId % (ulong)textures.Length);
            TextureIndex.Value = index;

        }
        ApplyTexture(TextureIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        TextureIndex.OnValueChanged -= OnTextureChanged;
    }

    private void OnTextureChanged(int prev, int current)
    {
        ApplyTexture(current);
    }

    private void ApplyTexture(int index)
    {
        if (index < 0 || index >= textures.Length) return;

        foreach (Renderer r in renderers)
        {
            r.material.mainTexture = textures[index];
        }
    }
}