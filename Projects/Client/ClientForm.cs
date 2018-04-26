using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MessageLib;
using System.Security.Cryptography;
/*
* Application name: TCP Chat with Diffie Hellman Exchange key & AES encryption
* Author: Le Hoang Tuan - 15520967
* Package name: client
*/

namespace TCPClient
{
    public partial class ClientForm : Form
    {
        public ClientForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        public enum Adder { Client, Server };
        private readonly int _maxBuffer = 1024; //1024 Byte
        private bool isConnected = false;
        Socket client;
        IPEndPoint ipeServer; //IP Endpoint of Server
        Thread thdClient;

        private ECDiffieHellmanCng DiffieHellman;
        private byte[] key;

        public void InitIP() //auto get IPAddress's my computer
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress addr in host.AddressList)
            {
                if (addr.AddressFamily.ToString() == "InterNetwork")
                {
                    tbHost.Text = addr.ToString();
                }
            }

        }

        private void ChangeItemsWhenConnect()
        {
            btnConnect.Text = "Ngắt kết nối";
            btnConnect.BackColor = Color.Silver;
            tbHost.Enabled = false;
            rtbMsg.Enabled = true;
            isConnected = true;
            pMessage.Show();
            pSend.Show();
        }

        private void ChangeItemsWhenDisconnect()
        {
            btnConnect.Text = "Kết nối";
            btnConnect.BackColor = Color.LimeGreen;
            tbHost.Enabled = true;
            rtbMsg.Enabled = false;
            isConnected = false;
            pMessage.Hide();
            pSend.Hide();
        }
        
        private bool Connect(IPAddress ip, int port)
        {
            try
            {

                ipeServer = new IPEndPoint(ip, port);
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ipeServer);
            }
            catch (SocketException e)
            {
                AddLog(e.Message);
                return false;
            }
            return true;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            bool isExcept = false;
            if (!isConnected)
            {
                //Start check exception
                IPAddress ipDesc;
                int portDesc;
                if (!IPAddress.TryParse(tbHost.Text, out ipDesc))
                {
                    isExcept = true;
                    MessageBox.Show("Địa chỉ IP chưa đúng. Vui lòng kiểm tra lại");
                }

                if (int.TryParse(tbPort.Text, out portDesc))
                {
                    if (portDesc <= 0 || portDesc >= 65535)
                    {
                        isExcept = true;
                        MessageBox.Show("Port chưa đúng. Vui lòng kiểm tra lại");
                    } 
                } else
                {
                    isExcept = true;
                    MessageBox.Show("Port chưa đúng. Vui lòng kiểm tra lại");
                }
                //end check exception

                //Connect
                if (!isExcept)
                {
                    if (Connect(ipDesc, portDesc))
                    {
                        AddLog("Đã kết nối thành công đến Server " + ipDesc.ToString() + ":" + portDesc.ToString());
                        isConnected = true;
                        DiffieHellman = new ECDiffieHellmanCng();
                        DiffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                        DiffieHellman.HashAlgorithm = CngAlgorithm.Sha256;
                        thdClient = new Thread(listenFromServer);
                        thdClient.IsBackground = true;
                        thdClient.Start(client);
                    } else  {
                        AddLog("Không thể kết nối đến Server " + ipDesc.ToString() + ":" + portDesc.ToString());
                    }
                }    
            } else {
                //Disconnect
                ChangeItemsWhenDisconnect();
                CloseConnect();
            }
        }

        public void listenFromServer(object obj)
        {
            Socket sk = (Socket)obj;
            try
            {
                while (true)
                {
                    byte[] buff = new byte[1024];
                    int recv = sk.Receive(buff);
                    if (recv == 0) break;
                    ReceiveMessage(buff);
                }
            } catch (SocketException)
            {
                AddLog("Server đã đóng máy chủ");
                ChangeItemsWhenDisconnect();
                CloseConnect();
                key = null;
                return;
            }
            
        }

        private void CloseConnect()
        {
            isConnected = false;
            thdClient.Abort();
            client.Close();

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
                if(adder == Adder.Client)
                {
                    rtbMsg.SelectionColor = Color.Blue;
                }
                else
                {
                    rtbMsg.SelectionColor = Color.Red;
                }
                rtbMsg.AppendText("[" + DateTime.Now + "] " + msg);
                rtbMsg.SelectionColor = Color.Gray;
                rtbMsg.AppendText("-------------------------------------------------------------------------------------------------------------------------------------------------------------------" + Environment.NewLine);
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
                    MessageBox.Show(e.Message, "Lỗi kỹ thuật DiffieHellman", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
            }
            else
            {
                msgStr.Encrypt(key);
            }

            msgStr.mode = mode;
            client.Send(Serialize(msgStr));
            //if (mode == 0)
            //    MessageBox.Show("Đã gửi key cho server");
        }

        private void ReceiveMessage(byte[] buff)
        {
            MsgStruct msgStr = Deserialize(buff);

            switch(msgStr.mode)
            {
                case 0:
                    key = DiffieHellman.DeriveKeyMaterial(CngKey.Import(msgStr.msg, CngKeyBlobFormat.EccPublicBlob));
                    //MessageBox.Show("Đã nhận key từ server");
                    AddLog("Đã nhận khóa từ server");
                    SendMessage(" ", 0); //send pub key
                    ChangeItemsWhenConnect();
                    break;
                case 1:
                    while (key == null) ;
                    msgStr.Decrypt(key);
                    AddMessage(msgStr.GetString(), Adder.Server);
                    break;
            }
        }

        private void rtbMsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void lbOnline_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string msg = "Client: " + tbMsg.Text + Environment.NewLine;
            SendMessage(msg);
            AddMessage(msg, Adder.Client);
            tbMsg.Text = String.Empty; //reset textbox message send
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e)
        {
          //catch press Enter to send message
          if (e.KeyCode == Keys.Enter)
            {
                btnSend.PerformClick();
            }
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {

            InitIP(); //auto load local IP Address
            ChangeItemsWhenDisconnect();
            
        }
    }
}
