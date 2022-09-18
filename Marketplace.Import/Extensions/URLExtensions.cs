using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Import
{
    internal static class URLExtensions
    {
        public static bool CheckHostMask(this string url, params string[] masks)
        {
            Uri uri = null;
            try
            { 
                uri = new Uri(url);
            }
            catch
            {
                return false;
            }

            return masks.Any(x => uri.Host.Equals(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}
