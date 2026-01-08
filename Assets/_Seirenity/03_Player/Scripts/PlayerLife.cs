using System;
using UnityEngine;

public class PlayerLife : MonoBehaviour
{
    public Action onDeath;
    
    [SerializeField]
    private float lifeTime = 10f;
    private float _currentLife;

    public void ResetLife()
    {
        _currentLife = lifeTime;
    }

    private void Start()
    {
        
    }
    
    private void Update()
    {
        _currentLife -= Time.deltaTime;

        if (_currentLife <= 0)
        {
            onDeath?.Invoke();
        }
    }
}
