using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformTest : NetworkBehaviour
{
    private float rand;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("NetworkTransformTest spawned on server");
        }
        else
        {
            rand = UnityEngine.Random.Range(0f, 1f);
            Debug.Log("NetworkTransformTest spawned on client");
        }
    }

    /*private void Update()
    {
        if (IsServer)
        {
            float theta = Time.frameCount / 10.0f + rand;
            transform.position = new Vector3((float) Math.Cos(theta), 0.0f, (float) Math.Sin(theta));
        }
    }*/
}