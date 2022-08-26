using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClientWinforms
{
    public partial class Form1 : Form
    {
        ServerComms comm;
        RegisterForm signup;
        string lastLogin;

        public Form1()
        {
            InitializeComponent();
            this.AcceptButton = btnSend;
            //Globals.messages = new List<ListViewItem>();
            comm = new ServerComms();
            signup = new RegisterForm(comm.secureChat);
            comm.ValueChanged += ConnectionStatusChanged;
            comm.MessageListChanged += MessageListUpdated;
            comm.OnlineListUpdated += OnlineListUpdated;
            comm.NewConvoStarted += NewTabAdded;

            lstMessages.Columns.Add("Global Chat:", lstMessages.Width, HorizontalAlignment.Left);

            tabControl1.TabPages[0].Name = "Global";
            tabControl1.TabPages[0].Text = "Global";
            tabControl1.SelectTab(0);
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += new DrawItemEventHandler(TabControl1_DrawItem);

            
        }

        //Manually draw text on tabControl to have the text on the tabs show horizontally instead of automatic vertical;
        public void TabControl1_DrawItem(Object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Brush textBrush;

            TabPage tabPage = tabControl1.TabPages[e.Index];

            Rectangle tabBounds = tabControl1.GetTabRect(e.Index);

            //Different colour/background for selected and not-selected tabs;
            if(e.State == DrawItemState.Selected)
            {
                textBrush = new SolidBrush(Color.Green);
                g.FillRectangle(Brushes.LightBlue, e.Bounds);

                    if(comm.convos.Find(x => x.name == tabPage.Name) != null)
                    {
                        comm.convos.Find(x => x.name == tabPage.Name).newMessage = false;
                    }
                
            }
            else if(comm.convos.Find(x => x.name == tabPage.Name)!= null && comm.convos.Find(x => x.name == tabPage.Name).newMessage == true)
            {
                textBrush = new SolidBrush(Color.DarkRed);
                e.DrawBackground();
            }

            else
            {
                textBrush = new SolidBrush(e.ForeColor);
                e.DrawBackground();
            }

            Font tabFont = new Font("Arial", 10.0f, FontStyle.Bold, GraphicsUnit.Pixel);

            //Draw the string. Center the text;
            StringFormat stringFlags = new StringFormat();
            stringFlags.Alignment = StringAlignment.Center;
            stringFlags.LineAlignment = StringAlignment.Center;
            g.DrawString(tabPage.Text, tabFont, textBrush, tabBounds, new StringFormat(stringFlags));
        }

        public void ConnectionStatusChanged(object source, EventArgs e)
        {
            ServerComms temp = (ServerComms)source;
            if (temp.Connected)
            {
                StatusLabel.Invoke((MethodInvoker)(() =>
                {
                    StatusLabel.Text = "Online";
                }));

                btnConnect.Invoke((MethodInvoker)(() =>
                {
                    btnConnect.Text = "Disconnect";
                }));
                //NOTE: the following 2 lines don't work for cross-thread control manipulation, so the above is needed instead. Same goes for any cross-thread changes.
                //StatusLabel.Text = "Online";
                //btnConnect.Text = "Disconnect";
            }
            else
            {
                StatusLabel.Invoke((MethodInvoker)(() =>
                {
                    StatusLabel.Text = "Offline";
                }));

                btnConnect.Invoke((MethodInvoker)(() =>
                {
                    btnConnect.Text = "Connect";
                }));

                //Set server/"Global" keyObtained to false when disconnected by server;
                int i = comm.convos.FindIndex(x => x.name == "Global");
                if (i != -1)
                {
                    comm.convos[i].keyObtained = false;
                }
            }
        }

        public void MessageListUpdated(object source, EventArgs e)
        {
            
            lstMessages.Invoke((MethodInvoker)(() =>
            {
                UpdateLstMessages();
            }));

            tabControl1.Invoke((MethodInvoker)(() =>
            {
                tabControl1.Refresh();
            }));
        }

        public void OnlineListUpdated(object source, EventArgs e)
        {
            lstOnline.Invoke((MethodInvoker)(() =>
            {
                UpdateLstOnline();
            }));
        }

        public void NewTabAdded(object source, NewConversationEventArgs e)
        {
            tabControl1.Invoke((MethodInvoker)(() =>
            {

                if(tabControl1.Controls.Find(e.Contact, false).Count() == 0)
                {
                    TabPage temp = new TabPage(e.Contact);
                    temp.Name = e.Contact;
                    tabControl1.Controls.Add(temp);
                    tabControl1.Refresh();
                }


            }));
        }


        private async void BtnConnect_Click(object sender, EventArgs e)
        {

            btnConnect.Enabled = false;

            if (!comm.Connected)
            {
                await comm.ConnectToServerAsync(txtName.Text, txtPassword.Text);

                //Request public keys from server from known online contacts/PMs since after last server downtime, update/add public key info from and to them, restore PM tabs.
                comm.SendMessage("/RequestContactKeys");

                //Initial /who to populate the online list upon logging in.
                comm.SendMessage("/who");

                #region
                /*
                //If there are PM tabs open, send a "/RequestMultipleKeys" server request for all online people whose PMs are open.
                //Else send a "/RequestContactKeys" server request for all online people of whom the PMs were open since last server restart. 
                if (tabControl1.TabPages.Count > 1)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 1; i < tabControl1.TabPages.Count; i++)
                    {
                        sb.Append(tabControl1.TabPages[i].Name + " ");
                    }
                    sb.Remove(sb.Length - 1, 1);

                    comm.SendMessage("/RequestMultipleKeys " + sb.ToString());
                }
                else
                {
                    comm.SendMessage("/RequestContactKeys");
                }
                */
                #endregion

                btnConnect.Enabled = true;
            }
            else
            {
                comm.DisconnectFromServerAsync();

                //Set all convos' keyObtained to false when disconnecting by button for requesting new public keys;
                for (int j = 0; j < comm.convos.Count; j++)
                {
                    comm.convos[j].keyObtained = false;
                }

                //Remove all PMs/tabpages except Global tab; 
                foreach (TabPage page in tabControl1.TabPages)
                {
                    if (page.Name != "Global")
                    {
                        tabControl1.TabPages.Remove(page);
                    }
                }

                comm.convos.RemoveAll(x => x.name != "Global");

                btnConnect.Enabled = true;
            }

        }

        private void BtnSend_Click(object sender, EventArgs e)
        {

            if(StatusLabel.Text == "Online")
            {
                int i = comm.convos.FindIndex(x => x.name == tabControl1.SelectedTab.Name);

                //Check if the convo(/connection) with the selected tab's name exists and if a public key was obtained already;
                //Otherwise deny sending a message;
                if (i != -1 && comm.convos[i].keyObtained)
                {
                    if (!String.IsNullOrWhiteSpace(txtInput.Text))
                    {
                        string temp = txtInput.Text;

                        if (tabControl1.SelectedTab.Name != "Global")
                        {
                            string encryptedText = comm.secureChat.RSAEncrypt(temp, comm.convos[i].RSAParams);
                            StringBuilder sb = new StringBuilder();
                            sb.Append("/PM " + tabControl1.SelectedTab.Name);
                            sb.Append(" ");
                            sb.Append(encryptedText);

                            temp = sb.ToString();

                            comm.AddMessageToList(tabControl1.SelectedTab.Name, txtInput.Text);

                        }
                        else
                        {
                            string encryptedText = comm.secureChat.RSAEncrypt(temp, comm.convos[i].RSAParams);
                            StringBuilder sb = new StringBuilder();
                            sb.Append("/Global");
                            sb.Append(" ");
                            sb.Append(encryptedText);

                            temp = sb.ToString();
                        }

                        comm.SendMessage(temp);
                        txtInput.Clear();
                        txtInput.Refresh();
                        txtInput.Focus();
                    }
                }
                else if (i != -1 && !comm.convos[i].keyObtained)
                {
                    comm.SendMessage("/RequestKey " + comm.convos[i].name);
                    MessageBox.Show("Retrieving secure connection, please send again in 5 seconds.");
                }
                else
                {
                    MessageBox.Show("Error: Could not encrypt message. Message not sent.");

                    return;
                }
            }
            else
            {
                MessageBox.Show("Please connect before trying to send messages.");
            }

            


            
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            comm.SendMessage("/who");
        }

        private void BtnAddPm_Click(object sender, EventArgs e)
        {
            //comm.SendMessage("/PM " + lstOnline.SelectedItems[0]);
            if (lstOnline.SelectedItems.Count > 0)
            {
                int count = 0;

                for (int i = 0; i < tabControl1.Controls.Count; i++)
                {
                    if(tabControl1.Controls[i].Name == lstOnline.SelectedItems[0].Text)
                    {                        
                        count++;
                    }
                }
                if(count > 0)
                {
                    tabControl1.SelectTab(lstOnline.SelectedItems[0].Text);
                }
                else
                {
                    //Create tab for PM;
                    TabPage temp = new TabPage(lstOnline.SelectedItems[0].Text);
                    temp.Name = lstOnline.SelectedItems[0].Text;
                    tabControl1.Controls.Add(temp);

                    //Send public key share request to server;
                    comm.SendMessage("/RequestKey " + lstOnline.SelectedItems[0].Text);

                }
            }
            else
            {
                MessageBox.Show("Select a recipient in online list");
            }
                
        }

        private void UpdateLstMessages()
        {
            int k = comm.convos.FindIndex(x => x.name == tabControl1.SelectedTab.Name);
            if(k != -1)
            {
                for (int i = 0; i < comm.convos[k].messages.Count(); i++)
                {
                    if (!lstMessages.Items.Contains(comm.convos[k].messages[i]))
                    {
                        lstMessages.Items.Add(comm.convos[k].messages[i]);
                    }
                }
            }
        }

        private void UpdateLstOnline()
        {
            lstOnline.Clear(); 

            for (int i = 0; i < comm.onlineList.Count(); i++)
            {
                lstOnline.Items.Add(comm.onlineList[i]);
            }
        }

        private void CreateNewAccountToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            signup.ShowDialog();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Get selected tab's text value, fill lstMessages with a ListView collection corresponding to the text value (aka name of PMing user);
            lstMessages.Items.Clear();
            UpdateLstMessages();
        }

    }
}
