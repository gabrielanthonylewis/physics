using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NewSphereCollider : MonoBehaviour 
{
    private float _radius = 1;
    public float radius { get { return _radius; } }

	void Start()
	{
        _radius =  this.transform.localScale.x / 2.0f;
	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position,  this.transform.localScale.x / 2.0f);
    }

}