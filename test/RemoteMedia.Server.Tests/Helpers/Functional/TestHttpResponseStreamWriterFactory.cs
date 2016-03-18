using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteMedia.Server.Tests.Helpers.Functional
{
    public class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        public TextWriter CreateWriter(Stream stream, Encoding encoding)
        {
            return new HttpResponseStreamWriter(stream, encoding);
        }
    }
}
