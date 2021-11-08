using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp2Typescript {
    public class TSMethod {

        public string Name;
        public string Modifier;
        public string ReturnType;
        public bool IsStatic;

        public List<TSParameter> Parameters = new List<TSParameter>();

    }
}
