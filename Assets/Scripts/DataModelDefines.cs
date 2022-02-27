using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DataTypes
{
    NUMBER = 0x0,
    STRING = 0x1,
    ENUM = 0x2,
    BOOL = 0x4,
    NULL = 0x8
}

public class DataPoint
{
    private string _rawValue;
    public string RawValue { get { return _rawValue; } }

    public DataTypes type { get; private set; }

    private readonly float defaultFloat = 0.0f;


    public DataPoint(string value)
    {
        _rawValue = value;
        _rawValue = _rawValue.TrimStart(new char[] { '"' });
        _rawValue = _rawValue.TrimEnd(new char[] { '"' });

        bool floatResult = float.TryParse(_rawValue, out float i);
        bool boolResult = bool.TryParse(_rawValue, out bool j);
        if (floatResult) type = DataTypes.NUMBER;
        else if (boolResult) type = DataTypes.BOOL;
        else type = DataTypes.STRING;
    }

    public bool GetBool()
    {
        switch(type)
        {
            case DataTypes.NUMBER:
                float val = float.Parse(_rawValue);
                if (val > 0.0f) return true;
                else return false;
            case DataTypes.BOOL:
                return bool.Parse(_rawValue);
            case DataTypes.STRING:
                return true;
            default:
                return false;
        }
    }

    public float GetFloat()
    {
        switch(type)
        {
            case DataTypes.NUMBER:
                return float.Parse(_rawValue);
            case DataTypes.BOOL:
                bool val = bool.Parse(_rawValue);
                if (val) return 1.0f;
                else return 0.0f;
            case DataTypes.STRING:
                return float.NaN;
            default:
                return float.NaN;
        }
    }

    public override string ToString()
    {
        return _rawValue;
    }
}

public class DataRecord
{
    private DataPoint[] _points;
    private List<DataTypes> types;

    public DataRecord(int n)
    {
        _points = new DataPoint[n];
        types = new List<DataTypes>();
    }

    public void AddPoint(int i, DataPoint point)
    {
        _points[i] = point;
        types.Add(point.type);
    }

    public List<DataTypes> GetTypes()
    {
        DataTypes[] val = new DataTypes[types.Count];
        types.CopyTo(val);
        return val.ToList();
    }

    public bool GetValueBool(int i)
    {
        return _points[i].GetBool();
    }

    public float GetValueFloat(int i)
    {
        return _points[i].GetFloat();
    }

    public string GetValueString(int i)
    {
        return _points[i].ToString();
    }

    public override string ToString()
    {
        return string.Join(",", _points.ToList());
    }
}