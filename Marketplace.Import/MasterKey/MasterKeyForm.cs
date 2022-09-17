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


            DataGridViewRow[] rows = AppSetting.PasswordManager.AllLogin.Select(CreateRow).ToArray();
            this.dataGridView1.Rows.AddRange(rows);
        }

        public class DataGridViewRowLogin : DataGridViewRow
        {
            public string Login { get; set; }
        }

        private DataGridViewRow CreateRow(string login)
        {
            DataGridViewRowLogin row = new DataGridViewRowLogin()
            {
                Login = login
            };
             
            row.Cells.Add(new DataGridViewTextBoxCell() { Value = login });
            row.Cells.Add(new DataGridViewTextBoxCell() { Value = _hideChangePasswor });

            return row;
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            bool change = false;
            List<string> removeLogins = AppSetting.PasswordManager.AllLogin.ToList();

            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                string oldLogin = string.Empty;
                if (row is DataGridViewRowLogin rowLogin)
                    oldLogin = rowLogin.Login;

                string login = row.Cells[0].Value?.ToString() ?? string.Empty;
                string password = row.Cells[1].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
                {
                    if (login != oldLogin && !string.IsNullOrEmpty(oldLogin))
                    {
                        AppSetting.PasswordManager.ChangeLogin(oldLogin, login);
                        change = true;
                    }

                    if (password != _hideChangePasswor || string.IsNullOrEmpty(oldLogin))
                    {
                        AppSetting.PasswordManager[login] = password;
                        change = true;
                    }

                    removeLogins.Remove(login);
                }
            }

            if (removeLogins.Count > 0)
            {
                foreach (string login in removeLogins)
                    AppSetting.PasswordManager.RemovePassword(login);
                change = true;
            }

            if (change)
                AppSetting.PasswordManager.SaveFile();

            this.Close();
        }

        private void MasterKeyForm_Load(object sender, EventArgs e)
        {

        }
    }
}
