using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class MessageObject
{
    public byte[] buffer = new byte[1024];
    public EndPoint sender;
    public IPPacketInformation packetInformation;
}
