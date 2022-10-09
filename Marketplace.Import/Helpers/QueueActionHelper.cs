using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Import.Helpers
{
    public abstract class QueueActionHelper<T>
    {
        private readonly BlockingCollection<T> _values = new BlockingCollection<T>();
        public void Enqueue(T item)
        {

        }
    }
}
