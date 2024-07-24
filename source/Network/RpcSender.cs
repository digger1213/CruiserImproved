using HarmonyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace CruiserImproved.Network;

//using BeginSendClientRpcFunc = Func<uint, ClientRpcParams, RpcDelivery, FastBufferWriter>;
//using EndSendClientRpcFunc = Func<uint, ClientRpcParams, RpcDelivery, FastBufferWriter>;

internal class RpcSender
{
    delegate FastBufferWriter BeginSendClientRpcDelegate(NetworkBehaviour behaviour, uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery);
    delegate void EndSendClientRpcDelegate(NetworkBehaviour behaviour, ref FastBufferWriter bufferWriter, uint rpcMethodId, ClientRpcParams clientRpcParams, RpcDelivery rpcDelivery);

    static BeginSendClientRpcDelegate __beginSendClientRpc =
        (BeginSendClientRpcDelegate) AccessTools.DeclaredMethod(typeof(NetworkBehaviour), "__beginSendClientRpc")
        .CreateDelegate(typeof(BeginSendClientRpcDelegate));

    static EndSendClientRpcDelegate __endSendClientRpc = 
        (EndSendClientRpcDelegate)AccessTools.DeclaredMethod(typeof(NetworkBehaviour), "__endSendClientRpc")
        .CreateDelegate(typeof(EndSendClientRpcDelegate));

    public delegate void LoadRpcBuffer(ref FastBufferWriter writer);

    //Send an existing Lethal Company ClientRpc to specific clients instead of everyone
    static public void SendClientRpc(NetworkBehaviour netBehaviour, uint rpcMethodId, IReadOnlyList<ulong> clients, LoadRpcBuffer bufferWrite)
    {
        ClientRpcParams param = new() { Send = { TargetClientIds = clients } };

        FastBufferWriter writer = __beginSendClientRpc(netBehaviour, rpcMethodId, param, RpcDelivery.Reliable);
        bufferWrite(ref writer);
        __endSendClientRpc(netBehaviour, ref writer, rpcMethodId, param, RpcDelivery.Reliable);
    }
}
