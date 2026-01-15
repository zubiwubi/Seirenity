using System;
using UnityEngine;

public class PlayerIncrementalChangeColouring : MonoBehaviour
{
    [SerializeField]
    public int playerNumber;

    private Renderer _renderer;

    private void Start()
    {
        _renderer = gameObject.GetComponent<Renderer>();
    }

    public void UpdateColour(Color colour)
    {
        _renderer.material.SetColor("_BaseColor", colour);
    }
}
