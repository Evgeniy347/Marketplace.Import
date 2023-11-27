using Marketplace.Import;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ConvertKey
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("start");
            try
            {

                if (args == null || args.Length == 0)
                {
                    string key = StringExtensions.GetKeyString();
                    Console.WriteLine($"CurrentKey:\"{key}\"");
                    Console.ReadLine();
                    return;
                }

                string oldKey = args.GetParam("oldKey");
                string newKey = args.GetParam("newKey");

                string inFile = args.GetParam("in");
                string outFile = args.GetParam("out");

                PasswordManager inManager = new PasswordManager(inFile);
                PasswordManager outManager = new PasswordManager(outFile);

                foreach (var oldCredential in inManager.Credentials)
                {
                    Console.WriteLine($"add '{oldCredential.ID}'");
                    CredentialEntry credential = outManager.CreateCredential(oldCredential.ID, oldCredential.Login); 
                    credential.CryptPassword = oldCredential.CryptPassword.Decrypt(oldKey).Crypt(newKey); 
                }

                outManager.SaveFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("end");
            Console.ReadLine();
        }



        public static string GetParam(this string[] args, string key, bool throwEx = true)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == $"-{key}")
                {
                    int ip = i + 1;

                    if (ip >= args.Length)
                        throw new Exception($"key index '{key}'");

                    return args[ip];
                }
            }

            if (throwEx)
                throw new Exception($"not found key '{key}'");

            return string.Empty;
        }
    }


    public class CredentialEntry
    {
        public string Login { get; set; }

        public string ID { get; set; }

        public string CryptPassword { get; set; }

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
                throw new Exception($"Не найден пароль для УЗ '{id}'");

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
                    throw new Exception("Поле CredentialID обязательно для заполнения");

                if (groupCredential.Count() > 1)
                    throw new Exception("Поле CredentialID должно быть уникально");
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
