using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Data;
using System.Windows;
using System.Text.RegularExpressions;

namespace AmiBroker.Controllers
{
    [ContentProperty("Bindings")]
    public class BcpBinding : MarkupExtension
    {
        private const string BINDING_EXTRACT_PATH_REGEX_PATTERN = @"Path=(?<path>(?(\.)[A-Za-z0-9]+)?(\[[0-9]+\])?(\.)?([A-Za-z0-9]+)?(\.)?([A-Za-z0-9]+)?(\.)?([A-Za-z0-9]+)?)";
        private const string ENUM_REGEX_PATTERN = @"Enum [A-Za-z0-9\.]+";
        private const string ENUM_EXTRACT_ENUM_TYPE_NAME_REGEX_PATTERN = @"Enum (?<typename>[A-Za-z0-9\.]+)";
        private const string BINDING_OR_ENUM_REGEX_PATTERN = "(" + "Binding" + ")?" + "(" + ENUM_REGEX_PATTERN + ")?";

        private const string BINDING_EXTRACT_ELEMENT_NAME_REGEX_PATTERN = @"ElementName=(?<elementname>([A-Za-z0-9]+))";

        public BcpBinding()
        {
            Mode = BindingMode.OneWay;
        }

        private static Dictionary<DependencyObject, List<BcpBinding>> dictAllDependencyObjectsBcpBindings = new Dictionary<DependencyObject, List<BcpBinding>>();


        public string Bindings { get; set; }
        public string ConverterParameters { get; set; }
        public object Converter { get; set; }
        public string Path { get; set; }
        public BindingMode Mode { get; set; }
        public string ElementName { get; set; }

        public DependencyProperty TargetDependencyProperty { get; set; }




        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IProvideValueTarget pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            FrameworkElement TargetObject = pvt.TargetObject as FrameworkElement;
            if (!dictAllDependencyObjectsBcpBindings.ContainsKey(TargetObject)) // this should allow us to register to Initialized only one time per object
            {
                TargetObject.Initialized += TargetObject_Initialized;
                TargetObject.Unloaded += TargetObject_Unloaded;
                dictAllDependencyObjectsBcpBindings.Add(TargetObject, new List<BcpBinding>());
            }
            this.TargetDependencyProperty = pvt.TargetProperty as DependencyProperty;
            dictAllDependencyObjectsBcpBindings[TargetObject].Add(this);


            return null;// there is no actual need to return value, as we will change this dependency-property
        }

        void TargetObject_Initialized(object sender, EventArgs e)
        {
            DependencyObject TargetDependencyObject = sender as DependencyObject;

            foreach (var objBcpBinding in dictAllDependencyObjectsBcpBindings[TargetDependencyObject])
            {
                UpdateDependencyObjectDependencyPropertyWithAttachedPropertiesBasedReplacement(TargetDependencyObject, objBcpBinding);
            }
        }

        void TargetObject_Unloaded(object sender, RoutedEventArgs e)
        {
            ////SOME MEMORY CLEANUP
            (sender as FrameworkElement).Initialized -= TargetObject_Initialized;
            (sender as FrameworkElement).Unloaded -= TargetObject_Unloaded;
            dictAllDependencyObjectsBcpBindings.Remove(sender as DependencyObject);
        }

