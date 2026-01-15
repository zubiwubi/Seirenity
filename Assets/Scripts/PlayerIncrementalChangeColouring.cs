using System;
using UnityEngine;

public class PlayerIncrementalChangeColouring : MonoBehaviour
{
    [SerializeField]
    public int playerNumber;

    private Renderer renderer;

    private void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
    }

    public void UpdateColour(Color colour)
    {
        renderer.material.SetColor("_BaseColor", colour);
    }
}
