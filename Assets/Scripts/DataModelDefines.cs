using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataPoint
{
    private string _rawValue;
    public string RawValue { get { return _rawValue; } }

    private readonly float defaultFloat = 0.0f;

    public DataPoint(string value)
    {
        _rawValue = value;
        _rawValue = _rawValue.TrimStart(new char[] { '"' });
        _rawValue = _rawValue.TrimEnd(new char[] { '"' });
    }

    public float GetFloat()
    {
        bool result = float.TryParse(_rawValue, out float i);
        if (result) return i;
        else return defaultFloat;
    }

    public override string ToString()
    {
        return _rawValue;
    }
}

public class DataRecord
{
    private DataPoint[] _points;

    public DataRecord(int n)
    {
        _points = new DataPoint[n];
    }

    public void AddPoint(int i, DataPoint point)
    {
        _points[i] = point;
    }

    public float GetValueFloat(int i)
    {
        return _points[i].GetFloat();
    }

    public override string ToString()
    {
        return string.Join(",", _points.ToList());
    }
}