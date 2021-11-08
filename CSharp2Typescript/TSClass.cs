using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp2Typescript {
    public class TSClass {
        public string Name;
        public string Modifier;
        public string BaseClass = String.Empty;
        public List<string> Generics = new List<string>();

        public List<TSMethod> Methods = new List<TSMethod>();
        public List<TSProperty> Properties = new List<TSProperty>();
        public List<TSMember> Members = new List<TSMember>();

    }
}