        private void UpdateDependencyObjectDependencyPropertyWithAttachedPropertiesBasedReplacement(DependencyObject item, BcpBinding BcpBindingN)
        {
            DependencyProperty dp = BcpBindingN.TargetDependencyProperty;

            List<DependencyProperty> ConverterParameters = new List<DependencyProperty>();
            List<DependencyProperty> Bindings = new List<DependencyProperty>();

            //(5.) attached prop for two-way binding operations
            DependencyProperty apIsSourceChanged = GetOrCreateAttachedProperty(dp.Name + "IsSourceChanged", typeof(bool), false);

            //(6.) attached prop that stores all ConverterParameters-bindings
            DependencyProperty apConverterParameters = GetOrCreateAttachedProperty(dp.Name + "ConverterParameters", typeof(List<DependencyProperty>), new List<DependencyProperty>());
            //(7.) attached prop that stores all MultiBinding-bindings
            DependencyProperty apBindings = GetOrCreateAttachedProperty(dp.Name + "Bindings", typeof(List<DependencyProperty>), new List<DependencyProperty>());


            // 1. attached-prop for the ConverterParameter-Binding
            // if ',' is pressent assume comma-seperated values. this is usefull - for mixed values(both static values and bindings) or for single line, multiple parameters syntax, in xaml
            string[] arrConverterParameters;
            if (BcpBindingN.ConverterParameters.Contains(','))
            {
                arrConverterParameters = BcpBindingN.ConverterParameters.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                var bindings = Regex.Split(BcpBindingN.ConverterParameters, @"Binding ").Where(s => s != "").Select(s => "Binding " + s);
                arrConverterParameters = bindings.ToArray();
            }

            Match match = null;
            for (int i = 0; i < arrConverterParameters.Length; i++)
            {
                DependencyProperty apConverterParameterBindingSource =
                 GetOrCreateAttachedProperty(dp.Name + "ConverterParameterBindingSource" + i.ToString(), typeof(object), null);

                ConverterParameters.Add(apConverterParameterBindingSource);

                match = Regex.Match(arrConverterParameters[i], BINDING_EXTRACT_PATH_REGEX_PATTERN);
                if (match.Groups["path"].Value != "")
                {
                    string spath = match.Groups["path"].Value;
                    Binding b = new Binding(spath);


                    match = Regex.Match(arrConverterParameters[i], BINDING_EXTRACT_ELEMENT_NAME_REGEX_PATTERN);
                    if (match.Groups["elementname"].Value != "")
                    {
                        b.ElementName = match.Groups["elementname"].Value;
                    }
                    BindingOperations.SetBinding(item, apConverterParameterBindingSource, b);
                }
                else
                {
                    match = Regex.Match(arrConverterParameters[i], ENUM_EXTRACT_ENUM_TYPE_NAME_REGEX_PATTERN);
                    if (match.Groups["typename"].Value != "")
                    {
                        string stypename = match.Groups["typename"].Value;
                        Type t = Type.GetType(stypename);
                        item.SetValue(apConverterParameterBindingSource, t);
                    }
                    else
                    {
                        item.SetValue(apConverterParameterBindingSource, arrConverterParameters[i]);
                    }
                }
            }
            item.SetValue(apConverterParameters, ConverterParameters);


            // 2. attached-prop to hold the Converter Object
            DependencyProperty apConverter = GetOrCreateAttachedProperty(dp.Name + "Converter", typeof(object), null);
            item.SetValue(apConverter, BcpBindingN.Converter);

            //3. attached-prop to hold the evaluate result >>> will be binded to the original Binded dp
            DependencyProperty apEvaluatedResult =
                GetOrCreateAttachedProperty(dp.Name + "EvaluatedResult", typeof(object), null, apEvaluatedResultChanged);

            Binding bindingOrigDpToEvaluatedResult = new Binding("(" + typeof(BindingBase).Name + "." + dp.Name + "EvaluatedResult)");
            bindingOrigDpToEvaluatedResult.Source = item;
            bindingOrigDpToEvaluatedResult.Mode = BcpBindingN.Mode;// IsSingleBinding ? ((Binding)bindingOrig).Mode : ((MultiBinding)bindingOrig).Mode;
            BindingOperations.SetBinding(item, dp, bindingOrigDpToEvaluatedResult);


            // 4. attached-prop to replace the source binding  
            //  BindingBase NewBindingToSource;
            Binding NewBindingToSource;

            // for single-binding scenarios >>> make it similar to multibinding syntax(with single binding)
            if (BcpBindingN.Path != null)
            {
                BcpBindingN.Bindings = "Binding " + (BcpBindingN.ElementName != "" ? "ElementName=" + BcpBindingN.ElementName : "") + " Path=" + BcpBindingN.Path;
            }
            else
            {
                if (!BcpBindingN.Bindings.Contains("Binding"))
                {
                    BcpBindingN.Bindings = "Binding Path=" + BcpBindingN.Bindings;
                }
            }

            //bind each Binding in the MultiBinding to special dp (instead of the original dp)

            var binds = Regex.Split(BcpBindingN.Bindings, @"Binding ").Where(s => s != "");
            int i1 = 0;
            foreach (var binding in binds)
            {
                string spath = Regex.Match(binding, BINDING_EXTRACT_PATH_REGEX_PATTERN).Groups["path"].Value;
                string selementname = Regex.Match(binding, BINDING_EXTRACT_ELEMENT_NAME_REGEX_PATTERN).Groups["elementname"].Value;
                DependencyProperty apBinding = GetOrCreateAttachedProperty(dp.Name + "_Binding" + i1.ToString(), typeof(object), null, apMultiBindingAnySourceChanged);
                NewBindingToSource = new Binding(spath);
                if (selementname != "") NewBindingToSource.ElementName = selementname;

                ((Binding)NewBindingToSource).Mode = BcpBindingN.Mode;
                BindingOperations.SetBinding(item, apBinding, NewBindingToSource);
                Bindings.Add(apBinding);
                i1++;
            }
            //save all bindings-dps into apBindings
            item.SetValue(apBindings, Bindings);
        }


