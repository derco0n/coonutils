using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Co0nUtilZ
{
    /// <summary>
    /// Klasse welchen einen TCP-Client darstellt
    /// Autor: Dennis Marx
    /// Version 0.1
    /// Letzte Änderung: 18.01.2018
    /// </summary>
    public class C_NetClient
    { 

        //private TcpClient tcpClient = null;
        private Socket mySocket;
        private int _port;
        private String _ip;

        //private Encoding m_encoding = Encoding.ASCII;
        private Encoding m_encoding = Encoding.UTF8;

        public delegate void ProgressEventHandler(object sender, ProgressEventArgs Args);
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Args);
        public event ProgressEventHandler DataReceived;
        public event ProgressEventHandler DataSent;
        public event ProgressEventHandler Connected;
        public event ProgressEventHandler Disconnected;
        public event ErrorEventHandler OnError;

        private System.Threading.Timer timer;

        /// <summary>
        /// Löst einen Hostnamen in eine IP auf
        /// </summary>
        /// <param name="Hostname"></param>
        /// <returns></returns>
        public static String IPbyHostname(String Hostname)
        {
            return Dns.GetHostAddresses(Hostname)[0].ToString();
        }


        private bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                /*if (this.mySocket != null)
                {
                    return this.mySocket.Connected;
                }

                return false;*/
                return this._isConnected;
            }


        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="Host">Zielhost</param>
        /// <param name="port">Zielport</param>
        public C_NetClient(String Host, int port)
        {
            this._port = port;
            try
            {
                IPAddress.Parse(Host); //Versuchen aus Host eine IP-Adresse zu parsen
                this._ip = Host; //Host ist eine IP
            }
            catch
            {
                this._ip = IPbyHostname(Host);
            }


        }



        /// <summary>
        /// Stellt eine Asynchrone-Verbindung mit dem angegebenen Server her
        /// </summary>
        public void ConnectToServer()
        {
            try
            {
                this.mySocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //IPAddress[] remoteHost = Dns.GetHostAddresses("hostaddress");

                //Start the async connect operation   

                this.mySocket.BeginConnect(
                    this._ip,
                    this._port,
                    new AsyncCallback(
                        ConnectCallback
                        ),
                    mySocket
                    );


            }
            catch (Exception ex)
            {
                /*  
                 *  
                 *  Logger.WriteLog(LogLevel.Error, "ex.Message);
                 *  
                 */

                if (this.OnError != null)
                {
                    this.OnError(this, new ErrorEventArgs("Fehler beim Verbindungsaufbau:\r\n" + ex.ToString()));
                }
            }
        }


        /// <summary>
        /// Prüft ob eine Verbindung noch aktiv ist
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool ConIsAlive(Socket socket)

        {
            bool myreturn = false;

            if (socket != null)
            {
                bool sockReadResult = socket.Poll(1, SelectMode.SelectRead);
                bool socKErrResult = socket.Poll(1, SelectMode.SelectError);
                bool sockConnected = socket.Connected;
                myreturn = !sockReadResult & !socKErrResult & sockConnected;
            }

            this._isConnected = myreturn;


            return myreturn;
        }

        /// <summary>
        /// Prüft durch den Timer getriggert, regelmäßig ob eine Verbindung noch besteht.
        /// </summary>
        /// <param name="state"></param>
        private void CheckConnection(object state)
        {
            if (this.mySocket == null || !ConIsAlive(this.mySocket)) //Wenn das Socket nicht initialisiert ist oder die Verbindung nicht mehr besteht.
            {
                this._isConnected = false;
                if (this.Disconnected != null)
                {
                    this.Disconnected(this, new ProgressEventArgs(0, "Verbindung wurde Server-/Netzwerkseitig getrennt."));
                }
                this.timer.Dispose();
            }

        }

        /// <summary>
        /// Verbindung zum Server trennen
        /// </summary>
        public void Disconnect()
        {
            if (this.mySocket != null && this.mySocket.Connected)
            {
                this.mySocket.Shutdown(SocketShutdown.Both); //Sämtliche aktiven Übertragungen beenden

                this.mySocket.Disconnect(false); //Das Socket trennen und nicht wieder benutzen (Dispose())
                if (!this.mySocket.Connected)
                {
                    this._isConnected = false;

                    if (this.Disconnected != null)
                    {
                        this.Disconnected(this, new ProgressEventArgs(10, "Verbindung wurde Clientseitig getrennt."));
                    }
                }

            }
        }

        /// <summary>
        /// Sendet einen String (Asynchron) an den Server
        /// </summary>
        /// <param name="Message"></param>
        public void SendData(String Message)
        {

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = this.m_encoding.GetBytes(Message);

            // Begin sending the data to the remote device.

            if (this.ConIsAlive(this.mySocket))
            {
                this.mySocket.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), this.mySocket);
            }
        }

        /// <summary>
        /// Call for Connection
        /// </summary>
        /// <param name="result"></param>
        private void ConnectCallback(IAsyncResult result)
        {

            // if (this._isConnected)
            if (this.mySocket != null && this.ConIsAlive(this.mySocket))
            {
                try
                {
                    //We are connected successfully.

                    NetworkStream networkStream = new NetworkStream(this.mySocket);

                    byte[] buffer = new byte[this.mySocket.ReceiveBufferSize];

                    //Now we are connected start asyn read operation.

                    networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);

                    if (this.Connected != null)
                    {
                        this.Connected(this, new ProgressEventArgs(30, "Verbunden."));
                    }


                    if (this.mySocket != null)
                    {
                        //Verbunden
                        this._isConnected = this.mySocket.Connected;

                        if (this._isConnected)
                        {
                            TimerCallback tick = new TimerCallback(CheckConnection); //Bei jedem Timertick die Methode "CheckAndDoWork" aufrufen
                            this.timer = new System.Threading.Timer(tick, null, 0, 1000); //Neuen Timer Initialisieren - Intervall in ms
                        }
                    }

                }

                catch (Exception ex)

                {
                    if (this.OnError != null)
                    {
                        this.OnError(this, new ErrorEventArgs("Fehler beim Verbindungsaufbau (Callback).\r\n" + ex.ToString()));
                    }
                }
            }
            else
            {
                if (this.OnError != null)
                {
                    this.OnError(this, new ErrorEventArgs(
                        "Die Verbindung konnte nicht hergestellt werden. Ist der Server erreichbar?"
                            )
                        );
                }
            }
        }


        /// <summary>
        /// Callback for Write operation
        /// </summary>
        private void SendCallback(IAsyncResult result)
        {
            if (this._isConnected)
            {
                if (result.IsCompleted)
                {
                    //Datentransfer abgeschlossen
                    if (this.DataSent != null)
                    {
                        this.DataSent(
                            this,
                            new ProgressEventArgs(
                                100,
                                result.ToString()
                                )
                            );
                    }
                }
            }
        }


        /// <summary>
        /// Callback for Read operation
        /// </summary>
        private void ReadCallback(IAsyncResult result)
        {
            if (this._isConnected)
            {
                NetworkStream networkStream;
                try
                {
                    networkStream = new NetworkStream(this.mySocket);
                }

                catch (Exception ex)
                {
                    //  Logger.WriteLog(LogLevel.Warning, "ex.Message);
                    if (this.OnError != null)
                    {
                        this.OnError(this, new ErrorEventArgs("Fehler beim lesen empfangener Daten:\r\n" + ex.ToString()));
                    }
                    return;
                }

                byte[] buffer = result.AsyncState as byte[];
                string data = ASCIIEncoding.ASCII.GetString(buffer, 0, buffer.Length);

                //Do something with the data object here.
                if (this.DataReceived != null)
                {
                    this.DataReceived(
                        this,
                        new ProgressEventArgs(
                            50,
                            data
                            )
                        );
                }
                networkStream.FlushAsync();
                //Then start reading from the network again.

                try
                {
                    networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);
                }
                catch (Exception ex)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(this, new ErrorEventArgs("Fehler beim Socket-Lesevorgang:\r\n" + ex.ToString()));
                    }
                }
            }
        }
    }
}
