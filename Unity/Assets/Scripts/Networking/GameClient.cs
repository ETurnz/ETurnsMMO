using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class GameClient : MonoBehaviour
{
    private const string ServerIp = "127.0.0.1";
    private const int ServerPort = 8000;
    private const int ClientTcpListenPort = 8002; // new listening port

    private UdpClient _udpClient;
    private IPEndPoint _udpEndPoint;
    private TcpListener _tcpListener; // for listening to incoming TCP messages
    private NetworkManager networkManager;
    
    string uniqueID;
    private void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();

        try
        {
            _udpClient = new UdpClient(8001);
            _udpClient.Client.Blocking = false;

            _udpEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);

            StartCoroutine(ReceiveUdpMessages());

            // Start listening for incoming TCP messages
            _tcpListener = new TcpListener(IPAddress.Any, ClientTcpListenPort);
            _tcpListener.Start();
            StartCoroutine(ListenForTcpMessages());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing Client: {ex.Message}");
        }

        if (!networkManager.assignedID)
            SendTcpMessage("NEW_CLIENT_REQUEST_ID");
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.S)) // Press 'S' to send a test TCP message
            {
                SendTcpMessage("Hello, TCP Server");
            }

            if (Input.GetKeyDown(KeyCode.D)) // Press 'D' to send a test UDP message
            {
                SendUdpMessage("Hello, UDP Server!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in Update method: {ex.Message}");
        }
    }

    private IEnumerator ReceiveUdpMessages()
    {
        while (true)
        {
            try
            {
                if (_udpClient.Available > 0)
                {
                    byte[] data = _udpClient.Receive(ref _udpEndPoint);
                    string message = Encoding.ASCII.GetString(data);
                    Debug.Log($"Received UDP: {message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error receiving UDP message: {ex.Message}");
            }

            yield return null; // Continue next frame
        }
    }

    private IEnumerator ListenForTcpMessages()
    {
        while (true)
        {
            if (_tcpListener.Pending())
            {
                TcpClient client = _tcpListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                networkManager.ParseTcpMessage(message);

                Debug.Log($"Received TCP: {message}");
                client.Close();
            }
            yield return null;
        }
    }

    public void SendTcpMessage(string message)
    {
        try
        {
            using (TcpClient client = new TcpClient(ServerIp, ServerPort))
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);
                Debug.Log($"Sent TCP: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error sending TCP message: {ex.Message}");
        }
    }

    private void SendUdpMessage(string message)
    {
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            _udpClient.Send(data, data.Length, ServerIp, ServerPort);
            Debug.Log($"Sent UDP: {message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error sending UDP message: {ex.Message}");
        }
    }
    private void OnDestroy()
    {
        try
        {
            StopAllCoroutines(); // Stops the UDP and TCP receiving coroutines
            _udpClient?.Close();
            _tcpListener?.Stop();  // Properly close the TCP listener
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in OnDestroy method: {ex.Message}");
        }
    }
}
