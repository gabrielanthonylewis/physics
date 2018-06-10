using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    [System.Serializable]
    struct RigidbodyState
    {
        public Vector3 velocity;
        public Vector3 position;
        public Vector3 acceleration;
		public Vector3 angularVelocity;
		public Vector3 orientation;
    };
	private RigidbodyState[] _initialStates;

	[System.Serializable]
	struct Storage
	{
		public RigidbodyState[] states;
	};
	[SerializeField]
	private Storage[] storage;
	const int STORAGE_SIZE = 200;


	[SerializeField]
	bool quit = false;

	[SerializeField]
	int undoCount = 0;
	[SerializeField]
	int stepIndex = 0;

	private NewRigidBody[] rigidBodies;

    [SerializeField]
	bool pause = true;
	bool nextFrame = false;
	bool prevFrame = false;

	[SerializeField]
	private GameObject _initialConditions;

	private bool firstPlay = false;


    void Start()
	{
		rigidBodies = GameObject.FindObjectsOfType<NewRigidBody> ();

        // Initialise
		undoCount = 0;
		stepIndex = 0;

		_initialStates = new RigidbodyState[rigidBodies.Length];
        RigidbodyState[] objectStates = new RigidbodyState[rigidBodies.Length];

		// Initial state of all objects
        int i = 0;
        foreach (NewRigidBody rigidBody in rigidBodies)
        {
            RigidbodyState state = new RigidbodyState();
            state.acceleration = rigidBody.acceleration;
            state.velocity = rigidBody.velocity;
            state.position = rigidBody.transform.position;
			state.orientation = rigidBody.transform.rotation.eulerAngles;
			state.angularVelocity = rigidBody.angularVelocity;
            objectStates[i] = state;
			_initialStates [i] = state;
            i++;            
        }

		// Storage initialisation
		storage = new Storage[STORAGE_SIZE];
		storage[stepIndex].states = objectStates;

		undoCount++;
		stepIndex++;

		// Start 
		StartCoroutine(Intergrate ());
    }
		
	// Reference on how to create a custom update loop for the physics:
	// https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-core-engine--gamedev-7493
	IEnumerator Intergrate()
	{
		const float fps = 60.0f;
		const float deltaTime = 1.0f / fps;

		float accumulator = 0.0f;
		float frameStart = Time.time;

		while (!quit) 
		{
			// For step through feature via mouse
			ReadInput ();

			float currentTime = Time.time;
			accumulator += currentTime - frameStart;
			frameStart = currentTime;

			if (accumulator > 0.2f)
				accumulator = 0.2f;

			// If not paused,
			if (pause == false || nextFrame == true)
			{
				while (accumulator > deltaTime)
				{
					// update all objects
					foreach (NewRigidBody rigidBody in rigidBodies)
						rigidBody.Integrate (currentTime, deltaTime);

					accumulator -= deltaTime;

					// Store the state of the objects
                    StoreState();

					// Next frame complete
                    if (nextFrame)
                    {
                        nextFrame = false;
                        break;
                    }
                }
            }

			// Undo
			if(pause == true && prevFrame == true)
			{
				Undo ();
				prevFrame = false;
			}
				
			yield return null;
		}

	}

	// nextframe
	public void Next()
	{
		nextFrame = true;
		pause = true;
	}

	// previous frame
	public void Prev()
	{
		prevFrame = true;
		pause = true;
	}

	// Store current state of objects
	private void StoreState()
	{		
		RigidbodyState[] objectStates = new RigidbodyState[rigidBodies.Length];
		for (int i = 0; i < rigidBodies.Length; i++)
		{
			NewRigidBody rigidBody = rigidBodies[i];

			RigidbodyState state = new RigidbodyState();
			state.acceleration = rigidBody.acceleration;
			state.velocity = rigidBody.velocity;
			state.position = rigidBody.position;
			state.angularVelocity = rigidBody.angularVelocity;
			state.orientation = rigidBody.transform.rotation.eulerAngles;

			objectStates[i] = state;
		}
		storage[stepIndex].states = objectStates;

		undoCount++;
		if (undoCount >= STORAGE_SIZE) 
			undoCount = STORAGE_SIZE - 1;

		stepIndex++;
		if (stepIndex >= STORAGE_SIZE) 
			stepIndex = 0;
	}

	// Undo objects to previous state
	private void Undo()
	{
		if (undoCount <= 0) 
			return;

		undoCount--;
		stepIndex--;
		if (stepIndex < 0)
			stepIndex = STORAGE_SIZE - 1;

		for (int i = 0; i < rigidBodies.Length; i++) 
		{
			NewRigidBody rigidBody = rigidBodies [i];

			RigidbodyState state = storage [stepIndex].states [i];
			rigidBody.acceleration = state.acceleration;
			rigidBody.velocity = state.velocity;
			rigidBody.position = state.position;
			rigidBody.angularVelocity = state.angularVelocity;
			rigidBody.orientation = state.orientation;

			rigidBody.transform.rotation = Quaternion.Euler (state.orientation);
			rigidBody.transform.position = rigidBody.position;
		}
	}

	public void PausePlay()
	{
		pause = !pause;

		// Hide initial conditions panel
		if (pause == false && firstPlay == false) 
		{
			_initialConditions.SetActive (false);
			firstPlay = true;
		}

		if (pause == false) 
		{
			for(int i = 0; i < storage.Length; i++)
				storage[i].states = new RigidbodyState[0];
			
			undoCount = 0;
			stepIndex = 0;
		}
	}

	// Reset objects to the initial state
	public void Reset()
	{
		for (int i = 0; i < rigidBodies.Length; i++) 
		{
			NewRigidBody rigidBody = rigidBodies [i];

			RigidbodyState state = _initialStates[i];
			rigidBody.acceleration = state.acceleration;
			rigidBody.velocity = state.velocity;
			rigidBody.position = state.position;
			rigidBody.angularVelocity = state.angularVelocity;
			rigidBody.orientation = state.orientation;

			rigidBody.transform.rotation = Quaternion.Euler (state.orientation);
			rigidBody.transform.position = rigidBody.position;

			rigidBody.transform.GetComponent<MeshRenderer> ().material = rigidBody._originalMaterial;
		}

		firstPlay = false;
		_initialConditions.SetActive (true);
		pause = true;
	}

	private void ReadInput()
	{
		if (Input.GetKeyDown (KeyCode.O)) 
			PausePlay ();

		if (Input.GetKeyDown (KeyCode.P)) 
			Next();

		if (Input.GetKey (KeyCode.I))
			Prev();
	}


}
