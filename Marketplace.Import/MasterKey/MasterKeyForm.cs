using Marketplace.Import.Exceptions;
using Marketplace.Import.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marketplace.Import.MasterKey
{
    public partial class MasterKeyForm : Form
    {
        private readonly string _hideChangePasswor = new string('*', 10);

        public MasterKeyForm()
        {
            InitializeComponent();
            ResizeFormHelper.Instance.AddFixControl(button_Save);
            ResizeFormHelper.Instance.AddResizeControl(dataGridView1);


            DataGridViewRow[] rows = AppSetting.PasswordManager.Credentials.Select(CreateRow).ToArray();
            this.dataGridView1.Rows.AddRange(rows);
        }

        public class DataGridViewRowLogin : DataGridViewRow
        {
            public CredentialEntry Credential { get; set; }
        }

        private DataGridViewRow CreateRow(CredentialEntry credential)
        {
            DataGridViewRowLogin row = new DataGridViewRowLogin()
            {
                Credential = credential
            };

            row.Cells.Add(new DataGridViewTextBoxCell() { Value = credential.ID });
            row.Cells.Add(new DataGridViewTextBoxCell() { Value = credential.Login });
            row.Cells.Add(new DataGridViewTextBoxCell() { Value = _hideChangePasswor });

            return row;
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            List<CredentialEntry> removeCredentials = AppSetting.PasswordManager.Credentials.ToList();

            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                CredentialEntry credential;
                if (row is DataGridViewRowLogin rowLogin)
                    credential = rowLogin.Credential;
                else
                {
                    credential = AppSetting.PasswordManager.CreateCredential();
                    removeCredentials.Add(credential);
                }

                int i = 0;
                credential.ID = row.Cells[i++].Value?.ToString() ?? string.Empty;
                credential.Login = row.Cells[i++].Value?.ToString() ?? string.Empty;
                string password = row.Cells[i++].Value?.ToString() ?? string.Empty;

                if (password != _hideChangePasswor)
                    credential.SetPassword(password);

                if (!credential.IsEmpty)
                    removeCredentials.Remove(credential);
            }

            if (removeCredentials.Count > 0)
            {
                foreach (CredentialEntry credential in removeCredentials)
                    AppSetting.PasswordManager.RemoveCredential(credential);
            }

            try
            {
                AppSetting.PasswordManager.SaveFile();
                this.Close();
            }
            catch (MessageBoxExeption ex)
            {
                ex.ShowMessageBox();
            }
        }

        private void MasterKeyForm_Load(object sender, EventArgs e)
        {

        }
    }
}
