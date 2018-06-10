using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Spring : MonoBehaviour 
{
	[SerializeField]
	private Text _angleTxt = null;

    [SerializeField]
    private float _springConstant = 300.0f;

    [SerializeField]
    private float _currentLength = 0.5f;

    [SerializeField]
    private float _restLength = 1.0f;

	[SerializeField]
	private NewRigidBody test = null;

    private Vector3 _initalScale = Vector3.zero;
    private Vector3 _initalPosition = Vector3.zero;
	private Vector3 _initalRotation = Vector3.zero;
	private Vector3 _springForward = Vector3.zero;

	private float _angle = 0.0f;
	private float _power = 2.0f;


    private void Start()
    {
        _initalScale = this.transform.localScale;
        _initalPosition = this.transform.position;
		_initalRotation = this.transform.rotation.eulerAngles;

		// Find forward
		if (_initalScale.x > _initalScale.y && _initalScale.x > _initalScale.z)
			_springForward = new Vector3 (1.0f, 0.0f, 0.0f);
		else if (_initalScale.y > _initalScale.x && _initalScale.y > _initalScale.z)
			_springForward = new Vector3 (0.0f, 1.0f, 0.0f);
		else
			_springForward = new Vector3 (0.0f, 0.0f, 1.0f);
    }


    public void Compress()
    {		
        _currentLength -= 10.0f * Time.deltaTime; // 10.0f is speed of compression
		float extension = (_currentLength - _restLength);
  
		// Change scale and position
		this.transform.localScale = _initalScale + extension * _springForward;
		this.transform.position = _initalPosition + extension * _springForward;
    }

	public void AngleChanged()
	{
		float tempAngle = 0.0f;
		if (float.TryParse (_angleTxt.text, out tempAngle)) 
		{
			_angle = float.Parse (_angleTxt.text);
			this.transform.rotation = Quaternion.Euler (_initalRotation + new Vector3 (0.0f, 0.0f, _angle));
		}
	}
   
    public void Release()
    {
		// Calculate force
		float extension = (_currentLength - _restLength);
		float springForce = -_springConstant * extension;

		// Update object's velocity based on angle and force
		Vector3 newVelocity = Vector3.zero;
		newVelocity += _springForward * Mathf.Cos (Mathf.Deg2Rad * _angle);
		newVelocity += this.transform.up * Mathf.Sin (Mathf.Deg2Rad * _angle);
		test.velocity += newVelocity * springForce * _power * Time.deltaTime;
	
		this.transform.localScale = _initalScale;
		this.transform.position = _initalPosition;
    }
		
}
