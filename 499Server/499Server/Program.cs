using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace _499Server
{
    class Program
    {
        //Global List of Clients
        private static List<Socket> clientList;// = new List<Socket>();

        //Global Variable for Server's Socket
        private static Socket listenSocket;// = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //Global Buffer
        private static byte[] buffr;// = new byte[1024];

        //A dictionary for storing files along with the client that owns them
        private static Dictionary<string, List<Socket>> fileDict;

        //A static variable for showing what port the server is operating on
        private static int portNum;
        //Listens for Clients, each time a new client connects, give it a thread and serve it
        //WE have a thread to listen for clients, each time a client connects, we give it a thread

        /*  THINGS TO THINK ABOUT/TO DO

            Send is synchronous, BeginSend is Asynchronous
                Send is faster overall, but BeginSend is for when you want the server to do something else while it is sending data
            
            How to determine what data will be revieved?
                Create and Call seperate functions when we anticipate specific data
                Ex: After client calls get: something, it can call GetFileFromClient which connects to a client, sends the request, and anticipates file transfer data

            Thread
                We will need seperate threads for various tasks, but for what?
                A new thread for each client that connects?
                A new thread in clients for connecting to another client for file transfer?

            Clients Sending Files
                Clients should maybe send all files at once?
                A list of files, each relating to the same socket
                This would mean when a new client connects, we look at the list given, check our dictionary for the key, if it exists, add the socket to the list

            Serialization of Objects

            Need to remove clients and their files from dictionary when a client disconnects
            Check to see if other clients have a file before removing file
         */

        static void Main(string[] args)
        {
            Console.Title = "Server";
            InitServer();

            //Ensure the Console does not close right after the window opens
            Console.ReadLine();
            CloseSockets();
        }

        private static void CloseSockets()
        {
            foreach(Socket socket in clientList)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            listenSocket.Close();
        }

        private static void InitServer()
        {
            //Display to console we are initializing server
            Console.WriteLine("Initializing Server...");

            //Set up global variables
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientList = new List<Socket>();
            buffr = new byte[1024];
            fileDict = new Dictionary<string, List<Socket>>();
            portNum = 100;

            //Get Connection Information
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine("The Host Name is: " + Dns.GetHostName());
            IPAddress ipAddress = getLocalIP();
            Console.WriteLine("The IP Address is: " + ipAddress.ToString());
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 100);

            //Attempt to begin listening for Traffic
            try
            {
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(5);
                //Thread listenLoop = new Thread(new ThreadStart(ListenLoop));
                listenSocket.BeginAccept(AcceptCallback, null);
                Console.WriteLine("Server Initialized.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        
        private static void AcceptCallback(IAsyncResult IAR)
        {
            Socket clientSocket;//= listenSocket.EndAccept(IAR);
            try
            {
                clientSocket = listenSocket.EndAccept(IAR);
            }
            catch(ObjectDisposedException)
            {
                return;
            }

            clientList.Add(clientSocket);
            Console.WriteLine("Client has Connected...");
            clientSocket.BeginReceive(buffr, 0, buffr.Length, SocketFlags.None, RecvCallBack, clientSocket);
            listenSocket.BeginAccept(AcceptCallback, null);

        }

        private static void RecvCallBack(IAsyncResult IAR)
        {
            Socket socket = (Socket)IAR.AsyncState;
            int recv;
            try
            {
                recv = socket.EndReceive(IAR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");
                socket.Close();
                clientList.Remove(socket);
                return;
            }

            
            byte[] tempBuffr = new byte[recv];
            Array.Copy(buffr, tempBuffr, recv);

            int listenPort = Int32.Parse(Encoding.ASCII.GetString(tempBuffr));
            Console.WriteLine("Socket is listening on Port: " + listenPort);

            string text = Encoding.ASCII.GetString(tempBuffr);
            Console.WriteLine("Text Recieved: " + text);

            char[] delimiterChars = {':'};
            string[] words = text.Split(delimiterChars);

            byte[] data;
            string input = words[0].ToLower();
            switch (input)
            {
                case "getserverip":
                    Console.WriteLine("Attempting to Get IP Address...");
                    string ipAddress = getLocalIP().ToString();
                    data = Encoding.ASCII.GetBytes(ipAddress);
                    socket.Send(data);
                    Console.WriteLine("Local IP Address is: " + ipAddress);
                    Console.WriteLine("Local IP Address Sent.\n");
                    break;
                case "getserverport":
                    Console.WriteLine("Attempting to Get Server Port...");
                    data = Encoding.ASCII.GetBytes(portNum.ToString());
                    socket.Send(data);
                    Console.WriteLine("Port Number is " + portNum.ToString());
                    Console.WriteLine("Port Number Sent");
                    break;
                case "getservertime":
                    Console.WriteLine("Sending Time...");
                    data = Encoding.ASCII.GetBytes(DateTime.Now.ToLongTimeString());
                    socket.Send(data);
                    Console.WriteLine("Time Sent\n");
                    break;
                case "add":
                    Console.WriteLine("Attempting to Add File {0} to List of Files", words[1]);
                    //When we go to Add the file to the dictionary, we first check to see if the file is already there
                    if (fileDict.ContainsKey(words[1]))
                    {
                        //If the File is already there, we Enqueue the Socket associated with it
                        fileDict[words[1]].Add(socket);
                    }
                    else
                    {
                        //If it isnt, the Key is added along with a new instance of a Socket Queue which we can access to add the socket
                        fileDict.Add(words[1], new List<Socket>());
                        fileDict[words[1]].Add(socket);
                    }
                    data = Encoding.ASCII.GetBytes("Added File");
                    socket.Send(data);
                    Console.WriteLine("Added File {0} to List of Files\n", words[1]);
                    break;
                case "get":
                    Console.WriteLine("Attempting to get Information for File {0}", words[1]);
                    Socket fileSocket;
                    string textToSend;
                    if (fileDict.ContainsKey(words[1]))
                    {
                        fileSocket = fileDict[words[1]][0];
                        IPEndPoint remoteIP = fileSocket.LocalEndPoint as IPEndPoint;
                        textToSend = "The client is located at:" + remoteIP.Address + ":" + remoteIP.Port;

                    }
                    else
                    {
                        textToSend = "File Could not be Found";
                    }
                    Console.WriteLine(textToSend);
                    Console.WriteLine();
                    data = Encoding.ASCII.GetBytes(textToSend);
                    socket.Send(data);
                    break;
                case "exit":
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    clientList.Remove(socket);
                    Console.WriteLine("Client Disconnected\n");
                    return;
                default:
                    Console.WriteLine("Invalid Request");
                    data = Encoding.ASCII.GetBytes("Invalid request");
                    socket.Send(data);
                    Console.WriteLine("Data Sent\n");
                    break;
            }
            try {

                socket.BeginReceive(buffr, 0, 1024, SocketFlags.None, RecvCallBack, socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static string Removespace(string text)
        {
            Console.WriteLine("Text Before remove white space: " + text);
            text = string.Join("", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            Console.WriteLine("Text after remove White Space: " + text);
            return text;
        }
        private static IPAddress getLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            foreach(var ip in host.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            throw new Exception("Local IP Address Not Found");
        }
    }
}
