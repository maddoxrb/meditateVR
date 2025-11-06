using Fusion;
using UnityEngine;
using System.Collections.Generic;
using Photon.Voice.Unity;
using Photon.Voice.Fusion;

[RequireComponent(typeof(NetworkRunner))]
[RequireComponent(typeof(NetworkEvents))]
[RequireComponent(typeof(Recorder))]
[RequireComponent(typeof(VoiceLogger))]
[RequireComponent(typeof(FusionVoiceClient))]
public class FusionPlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Prefab to Spawn")]
    public NetworkObject PlayerPrefab;
    public Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    public void Start()
    {
        var fvc = GetComponent<FusionVoiceClient>();
        var rec = GetComponent<Recorder>();
        if (fvc && rec) fvc.PrimaryRecorder = rec;
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Spawn only for yourself on your own machine.
        if (player != Runner.LocalPlayer)
            return;

        // Guard: never spawn for an invalid/None player
        if (player == PlayerRef.None)
        {
            Debug.LogWarning("Skipping spawn: PlayerRef.None");
            return;
        }

        // Guard: if something already set a player object, don't double-spawn
        if (Runner.GetPlayerObject(player) != null)
        {
            Debug.LogWarning($"Player object already exists for {player}, skipping spawn.");
            return;
        }

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // Give input authority to the joining player
        var obj = Runner.Spawn(PlayerPrefab, spawnPos, spawnRot, inputAuthority: player);

        // Register mapping so all peers can discover it
        Runner.SetPlayerObject(player, obj);

        _spawnedPlayers[player] = obj;

        Debug.Log($"Spawned local player for {player}. InputAuth={obj.InputAuthority}, StateAuth={obj.StateAuthority}");
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Let the server clean up so we don't rely on a disconnecting client
        if (Runner.IsServer)
        {
            var obj = Runner.GetPlayerObject(player);
            if (obj)
            {
                Runner.Despawn(obj);
            }
        }
        _spawnedPlayers.Remove(player);
    }
}
