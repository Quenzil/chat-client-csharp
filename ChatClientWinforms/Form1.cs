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

        public Form1()
        {
            InitializeComponent();
            this.AcceptButton = btnSend;
            //Globals.messages = new List<ListViewItem>();
            comm = new ServerComms();
            //comm.Connected = false;
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

            signup = new RegisterForm();
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
                btnConnect.Enabled = true;
            }
            else
            {
                comm.DisconnectFromServerAsync();
                btnConnect.Enabled = true;
            }

        }

        private void BtnSend_Click(object sender, EventArgs e)
        {

            if (!String.IsNullOrWhiteSpace(txtInput.Text))
            {
                string temp = txtInput.Text;

                if(tabControl1.SelectedTab.Name != "Global")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("/PM " + tabControl1.SelectedTab.Name);
                    sb.Append(" ");
                    sb.Append(txtInput.Text);
                    
                    temp = sb.ToString();

                    comm.AddMessageToList(tabControl1.SelectedTab.Name, txtInput.Text);

                }

                comm.SendMessage(temp);
                txtInput.Clear();
                txtInput.Refresh();
                txtInput.Focus();

                UpdateLstMessages();
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
                    TabPage temp = new TabPage(lstOnline.SelectedItems[0].Text);
                    temp.Name = lstOnline.SelectedItems[0].Text;
                    tabControl1.Controls.Add(temp);                   
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
