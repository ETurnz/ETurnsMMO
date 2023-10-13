using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClient2 : MonoBehaviour
{
    private NetworkManager networkManager;
    private TcpClient tcpClient;
    private UdpClient udpClient;
    private NetworkStream stream;
    private Task udpListeningTask;

    private bool isConnected = false;
    private bool isDisconnecting = false;

    private const string serverAddress = "127.0.0.1";
    //private const string serverAddress = "34.16.158.236";
    private const int serverTcpPort = 8000;
    private const int serverUdpPort = 8001;
    private CancellationTokenSource udpCancellationTokenSource;
    private CancellationTokenSource tcpCancellationTokenSource;



    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        udpClient = new UdpClient();
        udpCancellationTokenSource = new CancellationTokenSource();
        tcpCancellationTokenSource = new CancellationTokenSource();

        networkManager.isHandshakeSuccessful = false;

        ConnectToServer(serverAddress, serverTcpPort);
        udpListeningTask = ListenForUdpResponses();


        //Once network is initalized send a message to the server asking for a unique id
        PacketSerialization.BaseMessage baseMessage = new PacketSerialization.BaseMessage();
        baseMessage.messageType = "NEW_CLIENT_REQUEST_ID";
        string json = JsonUtility.ToJson(baseMessage);
        SendTcpMessageToServer(json);
    }
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.S))
        {
            SendTcpMessageToServer("Tcp yo");
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            SendUdpMessage("Udp yo");
        }*/
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    public void ConnectToServer(string ip, int port)
    {
        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse(serverAddress), serverTcpPort);

            stream = tcpClient.GetStream();
            isConnected = true;

            Thread listeningThread = new Thread(new ThreadStart(ReceiveMessages));
            listeningThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Error connecting to server: " + e.Message);
        }
    }

    public async void SendTcpMessageToServer(string message)
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Disconnect();
            Debug.LogError("Not connected to the server.");
            return;
        }

        if (isConnected)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            byte[] lengthBuffer = BitConverter.GetBytes(messageBuffer.Length);

            try
            {
                await stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
                await stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
                //Debug.Log("Sent " + message + " to server.");
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending message: " + e.Message);
                Disconnect();
            }
        }
    }


    private async Task ListenForUdpResponses()
    {
        while (!udpCancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                IPEndPoint clientEndPoint = result.RemoteEndPoint;
                string receivedText = Encoding.UTF8.GetString(data);

                networkManager.ParseUdpMessage(receivedText);
                Debug.Log("Received UDP message: " + receivedText);
            }
            catch (OperationCanceledException e)
            {
                Debug.LogError(e.Message);
                //Disconnect();
                // This exception is thrown when the task is canceled, just break the loop
            }
            catch (Exception e)
            {
                // Handle other potential exceptions
                Debug.LogError(e.Message);
                //Disconnect();
            }
        }
    }

    public void SendUdpMessage(string message)
    {
        if (isConnected)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                udpClient.Send(data, data.Length, serverAddress, serverUdpPort);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error sending UDP message: " + ex.Message);
            }
        }
    }

    private void ReceiveMessages()
    {
        byte[] lengthBuffer = new byte[4];

        while (isConnected && !tcpCancellationTokenSource.Token.IsCancellationRequested)
        {
            int bytesRead;

            try
            {
                // Read message length
                bytesRead = stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                if (bytesRead == 0) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] messageBuffer = new byte[messageLength];

                bytesRead = stream.Read(messageBuffer, 0, messageLength);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(messageBuffer);
                networkManager.ParseTcpMessage(message);
                Debug.Log("Received from server: " + message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Disconnect();
                // Handle server disconnecting or other issues
                break;
            }
        }

        Disconnect();
    }


    public async void Disconnect()
    {
        if (!isDisconnecting)
        {
            try
            {
                isDisconnecting = true;
                if (!isConnected) return;
                isConnected = false;

                networkManager.RequestDisconnect();

                if (tcpCancellationTokenSource != null)
                {
                    tcpCancellationTokenSource.Cancel();
                    tcpCancellationTokenSource.Dispose();
                    tcpCancellationTokenSource = null;
                }

                if (udpCancellationTokenSource != null)
                {
                    udpCancellationTokenSource.Cancel();
                    udpCancellationTokenSource.Dispose();
                    udpCancellationTokenSource = null;
                }

                if (stream != null)
                {
                    await stream.FlushAsync();
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }

                udpClient?.Close();
                udpClient?.Dispose();

                MainThreadDispatcher.Enqueue(() => {
                    GameObject.Destroy(networkManager.gameObject);
                    SceneManager.LoadScene("Main Menu");
                });

                Debug.Log("Disconnect successful");

                
            }
            catch (Exception e)
            {
                Debug.LogError("Error encountered when disconnecting: " + e.Message);
            }
        }
        
    }
}
