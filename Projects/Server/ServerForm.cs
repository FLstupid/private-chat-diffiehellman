using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using MessageLib;

/*
 * Application name: TCP Chat with Diffie Hellman Exchange key & AES encryption
 * Author: Le Hoang Tuan - 15520967
 * Package name: server
 */

namespace TCPServer
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        public enum Adder { Client, Server };

        private bool isConnected = false;
        private readonly int _defaultPort = 42018;
        private readonly int _maxBuffer = 1024; //1024 Byte
        private bool isAccepted = false;
        Thread thdListener;
        Socket server;
        IPEndPoint ipe; //my IP endpoint
        Socket client;

        private ECDiffieHellmanCng DiffieHellman;
        private byte[] key;


        public void initIP() //auto get IPAddress's my computer
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            string myIP = null;
            foreach (IPAddress addr in host.AddressList)
            {
                if (addr.AddressFamily.ToString() == "InterNetwork")
                {
                    myIP = addr.ToString();
                }
            }

            ipe = new IPEndPoint(IPAddress.Parse(myIP), _defaultPort);
        }

        private byte[] Serialize(MsgStruct msgStr)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, msgStr);
            byte[] buff = new byte[_maxBuffer];
            return stream.ToArray();
        }

        private MsgStruct Deserialize(byte[] buff)
        {
            MemoryStream stream = new MemoryStream(buff);
            BinaryFormatter bf = new BinaryFormatter();
            return (MsgStruct)bf.Deserialize(stream);
        }

        private void AddMessage(string msg, Adder adder)
        {
            rtbMsg.Invoke(new MethodInvoker(delegate ()
            {
                if (adder == Adder.Client)
                {
                    rtbMsg.SelectionColor = Color.Blue;
                }
                else
                {
                    rtbMsg.SelectionColor = Color.Red;
                }
                rtbMsg.AppendText("[" + DateTime.Now + "] " + msg);
                rtbMsg.SelectionColor = Color.Gray;
                rtbMsg.AppendText("----------------------------------------------------------------------------------------------------------------------------------------------------------------------------" + Environment.NewLine);
                rtbMsg.ScrollToCaret();
            }));
        }

        private void AddLog(string msg)
        {
            rtbLog.Invoke(new MethodInvoker(delegate ()
            {

                rtbLog.AppendText("[" + DateTime.Now + "]" + Environment.NewLine + msg + Environment.NewLine + "-----------------------------------------------------------" + Environment.NewLine);
                rtbLog.ScrollToCaret();
            }));
        }

        private void SendMessage(string msg, int mode = 1)
        {
            MsgStruct msgStr = new MsgStruct(msg);
            if (mode == 0)
            {
                try
                {
                    msgStr.msg = DiffieHellman.PublicKey.ToByteArray();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Lỗi kỹ thuật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
            }
            else
            {
                msgStr.Encrypt(key);
            }

            msgStr.mode = mode;
            client.Send(Serialize(msgStr));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            MsgStruct msgStr = new MsgStruct();
            string msg = "Server: " + tbMsg.Text + Environment.NewLine;
            SendMessage(msg);
            AddMessage(msg, Adder.Server);
            tbMsg.Text = string.Empty;
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnSend.PerformClick();
        }

        private bool Listen()
        {
            try
            {
                IPAddress ip;
                if (IPAddress.TryParse(tbHost.Text.Trim(), out ip))
                {
                    ipe.Address = ip;
                }
                else
                {
                    MessageBox.Show("IP không hợp lệ hoặc sai");
                    return false;
                }

                int Port;

                if (Int32.TryParse(tbPort.Text.Trim(), out Port))
                {
                    if (Port <= 0 || Port >= 65535)
                    {
                        MessageBox.Show("Port không hợp lệ");
                        return false;
                    }
                    ipe.Port = Port;
                }
                else
                {
                    MessageBox.Show("Port không hợp lệ");
                    return false;
                }


                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                thdListener = new Thread(new ThreadStart(Listener));
                thdListener.IsBackground = true;
                thdListener.Start();
            }
            catch (SocketException)
            {
                return false;
            }
            return true;
        }

        public void Listener()
        {
            server.Bind(ipe);
            server.Listen(1);
            try
            {
                while (true)
                {
                    if (isAccepted == false)
                    {
                        
                        client = server.Accept();
                        isAccepted = true;
                        AddLog("Đã chấp nhận kết nối từ " + client.RemoteEndPoint.ToString());
                        SendMessage(" ", 0); //send private message
                        Thread clientProccess = new Thread(threadClient);
                        clientProccess.IsBackground = true;
                        clientProccess.Start(client);

                    }
                }
            }
            catch
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

        }

        public void threadClient(Object obj)
        {
            if (!isConnected) return;
            Socket skClient = (Socket)obj;
            bool isWhile = true;

            try
            {
                while (isWhile)
                {
                    byte[] buff = new byte[1024];
                    int recv = skClient.Receive(buff);
                    string msg;
                    if (recv == 0)
                    {
                        AddLog(skClient.RemoteEndPoint.ToString() + " đã ngắt kết nối.");
                        isAccepted = false;
                        pSend.Hide();
                        break;
                    }
                    MsgStruct msgStr = Deserialize(buff);
                    switch (msgStr.mode)
                    {
                        case 0: //Key exchange
                            key = DiffieHellman.DeriveKeyMaterial(CngKey.Import(msgStr.msg, CngKeyBlobFormat.EccPublicBlob));
                            AddLog("Đã nhận khóa từ client.");
                            msg = "Server: Chào " + client.RemoteEndPoint.ToString() + Environment.NewLine;
                            AddMessage(msg, Adder.Server);
                            SendMessage(msg);
                            pSend.Show();

                            break;
                        case 1:
                            while (key == null) ;
                            msgStr.Decrypt(key);
                            AddMessage(msgStr.GetString(), Adder.Client);
                            break;
                    }
                }
            }
            catch (SocketException)
            {
                AddLog(skClient.RemoteEndPoint.ToString() + " đã ngắt kết nối.");
                isAccepted = false;
                pSend.Hide();
                isWhile = false;
                key = null;
                return;
            }

        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                if (Listen())
                {
                    DiffieHellman = new ECDiffieHellmanCng();
                    DiffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    DiffieHellman.HashAlgorithm = CngAlgorithm.Sha256;
                    pMessage.Show();
                    btnListen.Enabled = false;
                    isConnected = true;
                    tbHost.Enabled = false;
                    tbHost.Enabled = false;
                    btnListen.Text = "Đang lắng nghe";
                    AddLog("Đã mở cổng " + tbPort.Text);
                }
                else
                {
                    MessageBox.Show("Không thể mở kết nối");
                }
            }
            else
            {
                isConnected = false;
                btnListen.Text = "Mở kết nối";
                tbHost.Enabled = true;
                tbPort.Enabled = true;
                thdListener.Abort();
                server.Shutdown(SocketShutdown.Both);
                server.Disconnect(false);
                server.Close();
            }
        }

        private void rtbMsg_KeyPress(object sender, KeyPressEventArgs e)
        {

        }


        private void ServerForm_Load(object sender, EventArgs e)
        {
            initIP();
            tbHost.Text = ipe.Address.ToString();
            pSend.Hide();
            pMessage.Hide();
        }
    }
}
