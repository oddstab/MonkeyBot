using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pixeez2.Objects
{
    public interface IHasUser
    {
        User User { get; set; }
    }

}
