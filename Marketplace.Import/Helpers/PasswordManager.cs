using CefSharp;
using CefSharp.DevTools.CSS;
using Marketplace.Import.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Marketplace.Import
{
    public class CredentialEntry
    {
        public string Login { get; set; }

        public string ID { get; set; }

        public string CryptPassword { get; set; }

        public void SetPassword(string pwd)
        {
            string netpwd = string.IsNullOrEmpty(pwd) ? string.Empty : pwd.Crypt();
            CryptPassword = netpwd;
        }

        public string GetPassword()
        {
            return CryptPassword.Decrypt();
        }

        public const string HeaderLine = "CredentialID;Login;Password";

        public override string ToString()
        {
            return $"{ID};{Login};{CryptPassword}";
        }

        public bool IsEmpty => string.IsNullOrEmpty(Login) && string.IsNullOrEmpty(ID) && string.IsNullOrEmpty(CryptPassword);
    }

    public class PasswordManager
    {
        private volatile bool _init;
        private string _fileName;
        private List<CredentialEntry> _values = new List<CredentialEntry>();

        public PasswordManager(string fileName)
        {
            _fileName = fileName;
        }


        public CredentialEntry[] Credentials
        {
            get
            {
                Initializer();
                return _values.ToArray();
            }
        }

        public void RemoveCredential(CredentialEntry login)
        {
            _values.Remove(login);
        }

        public void RemoveCredential(string id)
        {
            _values.RemoveAll(x => x.ID == id);
        }

        public CredentialEntry GetCredential(string id)
        {
            Initializer();

            CredentialEntry result = _values.FirstOrDefault(x => x.ID == id);

            if (result == null)
                throw new MessageBoxExeption($"Не найден пароль для УЗ '{id}'");

            return result;
        }

        private void Initializer()
        {
            if (!_init)
            {
                lock (this)
                {
                    if (!_init)
                    {
                        if (File.Exists(_fileName))
                        {
                            _values = File.ReadAllLines(_fileName)
                                .Skip(1)
                                .Select(x => x?.Split(';'))
                                .Where(x => x != null && x.Length == 3)
                                .Select(x => InitCredentialEntry(x[0], x[1], x[2]))
                                .ToList();
                        }
                        else
                        {
                            _values = new List<CredentialEntry>();
                        }

                        _init = true;
                    }
                }
            }
        }

        public CredentialEntry CreateCredential(string id = null, string login = null, string pwd = null)
        {
            CredentialEntry credential = new CredentialEntry()
            {
                ID = id,
                Login = login,
                CryptPassword = pwd
            };

            _values.Add(credential);
            return credential;
        }

        private static CredentialEntry InitCredentialEntry(string id, string login, string pwd)
        {
            CredentialEntry credential = new CredentialEntry()
            {
                ID = id,
                Login = login,
                CryptPassword = pwd
            };

            return credential;
        }

        public void SaveFile()
        {
            CredentialEntry[] saveValues = _values.Distinct().ToArray();


            foreach (var groupCredential in saveValues.GroupBy(x => x.ID))
            {
                if (string.IsNullOrEmpty(groupCredential.Key))
                    throw new MessageBoxExeption("Поле CredentialID обязательно для заполнения");

                if (groupCredential.Count() > 1)
                    throw new MessageBoxExeption("Поле CredentialID должно быть уникально");
            }

            List<string> result = new List<string>(_values.Count + 1)
            {
                CredentialEntry.HeaderLine
            };
            result.AddRange(_values.Select(x => x.ToString()));
            File.WriteAllLines(_fileName, result, Encoding.UTF8);
        }
    }
}
