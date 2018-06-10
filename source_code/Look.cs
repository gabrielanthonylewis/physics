using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Look : MonoBehaviour 
{
	[SerializeField]
	private Button _freeLookBtn = null;
	private bool _freeLook = true;

	private DebugRaycast _DebugRaycast =null;


	void Start()
	{
		_DebugRaycast = this.GetComponent<DebugRaycast> ();
	}


	void Update () 
	{
		if (_freeLook) 
		{
			// Keyboard movement
			float horiz = Input.GetAxis ("Horizontal") * 20.0f * Time.deltaTime;
			float vert = Input.GetAxis ("Vertical") * 20.0f * Time.deltaTime;

			this.transform.position += this.transform.forward * vert;
            this.transform.position += this.transform.right * horiz;


            // Mouse look
			if (!Input.GetKey (KeyCode.LeftShift)) 
			{
				float mouseX = Input.GetAxis ("Mouse X") * 250.0f * Time.deltaTime;
				float mouseY = Input.GetAxis ("Mouse Y") * 250.0f * Time.deltaTime;
				this.transform.Rotate (Vector3.up, mouseX);
				this.transform.Rotate (-Vector3.right, mouseY);

				Vector3 euler = this.transform.rotation.eulerAngles;
				euler.z = 0.0f;
				this.transform.rotation = Quaternion.Euler (euler);
			}
		} 
		else
		{
			if (!Input.GetKey (KeyCode.LeftShift)) 
			{
				// orbital movement
				Vector3 newPos =  _DebugRaycast._selectedObject.transform.position - Camera.main.transform.forward * (_DebugRaycast._selectedObject.transform.localScale.x + 1.0f);
				this.transform.position = newPos;

				// orbital look
				float mouseX = Input.GetAxis ("Mouse X") * 250.0f * Time.deltaTime;


				transform.RotateAround (_DebugRaycast._selectedObject.transform.position, Vector3.up, mouseX);
			}
		}

	}

	public void Focus()
	{
		_freeLook = false;

        this.transform.rotation = Quaternion.identity;
		_freeLookBtn.interactable = true;

    }

    public void FreeLook()
    {
		_freeLook = true;
		_freeLookBtn.interactable = false;
    }
}
