using CefSharp.DevTools.CSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Marketplace.Import
{
    public class PasswordManager
    {
        private volatile bool _init;
        private string _fileName;
        private Dictionary<string, string> _values;
        public PasswordManager(string fileName)
        {
            _fileName = fileName;
        }


        public string this[string login]
        {
            get => GetPassword(login);
            set => _values[login] = value.Crypt();
        }

        public string[] AllLogin
        {
            get
            {
                Initializer();
                return _values.Keys.ToArray();
            }
        }

        public void ChangeLogin(string oldlogin, string newLogin)
        {
            if (!_values.TryGetValue(oldlogin, out string value))
                throw new Exception($"Не найден пароль для логина '{oldlogin}'");
            _values[newLogin] = value;
        }

        public void RemovePassword(string login)
        {
            _values.Remove(login);
        }

        public string GetPassword(string login)
        {
            Initializer();

            if (!_values.TryGetValue(login, out string value))
                throw new Exception($"Не найден пароль для логина '{login}'");

            return value.Decrypt();
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
                                .Select(x => x.Split(';'))
                                .ToDictionary(x => x.First(), x => x.Last());
                        }
                        else
                        {
                            _values = new Dictionary<string, string>();
                        }

                        _init = true;
                    }
                }
            }
        }

        public void SaveFile()
        {
            List<string> result = new List<string>(_values.Count + 1);
            result.Add("Login;Password");
            result.AddRange(_values.Select(x => $"{x.Key};{x.Value}"));

            File.WriteAllLines(_fileName, result, Encoding.UTF8);
        }
    }
}
