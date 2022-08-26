using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClientWinforms
{
    public class ServerComms
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;

        public Security secureChat;

        public List<MessageList> convos;
        public List<ListViewItem> messages;
        public List<ListViewItem> onlineList;
        private bool connected;
        private bool serverKeyRecevied;

        public bool Connected
        {
            get { return connected; }
            set { connected = value;
                OnValueChanged(null);
            }
        }

        public ServerComms()
        {
            Connected = false;
            convos = new List<MessageList>();
            convos.Add(new MessageList("Global"));
            messages = new List<ListViewItem>();
            onlineList = new List<ListViewItem>();
            secureChat = new Security();
            serverKeyRecevied = false;
        }

        #region //Eventhandler (+delegate) for when Connected value changes.
        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e)
        {
            if(ValueChanged != null)
            {
                ValueChanged(this, e);
            }
        }
        #endregion

        public event EventHandler MessageListChanged;
        protected virtual void OnMessageListChanged(EventArgs e)
        {
            if (MessageListChanged != null)
            {
                MessageListChanged(this, e);
            }
        }

        public event EventHandler OnlineListUpdated;
        protected virtual void OnUpdatedOnlineList(EventArgs e)
        {
            if(OnlineListUpdated != null)
            {
                OnlineListUpdated(this, e);
            }
        }

        public event EventHandler<NewConversationEventArgs> NewConvoStarted;
        protected virtual void OnNewConvoStarted(NewConversationEventArgs e)
        {
            if(NewConvoStarted != null)
            {
                NewConvoStarted(this, e);
                
            }
        }

        public void AddMessageToList(string recipient, string s)
        {
            //messages.Add(new ListViewItem(s));

            int i = convos.FindIndex(x => x.name == recipient);
            if(i == -1)
            {
                convos.Add(new MessageList(recipient));
                convos.Last().messages.Add(new ListViewItem(s));
                convos.Last().newMessage = true;
                OnMessageListChanged(null);
                OnNewConvoStarted(new NewConversationEventArgs(recipient));
            }
            else
            {
                convos[i].messages.Add(new ListViewItem(s));
                convos[i].newMessage = true;
                OnMessageListChanged(null);
            }
            
        }

        public void AddPublicKeyToContact(string contact, byte[] Modulus, byte[] Exponent)
        {
            int i = convos.FindIndex(x => x.name == contact);
            if(i == -1)
            {
                convos.Add(new MessageList(contact, Modulus, Exponent));
                OnNewConvoStarted(new NewConversationEventArgs(contact));
            }
            else
            {
                convos[i].pubKeyModulus = Modulus;
                convos[i].pubKeyExponent = Exponent;
                convos[i].UpdateRSAParamaters();
                convos[i].keyObtained = true;
            }
        }

        public void UpdateOnlineList(string s)
        {
            //Clear onlineList first.
            onlineList.Clear();

            //(Re)populate it with up to date online list from server.
            StringBuilder sb = new StringBuilder();
            sb.Append(s);
            sb.Remove(0, 5);
            string[] temp = sb.ToString().Split(',');
            foreach (var item in temp)
            {
                onlineList.Add(new ListViewItem(item));
            }
            
            OnUpdatedOnlineList(null);

        }


        public async Task ConnectToServerAsync(string NickName, string Password)
        {

            secureChat.ChangeRSAValue();

            await Task.Run(() =>
            {
                int port = 8080;
                IPAddress ipAddess = IPAddress.Parse("127.0.0.1");

                try
                {
                    client = new TcpClient(ipAddess.ToString(), port);
                    reader = new StreamReader(client.GetStream());
                    writer = new StreamWriter(client.GetStream());

                    Thread t = new Thread(RetrieveMessages);
                    t.IsBackground = true;
                    t.Start(client);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            });

            if(client != null)
            {
                Connected = true;

                //SECURELY Send first message for connection verification, AFTER making sure the server public key is known;             
                int i = convos.FindIndex(x => x.name == "Global");
                while(i == -1)
                {
                    Thread.Sleep(500);
                    i = convos.FindIndex(x => x.name == "Global");
                    System.Diagnostics.Debug.WriteLine("SLEEPING during first security check Sleep.");
                }
                int count = 0;
                while (!convos[i].keyObtained)
                {
                    Thread.Sleep(500);
                    count++;
                    if(count > 5)
                    {
                        break;
                    }
                    System.Diagnostics.Debug.WriteLine("SLEEPING during second security check Sleep.");
                }

                if (convos[i].keyObtained)
                {
                    string temp = NickName + " " + Password;
                    string secureTemp = secureChat.RSAEncrypt(temp, convos[i].RSAParams);
                    SendMessageLogin(secureTemp);
                    SendMessageLogin(secureChat.PublicKeyAsString());
                    AddMessageToList("Global", "Connected to server.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: COULD NOT LOG IN SECURELY.");
                    Connected = false;
                }

            }
        }

        public void SendMessageLogin(string Message)
        {
            string s = Message;

            if (Connected)
            {
                try
                {
                    writer.WriteLine(s);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    DisconnectFromServerAsync();
                }

            }
        }

        public void SendMessage(string Message)
        {
            string s = Message;

            //Check for manually typed /disconnect, call DisconnectFromServerAsync() if true;
            if(s.StartsWith("/disconnect"))
            {
                DisconnectFromServerAsync();
                return;
            }

            if (Connected)
            {
                try
                {
                    writer.WriteLine(s);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    DisconnectFromServerAsync();
                }

            }
            else
            {
                AddMessageToList("Global", "Not connected. Try again.");
            }
        }

        private void RetrieveMessages(object argument)
        {
            System.Diagnostics.Debug.WriteLine("Listening to server started...");

            while (true)
            {
                try
                {
                    string temp = reader.ReadLine();
                    string s = CheckForServerCommunication(temp);

                    if(s.StartsWith("/PM "))
                    {
                        string[] tempArray = s.Split(' ');

                        StringBuilder sb = new StringBuilder();
                        sb.Append(tempArray[1] + ": ");

                        string decryptedText = secureChat.RSADecrypt(tempArray[2]);

                        sb.Append(decryptedText);

                        AddMessageToList(tempArray[1], sb.ToString());
                        
                    }
                    else if (s.StartsWith("/PMX"))
                    {
                        string[] tempArray = s.Split(' ');

                        StringBuilder sb = new StringBuilder();

                        for (int i = 2; i < tempArray.Length; i++)
                        {
                            sb.Append(tempArray[i]);
                            sb.Append(" ");
                        }
                        sb.Remove(sb.Length - 1, 1);

                        AddMessageToList(tempArray[1], sb.ToString());
                    }
                    else if(s.StartsWith("/Global"))
                    {
                        string[] tempArray = s.Split(' ');

                        string decryptedText = secureChat.RSADecrypt(tempArray[1]);

                        AddMessageToList("Global", decryptedText);
                    }

                    //Temp stuff:
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(s);
                    }

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    break;
                }

            }
        }

        private string CheckForServerCommunication(string s)
        {
            if(s == "Error001")
            {
                DisconnectFromServerAsync();
                return "Connection denied, wrong nickname of password.";
            }
            else if(s.StartsWith("ERROR002"))
            {
                string[] tempArray = s.Split(' ');

                //Set recipient keyObtained to false so a new /requestkey is sent when sending message;
                int temp = convos.FindIndex(x => x.name == tempArray[1]);
                if(temp >= 0)
                {
                    convos[temp].keyObtained = false;
                }
                return "/PMX " + tempArray[1] + " <" + tempArray[1] + " is currently offline.>";
            }
            else if (s == "Shutdown")
            {
                DisconnectFromServerAsync();
                return "Server shut down.";
            }
            else if (s.StartsWith("/who:"))
            {
                UpdateOnlineList(s);
                return "Online list updated.";
            }
            else if (s.StartsWith("/PublicKey"))
            {
                //[0] = "/PublicKey", [1] = key owner, [2] = key modulus string, [3] = key exponent string;
                string[] tempArray = s.Split(' ');

                string contact = tempArray[1];
                byte[] keyModulus = Array.ConvertAll(tempArray[2].Split(','), Byte.Parse);
                byte[] keyExponent = Array.ConvertAll(tempArray[3].Split(','), Byte.Parse);

                AddPublicKeyToContact(contact, keyModulus, keyExponent);

                System.Diagnostics.Debug.WriteLine("/PM " + contact + " Connection secured.");
                return "Public key updated for " + contact;
            }
            else
            {
                return s;
            }

        }


        public async void DisconnectFromServerAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    writer.WriteLine("/disconnect");
                    writer.Flush();


                    writer.Close();
                    reader.Close();
                    client.Close();

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
                writer = null;
                reader = null;
                client = null;

                AddMessageToList("Global", "Disconnected from server.");
            });

            Connected = false;
        }








    }
}
