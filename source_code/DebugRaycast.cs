using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DebugRaycast : MonoBehaviour 
{
	[SerializeField]
	private Text _nameTxt = null, _velocityTxt = null, _accelerationTxt = null,
		_positionTxt = null, _orientationTxt = null, _angularVelocityTxt = null,
		_inertiaTxt = null, _isKinematicTxt = null, _massTxt = null, _restitutionTxt = null;

	[SerializeField]
	private InputField _massInput = null, _positionX = null, _positionY = null, _positionZ = null,
						_orienX = null, _orienY = null, _orienZ = null;

    [SerializeField]
    private Toggle _isKinematic;

	[SerializeField]
	public NewRigidBody _selectedObject = null;

	[SerializeField]
	private Material _lineMaterial = null;

    private bool _start = false;
 

    void Update ()
	{
		// Initial refresh
		if(_start == false)
        {
			_massInput.text = _selectedObject.mass.ToString();

			_positionX.text = _selectedObject.position.x.ToString();
			_positionY.text = _selectedObject.position.y.ToString();
			_positionZ.text = _selectedObject.position.z.ToString();

			_orienX.text = _selectedObject.orientation.x.ToString();
			_orienY.text = _selectedObject.orientation.y.ToString();
			_orienZ.text = _selectedObject.orientation.z.ToString();

			 _isKinematic.isOn = _selectedObject._isKinematic;
			_start = true;
        }

		// Prevent ray from going through UI
		if (EventSystem.current.IsPointerOverGameObject ())
			return;

		// Select object
		if (Input.GetKeyDown (KeyCode.Mouse0)) 
		{
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			if (Physics.Raycast (ray, out hit, 100.0f)) 
			{
				_selectedObject = hit.transform.gameObject.GetComponent<NewRigidBody> ();

				_massInput.text = _selectedObject.mass.ToString();

				_positionX.text = _selectedObject.position.x.ToString();
				_positionY.text = _selectedObject.position.y.ToString();
				_positionZ.text = _selectedObject.position.z.ToString();

				_orienX.text = _selectedObject.orientation.x.ToString();
				_orienY.text = _selectedObject.orientation.y.ToString();
				_orienZ.text = _selectedObject.orientation.z.ToString();

				_isKinematic.isOn = _selectedObject._isKinematic;

				RefreshUI();
			}
		}

		// Apply torque
		if (Input.GetKey (KeyCode.Mouse1)) 
		{
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			if (Physics.Raycast (ray, out hit, 100.0f)) 
			{
				hit.transform.gameObject.GetComponent<NewRigidBody> ().ApplyTorqueForce(hit.point, hit.normal);
			}
		}


		if (_selectedObject == null)
			return;

		// Refresh the selected object's UI
		RefreshUI();
	}

	private void RefreshUI()
	{
		_nameTxt.text = _selectedObject.name;
		_restitutionTxt.text = "Restitution: " + _selectedObject._restitution;
		_velocityTxt.text = "Velocity: " + _selectedObject.velocity + "m/s";
		_accelerationTxt.text = "Acceleration: " + _selectedObject.acceleration + "m/s^2";
		_angularVelocityTxt.text = "Angular Velocity: " + _selectedObject.angularVelocity;
		_inertiaTxt.text = "Inertia: " + _selectedObject.inertia;

		_massTxt.text = "Mass: " + _selectedObject.mass + "kg";
		//_massInput.text = _selectedObject.mass.ToString();

        _positionTxt.text = "Position: " + _selectedObject.position;
      //  _positionX.text = _selectedObject.position.x.ToString();
       // _positionY.text = _selectedObject.position.y.ToString();
     //   _positionZ.text = _selectedObject.position.z.ToString();

        _orientationTxt.text = "Orientation: " + _selectedObject.orientation;
        //_orienX.text = _selectedObject.orientation.x.ToString();
        //_orienY.text = _selectedObject.orientation.y.ToString();
        //_orienZ.text = _selectedObject.orientation.z.ToString();

        _isKinematicTxt.text = "IsKinematic: " + _selectedObject._isKinematic;
       // _isKinematic.isOn = _selectedObject._isKinematic;
    }
		
	public void RefreshInitialConditionsUI()
	{
		// Mass
		_selectedObject.mass = float.Parse(_massInput.text);
        if (_selectedObject.mass == 0)
            _selectedObject._inverseMass = 0.0f;
        else
            _selectedObject._inverseMass = 1.0f / _selectedObject.mass;

        _massTxt.text = "Mass: " + _selectedObject.mass + "kg";

		// Position
        Vector3 newPos = Vector3.zero;
        if (float.TryParse(_positionX.text, out newPos.x) && float.TryParse(_positionY.text, out newPos.y) && float.TryParse(_positionZ.text, out newPos.z))
        {

            if (_selectedObject.transform.position != newPos)
            {
                _selectedObject.position = newPos;
                _selectedObject.transform.position = _selectedObject.position;

                _positionTxt.text = "Position: " + _selectedObject.position;
            }
        }

		// Orientation
        Vector3 newOrien = Vector3.zero;
        if (float.TryParse(_orienX.text, out newOrien.x) && float.TryParse(_orienY.text, out newOrien.y) && float.TryParse(_orienZ.text, out newOrien.z))
        {

            if (_selectedObject.transform.rotation.eulerAngles != newOrien)
            {
                _selectedObject.orientation = newOrien;
                _selectedObject.transform.rotation = Quaternion.Euler(_selectedObject.orientation);

                _orientationTxt.text = "Orientation: " + _selectedObject.orientation;
            }
        }

		// IsKinematic
        _selectedObject._isKinematic = _isKinematic.isOn;
        _isKinematicTxt.text = "IsKinematic: " + _selectedObject._isKinematic;
    }

	void DrawSelectedObject()
	{
		if (_lineMaterial == null)
			return;

		NewBoxCollider box = _selectedObject.GetComponent<NewBoxCollider> ();
		if (box != null) 
		{
			DrawCube (box.origin.dots, 1f);
			DrawCube (box.origin.dots, 1.0005f);
			DrawCube (box.origin.dots, 1.0006f);
			DrawCube (box.origin.dots, 1.0007f);
			DrawCube (box.origin.dots, 1.0008f);

			return;
		}

		NewSphereCollider sphere = _selectedObject.GetComponent<NewSphereCollider> ();
		if (sphere != null) 
		{
			DrawSphere (sphere.radius, sphere.transform.position);

			return;
		}
	}

	void DrawCube(Vector3[] points, float scale)
	{
		// TOP
		for(int i = 1; i < 4; i++)
			DrawLine (points [i] * scale, points [i + 1] * scale);
		DrawLine (points [4] * scale, points [1] * scale);

		// BOTTOM
		for(int i = 5; i < 8; i++)
			DrawLine (points [i] * scale, points [i + 1] * scale);
		DrawLine (points [8] * scale, points [5] * scale);

		// VERTICAL
		for(int i = 1, j = 5; i < 5; i++, j++)
			DrawLine (points [i] * scale, points [j] * scale);
	}

	void OnPostRender()
	{
		DrawSelectedObject ();
	}

	void OnDrawGizmos()
	{
		DrawSelectedObject ();
	}

	public void DrawLine(Vector3 start, Vector3 end)
	{
		GL.Begin (GL.LINES);
		_lineMaterial.SetPass (0);
		GL.Color (new Color (_lineMaterial.color.r, _lineMaterial.color.g, _lineMaterial.color.b, _lineMaterial.color.a));
		GL.Vertex (start);
		GL.Vertex (end);
		GL.End ();
	}

	public void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		GL.Begin(GL.QUADS);
		GL.Color (new Color (_lineMaterial.color.r, _lineMaterial.color.g, _lineMaterial.color.b, _lineMaterial.color.a));
		GL.Vertex3(a.x, a.y, a.z);
		GL.Vertex3(b.x, b.y, b.z);
		GL.Vertex3(c.x, c.y, c.z);
		GL.Vertex3(d.x, d.y, d.z);
		GL.End();
	}


	void DrawSphere(float radius, Vector3 position)
	{
		DrawCircles(radius, position, 1.0f);
	}
		
	void DrawCircles(float radius, Vector3 position, float scale)
	{
		GL.Begin(GL.LINES);
		_lineMaterial.SetPass(0);
		GL.Color (new Color (_lineMaterial.color.r, _lineMaterial.color.g, _lineMaterial.color.b, _lineMaterial.color.a));

		float step = 0.15f;

		// 2D circle
		for (float i = 0.0f; i < (2.0f * Mathf.PI); i += step)
		{
			Vector3 pos = (new Vector3 (Mathf.Cos (i) * radius + position.x * scale, Mathf.Sin (i) * radius + position.y * scale, position.z * scale));
			GL.Vertex3 (pos.x, pos.y, pos.z);
		}

		// Equator
		for (float i = 0.0f; i < (2.0f * Mathf.PI); i += step)
		{
			Vector3 pos = (new Vector3 (Mathf.Cos (i) * radius + position.x * scale, position.y * scale, Mathf.Sin (i) * radius + position.z * scale));
			GL.Vertex3 (pos.x, pos.y, pos.z);
		}

		// Rotated 2D circle
		for (float i = 0.0f; i < (2.0f * Mathf.PI); i += step)
		{
			Vector3 pos = (new Vector3 (position.x * scale, Mathf.Cos (i) * radius + position.y * scale, Mathf.Sin (i) * radius + position.z * scale));
			GL.Vertex3 (pos.x, pos.y, pos.z);
		}

		GL.End();
	}
}