        private static void apMultiBindingAnySourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            string dpName = e.Property.Name.Split('_')[0];//.Replace("apEvaluatedResult","");
            DependencyProperty apIsSourceChanged = GetOrCreateAttachedProperty(dpName + "IsSourceChanged");
            obj.SetValue(apIsSourceChanged, true);
            DependencyProperty apConverterParameters = GetOrCreateAttachedProperty(dpName + "ConverterParameters");
            DependencyProperty apBindings = GetOrCreateAttachedProperty(dpName + "Bindings");
            List<DependencyProperty> ConverterParameters = (List<DependencyProperty>)obj.GetValue(apConverterParameters);// new List<DependencyProperty>();
            List<DependencyProperty> Bindings = (List<DependencyProperty>)obj.GetValue(apBindings);//


            IEnumerable<object> v1 = ConverterParameters.Select(dpcp => { return obj.GetValue(dpcp); });
            IEnumerable<object> v2 = Bindings.Select(dpcp => { return obj.GetValue(dpcp); });

            DependencyProperty apConverter = GetOrCreateAttachedProperty(dpName + "Converter");
            DependencyProperty apEvaluatedResult = GetOrCreateAttachedProperty(dpName + "EvaluatedResult");

            object converter = obj.GetValue(apConverter);
            if (converter is IMultiValueConverter)
            {
                obj.SetValue(apEvaluatedResult, (converter as IMultiValueConverter).Convert(v2.ToArray(), null, v1.ToArray(), null));
            }
            else
            {
                obj.SetValue(apEvaluatedResult, (converter as IValueConverter).Convert(e.NewValue, null, v1.ToArray(), null));
            }
            obj.SetValue(apIsSourceChanged, false);
        }

        private static void apEvaluatedResultChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            string dpName = e.Property.Name.Replace("EvaluatedResult", "");
            DependencyProperty apIsSourceChanged = GetOrCreateAttachedProperty(dpName + "IsSourceChanged");
            if (!(bool)obj.GetValue(apIsSourceChanged))
            {
                // change didn't come from source>>> target got changed in two-way binding
                // change source via convert back
                DependencyProperty apConverterParameters = GetOrCreateAttachedProperty(dpName + "ConverterParameters");
                List<DependencyProperty> ConverterParameters = (List<DependencyProperty>)obj.GetValue(apConverterParameters);// new List<DependencyProperty>();
                IEnumerable<object> v1 = ConverterParameters.Select(dpcp => { return obj.GetValue(dpcp); });
                DependencyProperty apBindings = GetOrCreateAttachedProperty(dpName + "Bindings");
                List<DependencyProperty> Bindings = (List<DependencyProperty>)obj.GetValue(apBindings);//

                DependencyProperty apConverter = GetOrCreateAttachedProperty(dpName + "Converter");
                object converter = obj.GetValue(apConverter);
                if (converter is IValueConverter)
                {
                    object ret = (obj.GetValue(apConverter) as IValueConverter).ConvertBack(e.NewValue, null, v1.ToArray(), null);
                    obj.SetValue(Bindings[0], ret);
                }
                else
                {
                    object[] ret = (obj.GetValue(apConverter) as IMultiValueConverter).ConvertBack(e.NewValue, null, v1.ToArray(), null);
                    for (int i = 0; i < ret.Length; i++)
                    {
                        obj.SetValue(Bindings[i], ret[i]);
                    }
                }
            }
        }

        // this Dictionary holds every Attached-Property we've created, so we can use it later
        private static Dictionary<string, DependencyProperty> RegistredDependencyProperties = new Dictionary<string, DependencyProperty>();// = new KeyValuePair<string, DependencyProperty>();
        private static DependencyProperty GetOrCreateAttachedProperty(string sDpName, Type t = null, object defaulevalue = null, PropertyChangedCallback cb = null)
        {
            if (RegistredDependencyProperties.ContainsKey(sDpName))
            {
                return RegistredDependencyProperties[sDpName];
            }
            else
            {
                DependencyProperty dp = DependencyProperty.RegisterAttached(sDpName, t, typeof(BindingBase), new PropertyMetadata(defaulevalue, cb));
                RegistredDependencyProperties.Add(sDpName, dp);
                return dp;
            }
        }
    }
}

