using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DBFieldLookupValue[] values = GetNewValue();

            Console.WriteLine(string.Join(", ", values.Select(x => x.LookupID)));

            Console.ReadLine();
        }



        static private DBFieldLookupValue[] GetNewValue()
        {
            DBFieldLookupValue[] newValue = new DBFieldLookupValue[] { new DBFieldLookupValue { LookupID = 1 }, new DBFieldLookupValue { LookupID = 2 }, new DBFieldLookupValue { LookupID = 3 } };
            DBFieldLookupValue[] oldValue = new DBFieldLookupValue[] { new DBFieldLookupValue { LookupID = 1 }, new DBFieldLookupValue { LookupID = 3 } };

            int[] newIDs = oldValue.Select(x => x.LookupID).ToArray();

            DBFieldLookupValue[] items = newValue.Where(x => !newIDs.Contains(x.LookupID))
                  .Where(x => x != null)
                  .ToArray();

            return items;
        }

        public class DBFieldLookupValue
        {
            public int LookupID { get; set; }
        }
    }
}
