using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace _499Client
{
    class Program
    {
        /* THINGS TO DO/THINK ABOUT
        
            Getting Files from other Clients
                we get the IP address and port when we call get file
                We need to intercept the client input and not send it
                call a function where a new socket is created, requests file
                Clients need a function to accept a new connection and send data
                Clients need to have a list of sockets connected to them
                Clients may need two thread: one to communicate with server, one to communicate with clients

            
        */


        private static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static void Main(string[] args)
        {
            Console.Title = "Client";
            //LoopConnect();
            //SendLoop();
            InitClient();
            Console.ReadLine();
            Exit();
        }

        private static void InitClient()
        {
            Console.WriteLine("Please enter the local IP of the machine you wish to connect to: ");
            string IPstring = Console.ReadLine();
            IPAddress serverIP;
            try
            {
                IPstring = "192.168.0.2";
                serverIP = IPAddress.Parse(IPstring);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                serverIP = IPAddress.Loopback;
            }

            LoopConnect(serverIP);
            SendLoop();
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            Environment.Exit(0);
        }

        private static void SendLoop()
        {
            Console.WriteLine("Type 'exit' to leave.");
            PrintIP();

            while (true)
            {
                //Send Request
                Console.Write("Enter a Request: ");
                string req = Console.ReadLine();
                string[] words = FormatText(req);
                //Check to see if Words is more than 2. If it is, wrong format

                if (words[0] == "exit")
                {
                    Exit();
                }
                else if (words[0] == "get ip")
                {
                    PrintIP();
                }
                else if (words[0] == "add")
                {
                    string path = Environment.CurrentDirectory.ToString();
                    Console.WriteLine("Current Directory is: " + path);

                    path = path + @"\" + words[1];
                    Console.WriteLine("File Path is: " + path);
                    Console.WriteLine(File.Exists(path) ? "File Exists" : "File Does Not Exist");

                    req = string.Join(":", words[0], words[1]);
                    Console.WriteLine("data to be sent looks like: " + req);
                    SendString(req);
                    RecvData();

                }
                else
                {
                    Console.WriteLine("data to be sent looks like: " + req);
                    SendString(req);
                    RecvData();
                                                                                   

                }

           
            }

        }

        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None); //exception  Specified argument was out of the range of valid values.
        }

        private static void RecvData()
        {
            //Recieve Data - Need to put into function
            byte[] recbuffer = new byte[1024];
            int rec = clientSocket.Receive(recbuffer, SocketFlags.None);
            if (rec == 0) return;
            byte[] data = new byte[rec];
            Array.Copy(recbuffer, data, rec);
            string thing = Encoding.ASCII.GetString(data);
            Console.WriteLine("Recieved: " + thing);
        }


        private static void LoopConnect(IPAddress serverIP)
        {
            int attempts = 0;

            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection Attempts: " + attempts);
                    clientSocket.Connect(serverIP, 100);
                }
                catch (SocketException s)
                {
                    Console.Clear();
                }

            }

            Console.Clear();
            Console.WriteLine("Connected");
        }

        private static string[] FormatText(string text)
        {
            text = text.ToLower();
            //Console.WriteLine("Text Before remove white space: " + text);
            text = string.Join("", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            //Console.WriteLine("Text after remove White Space: " + text);

            char[] delimiterChars = { ':' };
            string[] words = text.Split(delimiterChars);

            return words;
        }

        private static void PrintIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Console.WriteLine("Network Unavailable");
                return;
            }

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine("The Local IP is " + ip.ToString());
                    return;
                }
            }

            throw new Exception("Local IP Address Not Found");
        }
    }
}
