using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace onenotelink
{
    class OneNoteMemoryStream: MemoryStream
    {
        private int _readTimeOut = Int32.MaxValue;
        private int _writeTimeOut = Int32.MaxValue;

        public OneNoteMemoryStream(byte[] buffer) : base(buffer)
        {

        }

        public OneNoteMemoryStream() : base()
        {

        }

        public override int ReadTimeout
        {
            get {return _readTimeOut;}
            set {_readTimeOut = value;}
        }

        public override int WriteTimeout
        {
            get { return _writeTimeOut; }
            set { _writeTimeOut = value; }
        }
    }
}
