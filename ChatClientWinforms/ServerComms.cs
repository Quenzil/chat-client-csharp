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

        public List<MessageList> convos;
        public List<ListViewItem> messages;
        public List<ListViewItem> onlineList;
        private bool connected;
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

                //Send first message for connection verification;
                string temp = NickName + "," + Password;
                SendMessageLogin(temp);
                AddMessageToList("Global","Connected to server.");
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
            if(s == "/disconnect")
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
                    //AddMessageToList(s);
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
                    string s = CheckForServerErrors(temp);

                    if(s.StartsWith("/PM "))
                    {
                        string[] tempArray = s.Split(' ');

                        StringBuilder sb = new StringBuilder();
                        sb.Append(tempArray[1] + ": ");

                        for (int i = 2; i < tempArray.Length; i++)
                        {
                            sb.Append(tempArray[i]);
                            sb.Append(" ");
                        }
                        sb.Remove(sb.Length - 1, 1);

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
                    else
                    {
                        AddMessageToList("Global", s);
                    }



                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    break;
                }

            }
        }

        private string CheckForServerErrors(string s)
        {
            if(s == "Error001")
            {
                DisconnectFromServerAsync();
                return "Connection denied, wrong nickname of password.";
            }
            else if(s.StartsWith("ERROR002"))
            {
                string[] tempArray = s.Split(' ');

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
