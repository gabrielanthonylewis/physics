using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBoxCollider : MonoBehaviour 
{
	[SerializeField]
	private Vector3 _maxBounds;
	public Vector3 maxBounds { get { return _maxBounds; } }

	[SerializeField]
	private Vector3 _minBounds;
	public Vector3 minBounds { get { return _minBounds; } }

	[SerializeField]
	private Vector3 _extents;
	public Vector3 extents { get { return _extents; } }

	private MeshRenderer _MeshRenderer = null;

	public struct Face 
	{
		public Vector3[] dots; // center = 0, corners
		public Vector3[] normals;
	};
	public Face[] faces; // 6 sides on a cube
	public Face origin;


	void Awake()
	{
		_extents = this.transform.localScale / 2.0f;
	}

	void Start()
	{
		_MeshRenderer = this.GetComponent<MeshRenderer> ();
		RefreshBounds ();

		// Initialisation
		faces = new Face[6];
		origin = new Face ();

		origin.dots = new Vector3[9];
		origin.normals =new Vector3[9];

		for (int i = 0; i < faces.Length; i++) 
		{
			faces [i].dots = new Vector3[5];
			faces [i].normals = new Vector3[5];

			for (int j = 0; j < faces [i].dots.Length; j++)
				faces [i].dots [j] = new Vector3 (-1000.0f, 1000.0f, -1000.0f);

			for(int j = 0; j < faces[i].normals.Length; j++)
				faces [i].normals [j] = new Vector3 (-1000.0f, 1000.0f, -1000.0f);
		
		}
    }
		
    private void Update()
    {
		RefreshBounds ();
		RefreshFaces ();
		RefreshNormals ();
    }



	private void RefreshBounds ()
	{
		_minBounds = _MeshRenderer.bounds.min;
		_maxBounds = _MeshRenderer.bounds.max;
	}

	public List<Vector3> getNormals()
	{
		List<Vector3> normals = new List<Vector3> ();
		for (int i = 1; i < faces[1].dots.Length - 1; i++) 
		{
			Vector3 currentNormal = (faces [1].dots [i + 1] - faces [1].dots [i]).normalized;
			normals.Add (currentNormal);
		}

		normals.Add((faces[1].dots[1] - faces[1].dots[faces[1].dots.Length - 1]).normalized);

		return normals;
	}
		

	private void RefreshNormals()
	{
		// Faces
		for (int i = 0; i < faces.Length; i++) 
		{
			int count = 0;
			for (int j = 1; j < faces [i].normals.Length - 1; j++) 
			{
				faces [i].normals [j] = Vector3.Normalize(faces [i].dots [j + 1] - faces [i].dots [j]);
				count++;
			}
		
			faces[i].normals[0] = CalculateFaceNormal(faces[i].dots[1], faces[i].dots[2], faces[i].dots[3]);
			count++;

			faces [i].normals [count] = Vector3.Normalize(faces [i].dots [1] - faces [i].dots [faces [i].normals.Length - 1]);
			count++;
		}

		// Origin
		{
			int count = 0;
			for (int j = 1; j < origin.normals.Length - 1; j++) 
			{
				origin.normals [j] = Vector3.Normalize(origin.dots [j + 1] -origin.dots [j]);
				count++;
			}
				
			origin.normals[0] = CalculateFaceNormal(origin.dots[1], origin.dots[2], origin.dots[3]);
			count++;

			origin.normals [count] = Vector3.Normalize(origin.dots [1] - origin.dots [origin.normals.Length - 1]);
			count++;

		}

	}

	private Vector3 CalculateFaceNormal(Vector3 point1, Vector3 point2, Vector3 point3)
	{
		// reference: how to get a face normal 
		//https://www.opengl.org/discussion_boards/showthread.php/159259-How-to-Calculate-Polygon-Normal

		Vector3 a = point1 - point2;
		Vector3 b = point2 - point3;

		return Vector3.Normalize (Vector3.Cross (a, b));
	}
	 


	private void RefreshFaces()
	{
		origin.dots [0] = this.transform.position;
		origin.dots [1] = this.transform.position + (this.transform.forward * _extents.z)  + (-this.transform.right * _extents.x)  + (this.transform.up * _extents.y);
		origin.dots [2] = this.transform.position + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (this.transform.up * _extents.y);
		origin.dots [3] = this.transform.position + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (this.transform.up * _extents.y);
		origin.dots [4] = this.transform.position + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (this.transform.up * _extents.y);

		origin.dots [5] = this.transform.position + (this.transform.forward * _extents.z)  + (-this.transform.right * _extents.x)  + (-this.transform.up * _extents.y);
		origin.dots [6] = this.transform.position + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (-this.transform.up * _extents.y);
		origin.dots [7] = this.transform.position + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (-this.transform.up * _extents.y);
		origin.dots [8] = this.transform.position + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (-this.transform.up * _extents.y);

		
		// Front face
		faces[0].dots[0] = this.transform.position + (this.transform.forward * _extents.z); // middle
		faces[0].dots[1] = this.transform.position + (this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (this.transform.up * _extents.y);
		faces[0].dots[2] = this.transform.position + (this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (-this.transform.up * _extents.y);
		faces[0].dots[3] = this.transform.position + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (-this.transform.up * _extents.y); 
		faces[0].dots[4] = this.transform.position + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (this.transform.up * _extents.y);

		// Back face

		faces[1].dots[0] = this.transform.position + (-this.transform.forward * _extents.z); // middle
		faces[1].dots[1] = this.transform.position + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (this.transform.up * _extents.y); 
		faces[1].dots[2] = this.transform.position + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x) + (-this.transform.up * _extents.y); 
		faces[1].dots[3] = this.transform.position + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (-this.transform.up * _extents.y); 
		faces[1].dots[4] = this.transform.position + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x) + (this.transform.up * _extents.y); 

		// Left face
		faces[2].dots[0] = this.transform.position + (-this.transform.right * _extents.x); // middle 
		faces[2].dots[1] = this.transform.position + (-this.transform.right * _extents.x) + (-this.transform.forward * _extents.z) + (this.transform.up * _extents.y);
		faces[2].dots[2] = this.transform.position + (-this.transform.right * _extents.x) + (-this.transform.forward * _extents.z) + (-this.transform.up * _extents.y); 
		faces[2].dots[3] = this.transform.position + (-this.transform.right * _extents.x) + (this.transform.forward * _extents.z) + (-this.transform.up * _extents.y);
		faces[2].dots[4] = this.transform.position + (-this.transform.right * _extents.x) + (this.transform.forward * _extents.z) + (this.transform.up * _extents.y);


		// Right face
		faces[3].dots[0] = this.transform.position + (this.transform.right * _extents.x);  // middle
		faces[3].dots[1] = this.transform.position + (this.transform.right * _extents.x) + (this.transform.forward * _extents.z) + (this.transform.up * _extents.y); 
		faces[3].dots[2] = this.transform.position + (this.transform.right * _extents.x) + (this.transform.forward * _extents.z) + (-this.transform.up * _extents.y); 
		faces[3].dots[3] = this.transform.position + (this.transform.right * _extents.x) + (-this.transform.forward * _extents.z) + (-this.transform.up * _extents.y); 
		faces[3].dots[4] = this.transform.position + (this.transform.right * _extents.x) + (-this.transform.forward * _extents.z) + (this.transform.up * _extents.y); 

		// Top face
		faces[4].dots[0] = this.transform.position + (this.transform.up * _extents.y); // middle
		faces[4].dots[1] = this.transform.position + (this.transform.up * _extents.y) + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x); 
		faces[4].dots[2] = this.transform.position + (this.transform.up * _extents.y) + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x); 
		faces[4].dots[3] = this.transform.position + (this.transform.up * _extents.y) + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x); 
		faces[4].dots[4] = this.transform.position + (this.transform.up * _extents.y) + (this.transform.forward * _extents.z) + (-this.transform.right * _extents.x); 

		// Back face
		faces[5].dots[0] = this.transform.position + (-this.transform.up * _extents.y); // middle
		faces[5].dots[1] = this.transform.position + (-this.transform.up * _extents.y) + (-this.transform.forward * _extents.z) + (this.transform.right * _extents.x); 
		faces[5].dots[2] = this.transform.position + (-this.transform.up * _extents.y) + (this.transform.forward * _extents.z) + (this.transform.right * _extents.x);
		faces[5].dots[3] = this.transform.position + (-this.transform.up * _extents.y) + (this.transform.forward * _extents.z) + (-this.transform.right * _extents.x); 
		faces[5].dots[4] = this.transform.position + (-this.transform.up * _extents.y) + (-this.transform.forward * _extents.z) + (-this.transform.right * _extents.x);
	
	}

    private void OnDrawGizmos()
    {
		Gizmos.color = Color.green;

		if (_MeshRenderer == null)
			_MeshRenderer = this.GetComponent<MeshRenderer> ();
	
		Gizmos.DrawWireCube(transform.position, _MeshRenderer.bounds.size);

		DrawFaces ();
		//DrawOrigin ();
    }

	private void DrawFaces()
	{
		if (faces == null)
			return;
		
		foreach (Face face in faces) 
		{
			if (face.dots == null)
				continue;
			
			foreach (Vector3 dot in face.dots) 
				Gizmos.DrawSphere (dot, 0.1f);

			for (int i = 0; i < face.normals.Length; i++) 
				Gizmos.DrawLine(face.dots[0], face.dots[0] + (face.normals[0] / 2.0f));
		}
	}

	private void DrawOrigin()
	{
		for (int i = 0; i < origin.dots.Length; i++) 
		{
			Gizmos.DrawSphere (origin.dots[i], 0.1f);
		}
		for (int i = 0; i < origin.normals.Length; i++) 
		{
			Gizmos.DrawLine(origin.dots[0], origin.dots[0] + (origin.normals[i] / 2.0f));
		}
	}
}
