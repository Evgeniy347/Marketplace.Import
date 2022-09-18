using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marketplace.Import.Exceptions
{
    internal class MessageBoxExeption : Exception
    {
        public MessageBoxExeption()
        {
        }

        public MessageBoxExeption(string message) : base(message)
        {
        }

        public MessageBoxExeption(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MessageBoxExeption(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal void ShowMessageBox()
        { 
            MessageBox.Show(this.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
