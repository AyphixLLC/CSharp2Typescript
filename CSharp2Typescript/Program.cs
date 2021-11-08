using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharp2Typescript {
    class Program {

        static List<TSNamespace> Namespaces = new List<TSNamespace>();

        static TSNamespace CurrentNamespace;
        static void Main(string[] args) {
            foreach (string arg in args) {
                AssemblyName an = AssemblyName.GetAssemblyName(Path.Combine(Environment.CurrentDirectory, arg));
                Assembly assembly = Assembly.Load(an);
                List<Type> t = (from types in assembly.GetTypes() where types.IsClass && !types.Name.StartsWith("<") select types).GroupBy(x => x.Name).Select(y => y.First()).ToList<Type>();

                Program.CreateNamespace(t[0]);
                
                foreach (Type type in t) {
                    if (type == null) continue;

                    if(CurrentNamespace.Name != type.Namespace) {
                        Program.CreateNamespace(type);
                    }

                    TSClass clas = new TSClass();
                    clas.Name = Program.StripGenerics(type.Name);

                    if(type.BaseType.Name != "Object")
                        clas.BaseClass = Program.TransformType(Program.StripGenerics(type.BaseType.Name), type.GetGenericArguments());

                    clas.Modifier = (type.IsPublic) ? "public" : "private";
                    
                    foreach(Type g in type.GetGenericArguments()) {
                        clas.Generics.Add(Program.StripGenerics(g.Name));
                    }

                    CurrentNamespace.Classes.Add(clas);

                    List<FieldInfo> mi = (from members in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) where members.DeclaringType == type select members).ToList<FieldInfo>();

                    foreach(FieldInfo memInfo in mi) {
                        if (memInfo.Name.StartsWith("<") || memInfo.FieldType.Name.StartsWith("<") || memInfo.FieldType.Name.StartsWith("System.")) continue;
                        TSMember mem = new TSMember();
                        mem.Name = memInfo.Name;
                        mem.IsStatic = memInfo.IsStatic;
                        mem.Type = Program.TransformType(Program.StripGenerics(memInfo.FieldType.Name), memInfo.FieldType.GetGenericArguments());
                        clas.Members.Add(mem);
                    }

                    List<PropertyInfo> prop = (from properties in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) where properties.DeclaringType == type select properties).ToList<PropertyInfo>();

                    foreach (PropertyInfo propInfo in prop) {
                        if (propInfo.Name.StartsWith("<") || propInfo.PropertyType.Name.StartsWith("<") || propInfo.PropertyType.Name.StartsWith("System.")) continue;
                        TSProperty prope = new TSProperty();
                        prope.Name = propInfo.Name;
                        prope.Type = Program.TransformType(Program.StripGenerics(propInfo.PropertyType.Name), propInfo.PropertyType.GetGenericArguments());
                        clas.Properties.Add(prope);
                    }

                    List<MethodInfo> meths = (from methods in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) where methods.DeclaringType == type select methods).ToList<MethodInfo>();
                    foreach(MethodInfo methInfo in meths) {
                        if (methInfo.Name.StartsWith("<") || methInfo.Name.StartsWith("<") || methInfo.Name.StartsWith("System.")) continue;
                        TSMethod meth = new TSMethod();
                        meth.Name = Program.StripGenerics(methInfo.Name);
                        meth.ReturnType = Program.TransformType(Program.StripGenerics(methInfo.ReturnType.Name), methInfo.ReturnType.GetGenericArguments());
                        foreach(ParameterInfo pi in methInfo.GetParameters()) {
                            TSParameter para = new TSParameter();
                            para.Name = pi.Name;
                            para.Type = Program.TransformType(Program.StripGenerics(pi.ParameterType.Name), pi.ParameterType.GetGenericArguments());
                            meth.Parameters.Add(para);
                        }
                        clas.Methods.Add(meth);
                    }
                }
            }

            StringBuilder b = new StringBuilder();

            foreach(TSNamespace ns in Namespaces) {
                b.Append($"export namespace {ns.Name} {{\n");
                foreach(TSClass clas in ns.Classes) {
                    string extends = (clas.BaseClass != String.Empty) ? "extends " + clas.BaseClass : "";

                    b.Append($"\texport declare class {clas.Name} {extends} {{\n");

                    foreach (TSMember mem in clas.Members) {
                        b.Append($"\t\t{mem.Name}: {mem.Type};\n");
                    }

                    foreach (TSProperty prop in clas.Properties) {
                        b.Append($"\t\t{prop.Name}: {prop.Type};\n");
                    }

                    foreach(TSMethod meth in clas.Methods) {
                        b.Append($"\t\t{meth.Name}(");

                        List<string> paramS = new List<string>();

                        foreach(TSParameter param in meth.Parameters) {
                            paramS.Add(param.Name + ": " + param.Type);
                        }

                        b.Append(String.Join(",", paramS.ToArray()));

                        b.Append($"): {meth.ReturnType};\n");
                    }

                    b.Append("\t}\n\n");
                }
                b.Append("}\n\n");
            }

            File.WriteAllText("generated.ts", b.ToString());
        }

        static string TransformType(string type, Type[] generics) {
            type = type.Trim();

            if(type == "String") {
                return "string";
            }

            if(type == "Int32") {
                return "number";
            }

            if(type == "Boolean") {
                return "boolean";
            }

            if(type == "Object") {
                return "any";
            }

            if(type == "Void") {
                return "void";
            }

            if (type == "IEnumerable") {
                return "Array<any>";
            }

            if (type == "Dictionary") {
                if (generics.Length < 2) return "Dictionary";
                return "Map<"+Program.TransformType(Program.StripGenerics(generics[0].Name), generics[0].GetGenericArguments())+","+Program.TransformType(Program.StripGenerics(generics[1].Name), generics[1].GetGenericArguments())+">";
            }

            if(type == "List") {
                if (generics.Length < 1) return "[]";
                return "Array<" + Program.TransformType(Program.StripGenerics(generics[0].Name), generics[0].GetGenericArguments()) + ">";
            }

            if (type == "IList") {
                if (generics.Length < 1) return "[]";
                return "Array<" + Program.TransformType(Program.StripGenerics(generics[0].Name), generics[0].GetGenericArguments()) + ">";
            }

            return type;
        }

        static string StripGenerics(string name) {
            return (name.Contains("`")) ? name.Substring(0, name.IndexOf("`")).Trim() : name.Trim();
        }

        static void CreateNamespace(Type type) {
            TSNamespace fns = Program.GetExistingNamespace(type.Namespace);

            if(fns != null) {
                CurrentNamespace = fns;
                return;
            }

            TSNamespace ns = new TSNamespace();
            ns.Name = type.Namespace;
            CurrentNamespace = ns;
            Namespaces.Add(ns);
        }

        static TSNamespace GetExistingNamespace(string name) {
            TSNamespace ns = null;
            foreach (TSNamespace names in Namespaces) {
                if (names.Name == name) return names;
            }

            return null;
        }
    }
}
