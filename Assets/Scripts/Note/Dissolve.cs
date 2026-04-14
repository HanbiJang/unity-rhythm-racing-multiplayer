using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Dissolve : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer m_Sprite;

    public float m_Time;
    // Start is called before the first frame update
    void Start()
    {
        m_Sprite = GetComponent<SpriteRenderer>();
        m_Time = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        m_Sprite.color = new Color(0,0,0, Mathf.Abs(Mathf.Sin(m_Time)));
    }
}
