using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AmiBroker.Controllers;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleApp1
{
    public static class Reflection
    {

        /// <summary>
        /// Gets a property's parent object
        /// </summary>
        /// <param name="SourceObject">Source object</param>
        /// <param name="PropertyPath">Path of the property (ex: Prop1.Prop2.Prop3 would be
        /// the Prop1 of the source object, which then has a Prop2 on it, which in turn
        /// has a Prop3 on it.)</param>
        /// <param name="PropertyInfo">Property info that is sent back</param>
        /// <returns>The property's parent object</returns>
        public static object GetPropertyParent(object SourceObject, string PropertyPath, out PropertyInfo PropertyInfo)
        {
            try
            {
                if (SourceObject == null)
                {
                    PropertyInfo = null;
                    return null;
                }
                string[] Splitter = { "." };
                string[] SourceProperties = PropertyPath.Split(Splitter, StringSplitOptions.None);
                object TempSourceProperty = SourceObject;
                Type PropertyType = SourceObject.GetType();
                PropertyInfo = PropertyType.GetProperty(SourceProperties[0]);
                PropertyType = PropertyInfo.PropertyType;
                for (int x = 1; x < SourceProperties.Length; ++x)
                {
                    if (TempSourceProperty != null)
                    {
                        TempSourceProperty = PropertyInfo.GetValue(TempSourceProperty, null);
                    }
                    PropertyInfo = PropertyType.GetProperty(SourceProperties[x]);
                    PropertyType = PropertyInfo.PropertyType;
                }
                return TempSourceProperty;
            }
            catch { throw; }
        }

    }
    class Test
    {
        public string Name { get; set; } = "test";
        public List<string> Names { get; set; } = new List<string>();
    }
    class Program
    {
       
        [STAThread]
        static void Main(string[] args)
        {
            OrderManager om = new OrderManager();
            MainViewModel.Instance.test();

            //IBController controller = new IBController(MainViewModel.Instance);
            //controller.test();
            Console.ReadLine();
            
            
            //Console.ReadLine();
            /*
            Test test = new Test();
            test.Names.Add("chan");
            List<string> t = test.Names;
            Test test1 = null;
            PropertyInfo pi;
            test1 = (Test)Reflection.GetPropertyParent(t, "Test.Names", out pi);*/
            Regex r1 = new Regex(@"(?<dateFormat>y*[-\/]*M*[-\/]*[Dd]*)(?<dateFormat>M*[-\/]*[Dd]*[-\/]*y*)(?<dateFormat>[Dd]*[-\/]*M*[-\/]*y*)(?<timeFormat>[Hh]*[:]*m*[:]*s*[ ]*t*)");
            Regex r2 = new Regex(@"([Hh]*[:]*m*[:]*s*[ ]*t*)");
            //var p1 = new PcreRegex(@"(y*[-\/]*M*[-\/]*[Dd]*)(M*[-\/]*[Dd]*[-\/]*y*)([Dd]*[-\/]*M*[-\/]*y*)([Hh]*[:]*m*[:]*s*[ ]*t*)");
            string input = "MMDDyyyy HH:mm:ss";
            MatchCollection match = r1.Matches(input);
            Match m2 = r2.Match(input);
            //var m3 = p1.Match(input);
            
            if (m2.Success)
            {
                //string v = match.Groups[1].Value;
                //Console.WriteLine("Between One and Three: {0}", v);
            }
        }
    }
}
