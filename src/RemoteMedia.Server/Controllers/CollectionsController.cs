using Microsoft.AspNet.Mvc;
using RemoteMedia.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteMedia.Server.Controllers
{
    public class CollectionsController : Controller
    {
        [HttpGet("collections/{collection}/{*path}")]
        public CollectionItem Get(string collection, string path)
        {
            return new CollectionItemFolder();
        }
    }
}
