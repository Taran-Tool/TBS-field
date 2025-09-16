using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class NetworkSync : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> ObjectTag = new();
    public NetworkVariable<int> ObjectLayer = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ObjectTag.Value = gameObject.tag;
            ObjectLayer.Value = gameObject.layer;
        }
        else
        {
            ApplyTagAndLayer();

            ObjectTag.OnValueChanged += OnTagChanged;
            ObjectLayer.OnValueChanged += OnLayerChanged;
        }
    }

    private void OnTagChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        gameObject.tag = current.ToString();
    }

    private void OnLayerChanged(int previous, int current)
    {
        gameObject.layer = current;
    }

    private void ApplyTagAndLayer()
    {
        gameObject.tag = ObjectTag.Value.ToString();
        gameObject.layer = ObjectLayer.Value;
    }
}
