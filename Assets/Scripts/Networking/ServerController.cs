using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class ServerController : MonoBehaviour
{
    public bool startServer = false;
    public int serverPort = 10069;
    Socket socket;
    SocketFlags socketFlags = SocketFlags.None;
    bool serverActive = true;

    public bool testSend = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(startServer)
        {
            StartServer();
            startServer = false;
        }

        if(testSend)
        {
            TestSend();
            testSend = false;
        }
    }
    
    void TestSend()
    {
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,ProtocolType.Udp);

        IPAddress serverAddr = IPAddress.Parse("192.168.1.100");

        IPEndPoint endPoint = new IPEndPoint(serverAddr, serverPort);

        string text = "Test UDP Message!";
        byte[] send_buffer = Encoding.ASCII.GetBytes(text);

        sock.SendTo(send_buffer, endPoint);

        Debug.Log("Test Message Sent");
    }

    void StartServer()
    {
        Debug.Log("Starting server...");

        serverActive = true;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, serverPort));

        BeginReceive();
    }

    void BeginReceive()
    {
        MessageObject receiveObject = new MessageObject();
        receiveObject.sender = new IPEndPoint(IPAddress.Any, 0);
        socket.BeginReceiveFrom(receiveObject.buffer, 0, receiveObject.buffer.Length, 0, ref receiveObject.sender, new AsyncCallback(EndReceive), receiveObject);
    }

    void EndReceive(IAsyncResult ar)
    {
        String data = String.Empty;

        MessageObject receiveObject = (MessageObject)ar.AsyncState;

        int bytesRead = socket.EndReceiveFrom(ar, ref receiveObject.sender);

        if(bytesRead > 0)
        {
            data = Encoding.ASCII.GetString(receiveObject.buffer, 0, bytesRead);
            Debug.Log("Server Received:" + data + " From " + receiveObject.sender.ToString());
        }

        if (serverActive)
        {
            BeginReceive();
        }
    }

    void BeginSend()
    {

    }

    void EndSend()
    {

    }

    void Close()
    {
        socket.Close();
        serverActive = false;
        Debug.Log("Server closed...");
    }
}
