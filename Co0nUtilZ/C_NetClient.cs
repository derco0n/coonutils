using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Co0nUtilZ
{

    /// <summary>
    /// This class represents a TCP-Client
    /// Author: D. Marx
    /// Project: https://github.com/derco0n/coonutils   
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    public class C_NetClient
    {

        //private TcpClient tcpClient = null;
        private Socket mySocket;
        private int _port;
        private String _ip;


        //private Encoding m_encoding = Encoding.ASCII;
        private Encoding m_encoding = Encoding.UTF8;

        public delegate void ProgressEventHandler(object sender, Co0nUtilZ.ProgressEventArgs Args);
        public delegate void ErrorEventHandler(object sender, Co0nUtilZ.ErrorEventArgs Args);
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


        private int _connect_timeout = 5000; //Default: Wait 5 Second while trying to connect to server
        public int ConnectTimeout
        {
            get
            {
                return this._connect_timeout;
            }
            set
            {
                if (value > 0)
                {
                    this._connect_timeout = value;
                }
            }
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

                IAsyncResult result = this.mySocket.BeginConnect(
                    this._ip,
                    this._port,
                    new AsyncCallback(
                        ConnectCallback
                        ),
                    mySocket
                    );




                bool success = result.AsyncWaitHandle.WaitOne(this._connect_timeout, true);

                /*
                System.Threading.WaitHandle wh = result.AsyncWaitHandle;

                try
                {
                    if(!result.AsyncWaitHandle.WaitOne(this._connect_timeout, false))
                    {
                        // NOTE, MUST CLOSE THE SOCKET  
                        this.silentDisconnect();

                        if (this.OnError != null)
                        {
                            this.OnError(this, new Co0nUtilZ.ErrorEventArgs("\r\nTimeout while connecting to Server (Timeout: " + this._connect_timeout + "ms).\r\nIs the server (" + this._ip + ":" + this._port.ToString() + ") reachable and the service running?\r\nPlease check connection( -settings) and Firewallsettings or try to increase Timeout.\r\n"));
                        }
                        //throw new ApplicationException("\r\nTimeout while connecting to Server (Timeout: " + this._connect_timeout + "ms).\r\nIs the server (" + this._ip + ":" + this._port.ToString() + ") reachable and the service running?\r\nPlease check connection( -settings) and Firewallsettings or try to increase Timeout.\r\n");

                    }

                    this.mySocket.EndConnect(result);
                }
                finally
                {
                    wh.Close();
                }
                */


                if (success)
                {
                    this.mySocket.EndConnect(result);
                }
                else
                {
                    // NOTE, MUST CLOSE THE SOCKET  
                    this.silentDisconnect();

                    if (this.OnError != null)
                    {
                        this.OnError(this, new Co0nUtilZ.ErrorEventArgs("\r\nTimeout while connecting to Server (Timeout: " + this._connect_timeout + "ms).\r\nIs the server (" + this._ip + ":" + this._port.ToString() + ") reachable and the service running?\r\nPlease check connection( -settings) and Firewallsettings or try to increase Timeout.\r\n"));
                    }
                    //throw new ApplicationException("\r\nTimeout while connecting to Server (Timeout: " + this._connect_timeout + "ms).\r\nIs the server (" + this._ip + ":" + this._port.ToString() + ") reachable and the service running?\r\nPlease check connection( -settings) and Firewallsettings or try to increase Timeout.\r\n");

                }


            }
            catch (Exception ex)
            {
                /*  
                 *  
                 *  Logger.WriteLog(LogLevel.Error, "ex.Message);
                 *  
                 */
                this.silentDisconnect();
                if (this.OnError != null)
                {
                    this.OnError(this, new Co0nUtilZ.ErrorEventArgs("Error while connecting:\r\n" + ex.ToString()));
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

                /*
                Info:
                SelectRead=true if
                Listen has been called and a connection is pending; 
                data is available for reading; 
                connection has been closed, reset, or terminated; 
                    - hmmm... seems that this doesn't make sense here...

               SelectError=true if
               processing a Connect that does not block, and the connection has failed; 
               OutOfBandInline is not set and out-of-band data is available; 

               */

                //bool sockReadResult = socket.Poll(1, SelectMode.SelectRead);
                bool socKErrResult = socket.Poll(1, SelectMode.SelectError);
                bool sockConnected = socket.Connected;
                myreturn = /*sockReadResult &*/ !socKErrResult & sockConnected;


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
                    this.Disconnected(this, new Co0nUtilZ.ProgressEventArgs(0, "Connection closed by server/network."));
                }
                this.timer.Dispose();
            }

        }

        /// <summary>
        /// closes a connection
        /// </summary>
        private void silentDisconnect()
        {
            if (this.mySocket != null && this.mySocket.Connected)
            {

                this._isConnected = false; //tell that connection is not disconnected

                //Sämtliche aktiven Übertragungen beenden
                this.mySocket.Shutdown(SocketShutdown.Send);
                Thread.Sleep(50);
                this.mySocket.Shutdown(SocketShutdown.Receive);
                Thread.Sleep(50);
                this.mySocket.Disconnect(false); //Das Socket trennen und nicht wieder benutzen (Dispose())
                if (!this.mySocket.Connected)
                {
                    if (this.Disconnected != null)
                    {
                        this.Disconnected(this, new Co0nUtilZ.ProgressEventArgs(10, "Connection closed by client."));
                    }
                    if (this.timer != null)
                    {
                        this.timer.Dispose();
                    }
                }
                else
                {
                    this._isConnected = true; //Connection. however ist still alive
                }

            }
        }

        /// <summary>
        /// Verbindung zum Server trennen
        /// </summary>
        public void Disconnect()
        {
            this.silentDisconnect();

            if (this.Disconnected != null)
            {
                this.Disconnected(this, new Co0nUtilZ.ProgressEventArgs(10, "Connection closed by client."));
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
                        this.Connected(this, new Co0nUtilZ.ProgressEventArgs(30, "Connected."));
                    }


                    if (this.mySocket != null)
                    {
                        //Verbunden
                        this._isConnected = this.mySocket.Connected;

                        if (this._isConnected)
                        {
                            TimerCallback tick = new TimerCallback(CheckConnection); //Call method "CheckConnection" with every TimetrTick
                            this.timer = new System.Threading.Timer(tick, null, 0, 1000); //Initialize new timer - intervall in ms
                        }
                    }

                }

                catch (Exception ex)

                {
                    this.silentDisconnect();
                    if (this.OnError != null)
                    {
                        this.OnError(this, new Co0nUtilZ.ErrorEventArgs("Error while establishing a connection (Callback).\r\n" + ex.ToString()));
                    }
                }
            }
            else
            {
                if (this.OnError != null)
                {
                    String Details = "";
                    if (this.mySocket != null)
                    {
                        Details += "Socket is initialised.\r\n";
                    }
                    else
                    {
                        Details += "Socket is not initialised.\r\n";
                    }

                    if (this.ConIsAlive(this.mySocket))
                    {
                        Details += "Connection is alive.\r\n";
                    }
                    else
                    {
                        Details += "Connection is dead.\r\n";
                    }


                    this.OnError(this, new Co0nUtilZ.ErrorEventArgs(
                        "Connection could not be established!\r\nIs the Socket still in use?\r\nIs the Server reachable?\r\nPlease make sure no firewall is blocking the connection to \"" + this._ip + ":" + this._port.ToString() + "\"\r\nDetails: " + Details
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
                    Int32 sentbytes = 0;
                    try
                    {
                        sentbytes = this.mySocket.EndSend(result); //Ends Sending initiated by BeginSend(); Returns Bytes that have been sent
                    }
                    catch (ArgumentNullException ex)
                    {
                        //asyncResult is null.
                    }
                    catch (ArgumentException ex)
                    {
                        //asyncResult was not returned by a call to the BeginSend method.
                    }
                    catch (SocketException ex)
                    {
                        //An error occurred when attempting to access the socket. See the Remarks section for more information.
                    }
                    catch (ObjectDisposedException ex)
                    {
                        //The Socket has been closed.
                    }
                    catch (InvalidOperationException ex)
                    {
                        //EndSend was previously called for the asynchronous send.
                    }

                    if (sentbytes < this.mySocket.SendBufferSize)
                    {//Less Bytes than Buffer were sent... seems something ist missing

                    }

                    //Datentransfer abgeschlossen
                    if (this.DataSent != null)
                    {
                        this.DataSent(
                            this,
                            new Co0nUtilZ.ProgressEventArgs(
                                100,
                                result.ToString() + " (Bytes=" + sentbytes + ")"
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
                        this.OnError(this, new Co0nUtilZ.ErrorEventArgs("Fehler beim lesen empfangener Daten:\r\n" + ex.ToString()));
                    }
                    return;
                }
                Int32 BytesReceived = networkStream.EndRead(result);
                byte[] buffer = { };
                try
                {
                    buffer = result.AsyncState as byte[];
                }
                catch (ArgumentNullException ex)
                {
                    //asyncResult is null.
                }
                catch (ArgumentException ex)
                {
                    //asyncResult was not returned by a call to the BeginSend method.
                }
                catch (SocketException ex)
                {
                    //An error occurred when attempting to access the socket. See the Remarks section for more information.
                }
                catch (ObjectDisposedException ex)
                {
                    //The Socket has been closed.
                }
                catch (InvalidOperationException ex)
                {
                    //EndSend was previously called for the asynchronous send.
                }


                string data = "";
                if (BytesReceived > 0)
                {//Less Bytes than Buffer were sent... seems something ist missing
                    data = ASCIIEncoding.ASCII.GetString(buffer, 0, buffer.Length);
                    data = data.Substring(BytesReceived);
                }



                //Do something with the data object here.
                if (this.DataReceived != null)
                {
                    this.DataReceived(
                        this,
                        new Co0nUtilZ.ProgressEventArgs(
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
                        this.OnError(this, new Co0nUtilZ.ErrorEventArgs("Fehler beim Socket-Lesevorgang:\r\n" + ex.ToString()));
                    }
                }
            }
        }
    }


}
