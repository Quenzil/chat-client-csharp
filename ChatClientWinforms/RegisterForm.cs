using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClientWinforms
{
    public partial class RegisterForm : Form
    {
        RegistrationComms rComms;

        public RegisterForm()
        {
            InitializeComponent();
            rComms = new RegistrationComms();
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {

        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            btnRegister.Text = "Processing...";
            btnRegister.Enabled = false;
            btnCancel.Enabled = false;

            if (txtEmail.Text == "" || txtPassword1.Text == "" || txtPassword2.Text == "" || txtName.Text == "")
            {
                MessageBox.Show("Please fill in all the fields.");
                return;
            }
            else if (txtPassword1.Text != txtPassword2.Text)
            {
                MessageBox.Show("Password and Repeat Password are not the same. Please try again.");
                return;
            }
            else if(txtPassword1.Text.Contains(";") || txtPassword1.Text.Contains("'") || txtPassword1.Text.Contains("--") || txtPassword1.Text.Contains("/*") ||
                 txtPassword1.Text.Contains("*/") || txtPassword1.Text.Contains("*") || txtPassword1.Text.Contains("xp_"))
            {
                MessageBox.Show("Invalid characters used, please only use a-z, 1-9");
                return;
            }
            else
            {
                //MessageBox.Show("Proceed.");
                string temp = rComms.ConnectToServer(txtEmail.Text, txtPassword1.Text, txtName.Text);

                MessageBox.Show(temp);
            }


            btnRegister.Enabled = true;
            btnCancel.Enabled = true;
            btnRegister.Text = "Register";
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
