using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    public class VariableBundle
    {
        //private Dictionary<string, int> idict = new Dictionary<string, int>();
        //private Dictionary<string, string> sdict = new Dictionary<string, string>();
        //private Dictionary<string, float> fdict = new Dictionary<string, float>();
        private Dictionary<string, object> odict = new Dictionary<string, object>();

        public VariableBundle copy()
        {
            VariableBundle variableBundleNew = new VariableBundle();
            variableBundleNew.odict = new Dictionary<string, object>();
            foreach (string key in odict.Keys)
                variableBundleNew.odict.Add(key, odict[key]);
            return variableBundleNew;
        }

        public List<string> variables
        {
            get { return odict.Keys.ToList(); }
        }

        public T getValue<T>(string variable)
        {
            T retval;
            getValue(variable, out retval);
            return retval;
        }

        public bool getValue<T>(string variable, out T val)
        {
            val = default(T);

            object oval;
            if (!odict.TryGetValue(variable, out oval))
                return false;

            val = (T)oval;
            return true;
        }

        public void setValue<T>(string variable, T val)
        {
            odict[variable] = val;
        }

        public void merge(VariableBundle variableBundleToMerge)
        {
            merge(variableBundleToMerge, new List<string>());
        }

        public void merge(VariableBundle variableBundleToMerge, List<string> variablesToMerge)
        {
            if ((variableBundleToMerge == null) || (variableBundleToMerge == this))
                return;

            if ((variablesToMerge == null) || (variablesToMerge.Count <= 0))
                return;

            Dictionary<string, bool> dictVariableToMerge = variablesToMerge.ToDictionary(s => s, s => true);

            // TODO: Handle other than ints, eventually...
            foreach (string variable in variableBundleToMerge.variables)
            {
                if ((dictVariableToMerge.Count > 0) && !dictVariableToMerge.ContainsKey(variable))
                    continue;

                int existingValue = getValue<int>(variable);
                setValue(variable, existingValue + variableBundleToMerge.getValue<int>(variable));
            }                
        }

        public void moveValue(string variable, int amount, VariableBundle target)
        {
            // TODO: Handle other than ints, eventually...
            int existing = getValue<int>(variable);
            if (existing <= 0)
                return;

            int transfer = amount;
            if (existing < transfer)
                transfer = existing;

            target.setValue(variable, target.getValue<int>(variable) + transfer);
            setValue(variable, existing - transfer);
        }

        public void adjustValue(string variable, int amount)
        {
            int existing = getValue<int>(variable);
            setValue(variable, existing + amount);
        }
    }

    public class Condition
    {
        // TODO: Do private variables and handle persistence (and string for enum types)...

        public string variable 
        { 
            get { return _variable; }
            set { _variable = value; }
        }
        private string _variable;

        public VariableType type
        {
            get { return _type; }
            set { _type = value; } 
        }
        private VariableType _type;

        public Operation operation 
        {
            get { return _operation; }
            set { _operation = value; } 
        }
        private Operation _operation;

        public List<string> values 
        {
            get { return _values; }
            set { _values = value; }
        }
        private List<string> _values;

        public Condition() { }
        public Condition(string variable, VariableType type, Operation operation, string val) : this(variable, type, operation, new List<string>(new string[] { val })) { }
        public Condition(string variable, VariableType type, Operation operation, List<string> values)
        {
            _variable = variable;
            _type = type;
            _operation = operation;
            _values = values;
        }

        public bool isValid(VariableBundle gameState)
        {
            // TODO: Handle float.
            switch (_type)
            {
                case VariableType.Integer:
                    {
                        int val;
                        if (!gameState.getValue(_variable, out val))
                            return false;

                        int[] values = new int[0];
                        if ((_values != null) && (_values.Count > 0))
                        {
                            values = new int[_values.Count];
                            for (int i = 0; i < _values.Count; i++)
                                values[i] = int.Parse(_values[i]);
                        }
                        if (values.Length <= 0)
                            return false;

                        switch (_operation)
                        {
                            case Operation.Equals:
                            {
                                for (int i = 0; i < values.Length; i++)
                                {
                                    if (val == values[i])
                                        return true;
                                }
                                break;
                            }
                            case Operation.ExclusiveNotBetween:
                            {
                                if (values.Length >= 2)
                                {
                                    if ((val < values[0]) || (val > values[1]))
                                        return true;
                                }
                                break;
                            }
                            case Operation.GreaterThan:
                            {
                                if (val > values[0])
                                    return true;
                                break;
                            }
                            case Operation.GreatherThanEqual:
                            {
                                if (val >= values[0])
                                    return true;
                                break;
                            }
                            case Operation.InclusiveBetween:
                            {
                                if (values.Length >= 2)
                                {
                                    if ((val >= values[0]) && (val <= values[1]))
                                        return true;
                                }
                                break;
                            }
                            case Operation.LessThan:
                            {
                                if (val < values[0])
                                    return true;
                                break;
                            }
                            case Operation.LessThanEqual:
                            {
                                if (val <= values[0])
                                    return true;
                                break;
                            }
                            case Operation.NotEquals:
                            {
                                bool found = false;
                                for (int i = 0; i < values.Length; i++)
                                {
                                    if (val == values[i])
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                    return true;
                                break;
                            }
                            default:
                                break;
                        }
                        break;
                    }
                case VariableType.String:
                    {
                        string val;
                        if (!gameState.getValue(_variable, out val))
                            return false;

                        if ((_values == null) || (_values.Count <= 0))
                            return false;

                        switch (_operation)
                        {
                            case Operation.Equals:
                                {
                                    for (int i = 0; i < _values.Count; i++)
                                    {
                                        if (val == values[i])
                                            return true;
                                    }
                                    break;
                                }
                            case Operation.NotEquals:
                                {
                                    bool found = false;
                                    for (int i = 0; i < _values.Count; i++)
                                    {
                                        if (val == values[i])
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                        return true;
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                default:
                    break;
            }

            return false;
        }
    }

    public enum Operation { Equals, NotEquals, LessThan, LessThanEqual, GreaterThan, GreatherThanEqual, InclusiveBetween, ExclusiveNotBetween }
    public enum VariableType { String, Integer, Float }
}
