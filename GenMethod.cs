using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarketScraper
{
    class GenMethod
    {

        public static string GenMethodString(string className)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\nprivate static void Print");
            sb.Append(className);
            sb.Append("(");
            sb.Append(className);
            sb.Append(" msg, object tag) {\n");
            sb.Append(ClassVarsToString(className));
            sb.Append("}");
            return sb.ToString();
        }

        private static string PropertyInfoToLine(PropertyInfo p)
        {
            StringBuilder sb = new StringBuilder();
            string name = p.Name;
            Type type = p.PropertyType;
            bool hasToString = type.GetMethod("toString").DeclaringType == type;
            bool isPrim = type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
            sb.Append("\t");
            if (!hasToString && !isPrim)
            {
                sb.Append(@"//");
            }
            sb.Append("Console.WriteLine(\"");
            sb.Append(name);
            sb.Append("={0}\",msg.");
            sb.Append(name);
            sb.Append(@"); //");
            sb.Append(type);
            if (hasToString)
            {
                sb.Append(" has a toString()");
            }
            sb.Append("\n");
            return sb.ToString();
        }

        private static string ClassVarsToString(string className)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\tConsole.WriteLine(\"");
            sb.Append(className);
            sb.Append(":\");\n");
            PropertyInfo[] properties = Type.GetType(className).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            HashSet<string> publicProps = new HashSet<string>();
            foreach (PropertyInfo p in properties)
            {
                sb.Append(PropertyInfoToLine(p));
                publicProps.Add(p.Name.ToLower());
            }
            properties = Type.GetType(className).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo p in properties)
            {
                String name = p.Name;
                String lowerCase = name.ToLower();
                String type = p.PropertyType.Name;
                if (!publicProps.Contains(lowerCase))
                {
                    sb.Append(type);
                    sb.Append(" ");
                    sb.Append(name);
                    sb.Append(@" = GetPrivateProperty<");
                    sb.Append(type);
                    sb.Append(">(msg, \"");
                    sb.Append(name);
                    sb.Append("\");\n");
                    sb.Append(PropertyInfoToLine(p));
                }
            }
            return sb.ToString();
        }

    }
}
