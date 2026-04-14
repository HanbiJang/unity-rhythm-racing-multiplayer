using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteData
{
    private int _Index;
    private float _Time;
    private char _Location;
    private string _Type;
    private int _Rand;
    private int _Score;
    private string _Sound;
    private int _Test;

    public int Index { get { return _Index; } set { _Index = value; } }
    public float Time { get { return _Time; } set { _Time = value; } }
    public char Location { get { return _Location; } set { _Location = value; } }
    public string Type { get { return _Type; } set { _Type = value; } }
    public int Rand { get { return _Rand; } set { _Rand = value; } }
    public int Score { get { return _Score; } set { _Score = value; } }
    public string Sound { get { return _Sound; } set { _Sound = value; } }

    //생성자
    public NoteData()
    {
        _Index = 0;
    }
    public NoteData(int Index_, float Time_, char Location_, string Type_, int Rand_, int Score_, string Sound_) {
        _Index = Index_;
        _Time = Time_;
        _Location = Location_;
        _Type = Type_;
        _Rand = Rand_;
        _Score = Score_;
        _Sound = Sound_;

    }


   

}
