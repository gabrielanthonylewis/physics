using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{
	// Singleton
	private static Collision _instance = null;
	public static Collision instance { get { return _instance;  } }

	public Material lineMaterial = null;


	public struct CollisionData
	{
		public bool wasCollision;
		public List<Vector3> contacts;
		public NewRigidBody bodyA;
		public NewRigidBody bodyB;
		public GameObject bodyAobj;
		public GameObject bodyBobj;
		public Vector3 normal;
		public float penetration;
	};
	CollisionData[] testMani = new CollisionData[2]; // for debugging

	struct MinMax
	{
		public float min;
		public float max;
		public Vector3 minPoint;
		public Vector3 maxPoint;
	};

	MinMax _debugMinMax = new MinMax();


	[System.Serializable]
	struct SeperationDataDebug
	{
		public Vector3 axis;
		public Vector3 faceA;
		public Vector3 faceb;
		public string faceBName;
	};

	[SerializeField]
	SeperationDataDebug[] _previousStateDebug;
	[SerializeField]
	List<SeperationDataDebug> _currentStateDebug = new List<SeperationDataDebug>();


    private void Awake()
    {
		// enforce singleton
        if(_instance == null)
            _instance = this;
        else
            Destroy(this.gameObject);
    }

	void OnPostRender()
	{
		DrawManifold ();
		DrawMinMax ();
	}

	void OnDrawGizmos()
	{
		DrawManifold ();
		DrawMinMax ();
        DrawSeperationDebug();

    }
		

	#region Collision Detection (with CollisionData)
	public CollisionData BoxToBoxManifold(NewBoxCollider boxA, NewBoxCollider boxB)
	{ 
		// reference: https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
		// NOTE that I had to convert from 2D to 3D and add my own stuff (the tutorial wasn't entirely accurate)

		// Manifold Initialisation
		CollisionData manifold = new CollisionData();
		manifold.bodyA = boxA.transform.gameObject.GetComponent<NewRigidBody> ();
		manifold.bodyB = boxB.transform.gameObject.GetComponent<NewRigidBody> ();
		manifold.bodyAobj = boxA.transform.gameObject;
		manifold.bodyBobj = boxB.transform.gameObject;
		manifold.normal = Vector3.zero;
		manifold.penetration = 0.0f;

		// Exit if there is not a collision
		manifold.wasCollision = BoxToBox(boxA, boxB);
		if (manifold.wasCollision == false)
			return manifold;

		Vector3 relativePosition = boxB.transform.position - boxA.transform.position;
		float xOverlap = boxA.extents.x + boxB.extents.x - Mathf.Abs (relativePosition.x);

		// If overlapping in the x-axis
		if (xOverlap > 0.0f) 
		{
			float yOverlap = boxA.extents.y + boxB.extents.y - Mathf.Abs (relativePosition.y);

			// If also overlapping on the y-axis
			if (yOverlap > 0.0f) 
			{
				float zOverlap = boxA.extents.z + boxB.extents.z - Mathf.Abs (relativePosition.z);

				// Find axis of least penetration
				if (xOverlap < yOverlap && xOverlap < zOverlap) 
				{
					if (relativePosition.x < 0.0f)
						manifold.normal = new Vector3 (-1.0f, 0.0f, 0.0f); 
					else
						manifold.normal = new Vector3 (1.0f, 0.0f, 0.0f);

					manifold.penetration = xOverlap;
				} 
				else if (yOverlap <= xOverlap && yOverlap < zOverlap) 
				{
					if (relativePosition.y < 0.0f)
						manifold.normal = new Vector3 (0.0f, -1.0f, 0.0f);
					else
						manifold.normal = new Vector3 (0.0f, 1.0f, 0.0f);

					manifold.penetration = yOverlap;
				} 
				else 
				{
					if (relativePosition.z < 0.0f)
						manifold.normal = new Vector3 (0.0f, 0.0f, -1.0f);
					else
						manifold.normal = new Vector3 (0.0f, 0.0f, 1.0f);

					manifold.penetration = zOverlap;
				}
			}
		}

		return manifold;
	}

	public CollisionData SphereToSphereManifold(NewSphereCollider sphereA, NewSphereCollider sphereB)
	{
		// reference: https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331

		// Manifold Initialisation
		CollisionData manifold = new CollisionData();
		manifold.bodyA = sphereA.transform.gameObject.GetComponent<NewRigidBody>();
		manifold.bodyB = sphereB.transform.gameObject.GetComponent<NewRigidBody>();
		manifold.bodyAobj = sphereA.transform.gameObject;
		manifold.bodyBobj = sphereB.transform.gameObject;
		manifold.normal = Vector3.zero;
		manifold.penetration = 0.0f;

		// Exit if no collision
		manifold.wasCollision = SphereToSphere(sphereA, sphereB);
		if (manifold.wasCollision == false)
			return manifold;

		Vector3 relativePosition = sphereB.transform.position - sphereA.transform.position;

		float distance = Mathf.Sqrt(
			(sphereA.transform.position.x - sphereB.transform.position.x) * (sphereA.transform.position.x - sphereB.transform.position.x) +
			(sphereA.transform.position.y - sphereB.transform.position.y) * (sphereA.transform.position.y - sphereB.transform.position.y) +
			(sphereA.transform.position.z - sphereB.transform.position.z) * (sphereA.transform.position.z - sphereB.transform.position.z));

        // Ensure there is a distance to avoid / 0 error 
        manifold.contacts = new List<Vector3>();
		if (distance != 0.0f) 
		{
			manifold.penetration = (sphereA.radius + sphereB.radius) - distance; 
			manifold.normal = relativePosition / distance;
			manifold.contacts.Add(sphereA.transform.position);
        }
		// Avoid / 0 error
		else 
		{
			manifold.penetration = sphereA.radius;
			manifold.normal = new Vector3 (0.0f, 1.0f, 0.0f);// Consistent value (randomly chosen)
			manifold.contacts.Add(manifold.normal * sphereA.radius + sphereA.transform.position);
        }

		return manifold;     
	}

	public CollisionData BoxToSphereManifold (NewBoxCollider box, NewSphereCollider sphere)
	{
		// reference: https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
		// (NOTE reference is in 2d, had to add 3D as well as my own manifold stuff)

		// Initialise manifold
		CollisionData manifold = new CollisionData();
		manifold.bodyA = box.transform.gameObject.GetComponent<NewRigidBody> ();
		manifold.bodyB = sphere.transform.gameObject.GetComponent<NewRigidBody> ();
		manifold.bodyAobj = box.transform.gameObject;
		manifold.bodyBobj = sphere.transform.gameObject;
		manifold.normal = Vector3.zero;
		manifold.penetration = 0.0f;
		manifold.wasCollision = false;


		Vector3 relativePosition = sphere.transform.position - box.transform.position; 

		// Closest point on A to center of B 
		Vector3 closestPoint = Vector3.zero;
		closestPoint.x = Mathf.Max(box.minBounds.x, Mathf.Min(sphere.transform.position.x, box.maxBounds.x));
		closestPoint.y = Mathf.Max(box.minBounds.y, Mathf.Min(sphere.transform.position.y, box.maxBounds.y));
		closestPoint.z = Mathf.Max(box.minBounds.z, Mathf.Min(sphere.transform.position.z, box.maxBounds.z));


		// If Circle is inside Box
		bool inside = false;
		if (relativePosition == closestPoint) 
		{
			inside = true;

			// Find closest axis
			if (Mathf.Abs (relativePosition.x) > Mathf.Abs (relativePosition.y) && Mathf.Abs (relativePosition.x) > Mathf.Abs (relativePosition.z)) 
			{
				// Clamp to closest extent
				if (closestPoint.x > 0.0f)
					closestPoint.x = box.extents.x;
				else
					closestPoint.x = -box.extents.x;
			}
			// y-axis is shorter
			else if (Mathf.Abs (relativePosition.y) > Mathf.Abs (relativePosition.z)) 
			{
				// Clamp to closest extent
				if (closestPoint.y > 0.0f)
					closestPoint.y = box.extents.y;
				else
					closestPoint.y = -box.extents.y;
			}
			// z-axis is shorter
			else
			{
				// Clamp to closest extent
				if (closestPoint.z > 0.0f)
					closestPoint.z = box.extents.z;
				else
					closestPoint.z = -box.extents.z;
			}
		}


		Vector3 normal = sphere.transform.position - closestPoint; 

		float distance = (closestPoint.x - sphere.transform.position.x) * (closestPoint.x - sphere.transform.position.x) +
			(closestPoint.y - sphere.transform.position.y) * (closestPoint.y - sphere.transform.position.y) +
			(closestPoint.z - sphere.transform.position.z) * (closestPoint.z - sphere.transform.position.z);


		// If no collision then return
		manifold.wasCollision = (Mathf.Sqrt (distance) < sphere.radius);
		if (manifold.wasCollision == false)
			return manifold;


		distance = Mathf.Sqrt (distance);


		// Flip collision normal if inside
		if (inside == true)
			manifold.normal = Vector3.Normalize(-normal);
		else
			manifold.normal = Vector3.Normalize(normal);


		manifold.penetration = sphere.radius - distance;

		// Contact point
		manifold.contacts = new List<Vector3> ();
		manifold.contacts.Add (closestPoint);
	
		return manifold;
	}
	#endregion

	#region SAT Collision Detection
	public CollisionData BoxToBoxSAT(NewBoxCollider boxA, NewBoxCollider boxB)
	{
		// reference to seperating axis theorem:
		// https://gamedevelopment.tutsplus.com/tutorials/collision-detection-using-the-separating-axis-theorem--gamedev-169
		// 3D axis found using: https://gamedev.stackexchange.com/questions/44500/how-many-and-which-axes-to-use-for-3d-obb-collision-with-sat
		CollisionData manifold = new CollisionData ();
		manifold.wasCollision = false;

		if (boxA == null || boxB == null)
			return manifold;

		manifold.bodyA = boxA.transform.gameObject.GetComponent<NewRigidBody>();
		manifold.bodyB = boxB.transform.gameObject.GetComponent<NewRigidBody>();
		manifold.bodyAobj = boxA.transform.gameObject;
		manifold.bodyBobj = boxB.transform.gameObject;
		manifold.normal = Vector3.zero;
		manifold.penetration = 0.0f;


		// dots
		List<Vector3> boxADots = new List<Vector3> ();
		List<Vector3> boxBDots = new List<Vector3> ();

		for (int i = 0; i < boxA.origin.dots.Length; i++) 
		{
			boxADots.Add (boxA.origin.dots [i]);
			boxBDots.Add (boxB.origin.dots [i]);
		}

		// normals
		List<Vector3> boxANormals = new List<Vector3> ();
		List<Vector3> boxBNormals = new List<Vector3> ();

		for (int i = 1; i < 3; i++) 
		{
			boxANormals.Add(boxA.origin.normals[i]);
			boxBNormals.Add (boxB.origin.normals [i]);
		}
		boxANormals.Add (boxA.faces [1].normals [0]);
		boxBNormals.Add (boxB.faces [1].normals [0]);



		_currentStateDebug.Clear ();

		// Optimisation
		if (Vector3.Distance (boxA.transform.position, boxB.transform.position) > 10.0f)
			return manifold;

		// Box 1 XYZ
		bool seperate_Q = isSeperate(boxADots, boxBDots, boxANormals[0], boxB.transform.name);
		bool seperate_P = isSeperate(boxADots, boxBDots, boxANormals[1], boxB.transform.name); 
		bool seperate_5 = isSeperate(boxADots, boxBDots, boxANormals[2], boxB.transform.name);

		// Box 2 XYZ
		bool seperate_S = isSeperate(boxADots, boxBDots, boxBNormals[0], boxB.transform.name); 
		bool seperate_R = isSeperate(boxADots, boxBDots, boxBNormals[1], boxB.transform.name); 
		bool seperate_6 = isSeperate(boxADots, boxBDots, boxBNormals[2], boxB.transform.name);

		// Cross Products
		bool seperate_7 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[0], boxBNormals[0]), boxB.transform.name); 
		bool seperate_8 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[0], boxBNormals[1]), boxB.transform.name);
		bool seperate_9 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[0], boxBNormals[2]), boxB.transform.name);

		bool seperate_10 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[1], boxBNormals[0]), boxB.transform.name);
		bool seperate_11 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[1], boxBNormals[1]), boxB.transform.name);
		bool seperate_12 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[1], boxBNormals[2]), boxB.transform.name);

		bool seperate_13 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[2], boxBNormals[0]), boxB.transform.name);
		bool seperate_14 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[2], boxBNormals[1]), boxB.transform.name);
		bool seperate_15 = isSeperate(boxADots, boxBDots, Vector3.Cross(boxANormals[2], boxBNormals[2]), boxB.transform.name);


		// If one is seperate then no collision
		manifold.wasCollision = !(seperate_10 || seperate_11 || seperate_12 || seperate_13 || seperate_14 || seperate_15 || seperate_5 || seperate_6 || seperate_7 || seperate_8 || seperate_9
			|| seperate_P || seperate_Q || seperate_R || seperate_S);


		if (manifold.wasCollision == true)
		{
			if (_previousStateDebug.Length > 0) 
			{
				// Find penertration, normal and contact point
				Vector3 checkPoint = _previousStateDebug [0].faceA - Vector3.Scale (-manifold.normal /2.0f, boxB.extents); 
				manifold.penetration = Vector3.Distance(checkPoint, _previousStateDebug[0].faceA); 

				manifold.normal = -_previousStateDebug [0].axis;

				manifold.contacts = new List<Vector3> ();
				for (int contactI = 0; contactI < _previousStateDebug.Length; contactI++)
					manifold.contacts.Add (_previousStateDebug [contactI].faceA);
			} 
			else 
			{
				Debug.LogWarning ("No contact between? " + boxA.transform.name + "," + boxB.transform.name);
			}
		}
		else
		{
			_previousStateDebug = new SeperationDataDebug[_currentStateDebug.Count];
			_currentStateDebug.CopyTo(_previousStateDebug);
			_currentStateDebug.Clear ();
		}


		return manifold;
	}

	private MinMax CalculateMinMax(List<Vector3> vertices, Vector3 axis)
	{
		MinMax minMax = new MinMax ();

		float minProjection = Vector3.Dot(vertices[1], axis);
		int minDotIndex = 1;

		float maxProjection = Vector3.Dot (vertices [1], axis);
		int maxDotIndex = 1;

		for (int i = 2; i < vertices.Count; i++) 
		{
			float curr_proj1 = Vector3.Dot (vertices [i], axis);

			if (minProjection > curr_proj1) 
			{
				minProjection = curr_proj1;
				minDotIndex = i;
			}
			if (curr_proj1 > maxProjection) 
			{
				maxProjection = curr_proj1;
				maxDotIndex = i;
			}
		}

		minMax.min = minProjection;
		minMax.max = maxProjection;

		minMax.minPoint = vertices[minDotIndex];
		minMax.maxPoint = vertices [maxDotIndex];

		_debugMinMax = minMax;

		return minMax;
	}

	private bool isSeperate(List<Vector3> vecs_box1, List<Vector3> vecs_box2, Vector3 axis, string faceBname)
	{
		MinMax result1 = CalculateMinMax(vecs_box1, axis);
		MinMax result2 = CalculateMinMax(vecs_box2, axis);
		bool seperate = (result1.max < result2.min) || (result2.max < result1.min);

		if(seperate)
		{
			// store for later
			SeperationDataDebug dataDebug = new SeperationDataDebug();
			dataDebug.axis = axis;
			dataDebug.faceBName = faceBname;
			if (result1.max < result2.min)
			{
				dataDebug.faceA = result1.maxPoint;
				dataDebug.faceb = result2.minPoint;
			}
			else
			{
				dataDebug.faceA = result1.minPoint;
				dataDebug.faceb = result2.maxPoint;
			}

			_currentStateDebug.Add(dataDebug);
		}

		return seperate;
	}
	#endregion

	#region Basic Collision Detection
	public bool BoxToBox(NewBoxCollider boxA, NewBoxCollider boxB)
	{ 
		// reference https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection
		// If all boxA axis are inside boxB then there is a collision
		return (boxA.minBounds.x < boxB.maxBounds.x && boxA.maxBounds.x > boxB.minBounds.x) &&
			(boxA.minBounds.y < boxB.maxBounds.y && boxA.maxBounds.y > boxB.minBounds.y) &&
			(boxA.minBounds.z < boxB.maxBounds.z && boxA.maxBounds.z > boxB.minBounds.z);
	}

	public bool SphereToSphere(NewSphereCollider sphereA, NewSphereCollider sphereB)
	{
		// reference https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection
		float distance = Mathf.Sqrt(
			(sphereA.transform.position.x - sphereB.transform.position.x) * (sphereA.transform.position.x - sphereB.transform.position.x) +
			(sphereA.transform.position.y - sphereB.transform.position.y) * (sphereA.transform.position.y - sphereB.transform.position.y) +
			(sphereA.transform.position.z - sphereB.transform.position.z) * (sphereA.transform.position.z - sphereB.transform.position.z));

		return (distance < (sphereA.radius + sphereB.radius));
	}
	public bool SphereToBox(NewSphereCollider sphere, NewBoxCollider box)
	{
		// reference https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection

		// find the closest point on the box to the center of the sphere
		Vector3 closestPoint = Vector3.zero;
		closestPoint.x = Mathf.Max(box.minBounds.x, Mathf.Min(sphere.transform.position.x, box.maxBounds.x));
		closestPoint.y = Mathf.Max(box.minBounds.y, Mathf.Min(sphere.transform.position.y, box.maxBounds.y));
		closestPoint.z = Mathf.Max(box.minBounds.z, Mathf.Min(sphere.transform.position.z, box.maxBounds.z));

		// distance between closest point and center of the sphere
		float distance = Mathf.Sqrt(
			(closestPoint.x - sphere.transform.position.x) * (closestPoint.x - sphere.transform.position.x) +
			(closestPoint.y - sphere.transform.position.y) * (closestPoint.y - sphere.transform.position.y) +
			(closestPoint.z - sphere.transform.position.z) * (closestPoint.z - sphere.transform.position.z));

		return (distance < sphere.radius);
	}
	#endregion

	#region Debug Info
    private void DrawSeperationDebug()
    {
        if (lineMaterial == null)
            return;

        for (int i = 0; i < _previousStateDebug.Length; i++)
        {
          //  for (int j = 0; j < _previousStateDebug[i].faceA.Count; j++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_previousStateDebug[i].faceA, 0.6f);
                // Gizmos.DrawSphere(_previousStateDebug[i].faceA[j], 0.6f);
            }

            //for (int j = 0; j < _previousStateDebug[i].faceb.Count; j++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_previousStateDebug[i].faceb, 0.6f);
                //  Gizmos.DrawSphere(_previousStateDebug[i].faceb[j], 0.6f);
            }

        }

    }

    void DrawMinMax()
	{
		if (lineMaterial == null)
			return;

		Vector3 startPos = _debugMinMax.minPoint;
		Vector3 endPos = startPos * 1.5f; // 1.5f  for debug


		GL.Begin (GL.LINES);
		lineMaterial.SetPass (0);
		GL.Color (new Color (lineMaterial.color.r / 2.0f, lineMaterial.color.g  / 2.0f, lineMaterial.color.b, lineMaterial.color.a));
		GL.Vertex (startPos);
		GL.Vertex (endPos);
		GL.End ();
	}

	void DrawManifold()
	{
		if (lineMaterial == null)
			return;
		
		foreach (CollisionData manifold in testMani) 
		{
			if (manifold.wasCollision == false)
				continue;

			Vector3 startPos = manifold.bodyAobj.transform.position;
			Vector3 endPos = startPos + manifold.normal * manifold.penetration * 10.0f; // 10.0f  for debug

			GL.Begin (GL.LINES);
			lineMaterial.SetPass (0);
			GL.Color (new Color (lineMaterial.color.r, lineMaterial.color.g, lineMaterial.color.b, lineMaterial.color.a));
			GL.Vertex (startPos);
			GL.Vertex (endPos);
			GL.End ();

			startPos = manifold.bodyBobj.transform.position;
			endPos = startPos + manifold.normal * manifold.penetration * 10.0f; // 10.0f  for debug


			GL.Begin (GL.LINES);
			lineMaterial.SetPass (0);
			GL.Color (new Color (lineMaterial.color.r, lineMaterial.color.g, lineMaterial.color.b, lineMaterial.color.a));
			GL.Vertex (startPos);
			GL.Vertex (endPos);
			GL.End ();
		}

	}
	#endregion

	#region Debug.Log test for Collision Detection
	private void TestBoxBox()
	{
		NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider> ();

		foreach (NewBoxCollider box1 in boxes) 
			foreach (NewBoxCollider box2 in boxes) 
			{
				if (box1 == box2)
					continue;

				Debug.Log(BoxToBox (box1, box2));
			}
	}
	private void TestBoxBoxManifold()
	{
		NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider> ();

		foreach (NewBoxCollider box1 in boxes) 
			foreach (NewBoxCollider box2 in boxes) 
			{
				if (box1 == box2)
					continue;

				testMani[0] = BoxToBoxManifold (box1, box2);

				if(testMani[0].wasCollision)
					Debug.Log(testMani[0].bodyAobj.name + " vs. " + testMani[0].bodyBobj.name + " : " + testMani[0].normal + " , " +	testMani[0].penetration);
			}
	}

	private void TestSphereSphere()
	{
		NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider> ();

		foreach (NewSphereCollider sphereA in spheres) 
			foreach (NewSphereCollider sphereB in spheres) 
			{
				if (sphereA == sphereB)
					continue;

				Debug.Log(SphereToSphere (sphereA, sphereB));
			}
	}
	private void TestSphereSphereManifold()
	{
		NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider> ();

		foreach (NewSphereCollider sphereA in spheres) 
			foreach (NewSphereCollider sphereB in spheres) 
			{
				if (sphereA == sphereB)
					continue;

				testMani[0] = SphereToSphereManifold (sphereA, sphereB);

				if(testMani[0].wasCollision)
					Debug.Log(testMani[0].bodyAobj.name + " vs. " + testMani[0].bodyBobj.name + " : " + testMani[0].normal + " , " +	testMani[0].penetration);
			}
	}

	private void TestSphereBox()
	{
		NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider> ();
		NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider> ();

		foreach (NewSphereCollider sphere in spheres) 
			foreach (NewBoxCollider box in boxes) 
				Debug.Log(SphereToBox (sphere, box));
	}
	private void TestSphereBoxManifold()
	{
		NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider> ();
		NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider> ();

		foreach (NewSphereCollider sphere in spheres) 
			foreach (NewBoxCollider box in boxes)  
			{
				testMani[0] = BoxToSphereManifold (box, sphere);

				//if(testMani[0].wasCollision)
				//	Debug.Log(testMani[0].bodyAobj.name + " vs. " + testMani[0].bodyBobj.name + " : " + testMani[0].normal + " , " +	testMani[0].penetration);
			}
	}
	#endregion
}
