using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewRigidBody : MonoBehaviour
{
    [SerializeField]
    public bool _isKinematic = false;

    [SerializeField]
	public float mass = 1.0f;
    public float _inverseMass = 0.0f;

    [SerializeField]
    public float _restitution = 0.0f;

    [SerializeField]
	private float accelerationDueToGravity = -9.81f;

	[SerializeField]
	private float staticFriction = 0.5f;

	[SerializeField]
	private float _dynamicFriction = 0.2f;

    // Linear components
    public Vector3 position = Vector3.zero;
	public Vector3 velocity = Vector3.zero; // Change in position
	public Vector3 acceleration = Vector3.zero;

	// Angular components
	public Vector3 orientation = Vector3.zero;

    [SerializeField]
    public Vector3 angularVelocity = Vector3.zero;

    [SerializeField]
	public float inertia = 0.0f; // "inertia" is the difficulty to rotate factor // > inertia is hard to spin
    private float _inverseInertia = 0.0f;

	// Debug Info
	public Material _originalMaterial = null;

	[SerializeField]
	private Material _collidedMaterial = null;


	private float _dragCoefficient = 0.0f;
	private float _frontalArea = 0.0f;

	private Vector3 _initialOrientation = Vector3.zero;

	List<Vector3> torquesToApply = new List<Vector3> ();


    void Start () 
	{
		// Initialise variables
		position = this.transform.position;
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
		_initialOrientation = this.transform.rotation.eulerAngles;
		orientation = _initialOrientation;

        // for objects with infinite mass
        if (mass == 0)
            _inverseMass = 0.0f;
        else
            _inverseMass = 1.0f / mass;

		// Calculate Inertia
		CalculateInertia ();
	
		//_sleepMaterial = Resources.Load<Material> ("sleep");
        _originalMaterial = this.GetComponent<MeshRenderer> ().material;

		// Calculate coefficient
		CalculateCoefficient();
	}

	// reference to order of operation:
	// https://gafferongames.com/post/integration_basics/
	public void Integrate(float time, float deltaTime)
	{
		// Mass infinite so dont update
		if (mass == 0)
			return;

		// Avoid infinity error
		if (Mathf.Abs(position.y) == Mathf.Infinity)
			return;


        Vector3 force = Vector3.zero;

		// Apply drag force
		force += CalculateDragForce() * deltaTime;


		// a = f / m
		acceleration = force / mass;

        // Apply gravity
        if(_isKinematic == false)
	    	acceleration += new Vector3 (0.0f, accelerationDueToGravity, 0.0f); // gravity irrespective of mass


		// angularVelocity += torqueForce * _inverseInertia;
		foreach (Vector3 torque in torquesToApply) 
			angularVelocity += (torque * _inverseInertia);

		torquesToApply.Clear ();


		orientation +=  angularVelocity * deltaTime;
		this.transform.rotation = Quaternion.Euler(orientation);


        // change in velocity = a * dt
        velocity += acceleration * deltaTime;

		// Collision Detection
		if (IsCollision (deltaTime))
		{
			if (_collidedMaterial != null)
				this.GetComponent<MeshRenderer> ().material = _collidedMaterial;
		} 
			
        // change in position = v * dt
        position += velocity * deltaTime;
		this.transform.position = position;

		// decrease angularVelocity over time
		angularVelocity *= 1.0f - deltaTime;
	}


	public void ApplyTorqueForce(Vector3 point, Vector3 direction)
	{
		Vector3 torque = Vector3.Distance(this.transform.position, point) * direction;

		Vector3 newT = -torque;
		newT.x = torque.z;
		newT.z = torque.x;

		torquesToApply.Add (newT);        
	}

	private Vector3 CalculateDragForce()
	{
		// reference: air resistance force http://buildnewgames.com/gamephysics/
		// reference: burakkanber.com/blog/modeling-physics-javascript-gravity-and-drag/
		float fluidDensity = 1.2f;// for air // 1000 for water

		Vector3 dragForce = Vector3.zero;
		dragForce.x = -0.5f * fluidDensity * _dragCoefficient * _frontalArea * velocity.x * velocity.x;
		dragForce.y = -0.5f * fluidDensity * _dragCoefficient * _frontalArea* velocity.y * velocity.y;
		dragForce.z = -0.5f * fluidDensity * _dragCoefficient * _frontalArea* velocity.z * velocity.z;

		return dragForce;
	}

	void CalculateCoefficient()
	{
		if (this.GetComponent<NewSphereCollider> ()) 
		{
			float coeffecient_Sphere = 0.5f;
			_dragCoefficient = coeffecient_Sphere;

			float radius = this.GetComponent<NewSphereCollider> ().radius;
			float frontalArea_Sphere = Mathf.PI * radius * radius;
			_frontalArea = frontalArea_Sphere;

		}
		else if (this.GetComponent<NewBoxCollider> ())
		{
			float coefficientRectangularBox = 2.1f;
			_dragCoefficient = coefficientRectangularBox;

			// for cube
			// Width * Height
			float frontalArea_Cube = this.GetComponent<NewBoxCollider>().extents.x * this.GetComponent<NewBoxCollider>().extents.y;
			_frontalArea = frontalArea_Cube;
		} 
		else 
		{
			Debug.LogWarning ("not found coeff etc.");
		}
	}

	void CalculateInertia()
	{
		if (inertia > -1.0f)
		{
			if (this.GetComponent<NewSphereCollider> ())
			{
				// Solid sphere: I = 2/5 * m * r^2
				inertia = (2.0f / 5.0f) * mass * this.GetComponent<NewSphereCollider> ().radius;
			} 
			else if (this.GetComponent<NewBoxCollider> ())
			{
				// Cuboid: I = 1/12 * m *(height^2 + length^2)
				inertia = (1.0f / 12.0f) * mass * (Mathf.Pow (this.GetComponent<NewBoxCollider> ().extents.y, 2) + Mathf.Pow (this.GetComponent<NewBoxCollider> ().extents.x, 2));
			}
		} 
		else 
			inertia = 0.0f;

		if (inertia == 0)
			_inverseInertia = 0.0f;
		else
			_inverseInertia = 1.0f / inertia;
	}
		
	bool IsCollision(float deltaTime)
	{
		if (this._isKinematic == true)
			return false;
		
		bool wasCollision = false;
        if (this.GetComponent<NewSphereCollider>() != null)
        {
            NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider>();
            foreach (NewBoxCollider box in boxes)
			{
				Collision.CollisionData mani = Collision.instance.BoxToSphereManifold (box, this.GetComponent<NewSphereCollider> ());
							
				if (mani.wasCollision)
                {
					CalculateImpulse(mani, deltaTime);
					wasCollision = true;
                }

            }

            NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider>();
            foreach (NewSphereCollider sphere in spheres)
            {
                if (sphere.gameObject.GetInstanceID() == this.gameObject.GetInstanceID())
                    continue;

				Collision.CollisionData mani = Collision.instance.SphereToSphereManifold (this.GetComponent<NewSphereCollider> (), sphere);
				if (mani.wasCollision)
                {
					CalculateImpulse(mani, deltaTime);
					wasCollision = true;
                }

            }
        }
        else if(this.GetComponent<NewBoxCollider>() != null)
        {
            NewBoxCollider[] boxes = GameObject.FindObjectsOfType<NewBoxCollider>();
            foreach (NewBoxCollider box in boxes)
            {
                if (box.gameObject.GetInstanceID() == this.gameObject.GetInstanceID())
                    continue;

                Vector3 bOrientation = box.GetComponent<NewRigidBody>().orientation;
                Collision.CollisionData mani = new Collision.CollisionData();

				// if axis aligned 
                if ((this.orientation.x == 0 || this.orientation.x == 90 || this.orientation.x == 180 || this.orientation.x == 270)
                    && (this.orientation.y == 0 || this.orientation.y == 90 || this.orientation.y == 180 || this.orientation.y == 270)
                    && (this.orientation.z == 0 || this.orientation.z == 90 || this.orientation.z == 180 || this.orientation.z == 270)
                    
                    &&

                    (bOrientation.x == 0 || bOrientation.x == 90 || bOrientation.x == 180 || bOrientation.x == 270)
                    && (bOrientation.y == 0 || bOrientation.y == 90 || bOrientation.y == 180 || bOrientation.y == 270)
                    && (bOrientation.z == 0 || bOrientation.z == 90 || bOrientation.z == 180 || bOrientation.z == 270))
                {

                    mani = Collision.instance.BoxToBoxManifold(this.GetComponent<NewBoxCollider>(), box);
                }
				// otherwise SAT
                else
                {
                    mani = Collision.instance.BoxToBoxSAT(this.GetComponent<NewBoxCollider>(), box);
                }

				if (mani.wasCollision)
                {
					CalculateImpulse(mani, deltaTime);
					wasCollision = true;
                }
            }

            NewSphereCollider[] spheres = GameObject.FindObjectsOfType<NewSphereCollider>();
            foreach (NewSphereCollider sphere in spheres)
            {
				
				Collision.CollisionData mani = Collision.instance.BoxToSphereManifold (this.GetComponent<NewBoxCollider> (), sphere);

				if (mani.wasCollision)
                {
					CalculateImpulse(mani, deltaTime);
					wasCollision = true;
                }
            }

        }
			
		return wasCollision;
	}
		
	public void CalculateImpulse(Collision.CollisionData manifold, float deltaTime)
	{
		// reference on how to calculate and use impulse force:
		// https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
	
		if(manifold.bodyA == null || manifold.bodyB == null)
		{
			Debug.LogWarning("Missing Rigidbody");
			return;
		}
			
        // Calculate relative velocity
		Vector3 relativeVelocity = manifold.bodyB.velocity - manifold.bodyA.velocity;
	
		// Calculate relative velocity in terms of the normal direction
		Vector3 collisionNormal = manifold.normal;
		float velocityAlongNormal = Vector3.Dot(relativeVelocity, collisionNormal);
	
		// Return if moving away from eachother as will be seperated anyways
		if (velocityAlongNormal > 0.0f)
			return;

		// Calculate restitution
		float restituion = Mathf.Min(manifold.bodyA._restitution, manifold.bodyB._restitution);
	

		// One static, one dynamic
		if ((manifold.bodyA.velocity == Vector3.zero && manifold.bodyB.velocity != Vector3.zero) ||
			(manifold.bodyB.velocity == Vector3.zero && manifold.bodyA.velocity != Vector3.zero)) 
		{
			// Get moving object
			NewRigidBody movingBody;
			if (manifold.bodyA.velocity != Vector3.zero)
				movingBody = manifold.bodyA;
			else
				movingBody = manifold.bodyB;
			
			restituion = movingBody._restitution;
  
			Vector3 newVelocity = movingBody.velocity * restituion;

			if (Mathf.Abs (collisionNormal.x) > Mathf.Abs (collisionNormal.y) && Mathf.Abs (collisionNormal.x) > Mathf.Abs (collisionNormal.z)) 
				newVelocity.x = -newVelocity.x;
			else if (Mathf.Abs (collisionNormal.y) > Mathf.Abs (collisionNormal.x) && Mathf.Abs (collisionNormal.y) > Mathf.Abs (collisionNormal.z)) 
				newVelocity.y = -newVelocity.y;
			else if (Mathf.Abs (collisionNormal.z) > Mathf.Abs (collisionNormal.x) && Mathf.Abs (collisionNormal.z) > Mathf.Abs (collisionNormal.y)) 
				newVelocity.z = -newVelocity.z;
			

            if(movingBody._isKinematic == false)
			    movingBody.velocity = newVelocity;


            // Friction
			{
				float j = -(1.0f + movingBody._restitution) * velocityAlongNormal;
				j /= manifold.bodyA._inverseMass + manifold.bodyB._inverseMass;
				ApplyFriction (manifold, j, deltaTime);
			}

			if(manifold.contacts != null)
			{
				if (manifold.contacts.Count > 0)
				{
					float impulseRot = 0.0f;

					 impulseRot += Vector3.Cross ((manifold.contacts [0] - movingBody.transform.position), collisionNormal).sqrMagnitude * movingBody._inverseInertia;
			
					float jRot = -(1.0f + movingBody._restitution) * velocityAlongNormal;
					jRot /= manifold.bodyA._inverseMass + manifold.bodyB._inverseMass + impulseRot;

					movingBody.angularVelocity -= manifold.bodyA._inverseInertia * Vector3.Cross (manifold.contacts [0], jRot * -collisionNormal) * deltaTime;
				}
			}

        }
		// Two Dyanmic objects
		else 
		{
			// Calculate impulse scalar
			float j = -(1.0f + restituion) * velocityAlongNormal;

            if (manifold.contacts == null || manifold.contacts.Count == 0)
                j /= manifold.bodyA._inverseMass + manifold.bodyB._inverseMass;
            else
            {
                // with rotation
				float rA = Vector3.Cross ((manifold.contacts [0] - manifold.bodyAobj.transform.position), -collisionNormal).sqrMagnitude;
				float rB = Vector3.Cross ((manifold.contacts [0] - manifold.bodyBobj.transform.position), -collisionNormal).sqrMagnitude;

				float impulseRotA =  rA * manifold.bodyA._inverseInertia;
				float impulseRotB =  rB * manifold.bodyB._inverseInertia;

				j /= manifold.bodyA._inverseMass + manifold.bodyB._inverseMass + impulseRotA + impulseRotB;

				// Apply
				if (manifold.bodyA._isKinematic == false)
					manifold.bodyA.angularVelocity -= manifold.bodyA._inverseInertia * Vector3.Cross (manifold.contacts [0], j * -collisionNormal) * deltaTime;

				if(manifold.bodyB._isKinematic == false)
					manifold.bodyB.angularVelocity += manifold.bodyB._inverseInertia  * Vector3.Cross(manifold.contacts[0], j * -collisionNormal) * deltaTime;
            }
				

			// Apply impulse
			Vector3 impulse = j * collisionNormal;

			if (manifold.bodyA._isKinematic == false) 
				manifold.bodyA.velocity -= manifold.bodyA._inverseMass * impulse ;

			if (manifold.bodyB._isKinematic == false) 
				manifold.bodyB.velocity += manifold.bodyB._inverseMass * impulse;
			
            // Friction 
            ApplyFriction(manifold, j, deltaTime);
        }


		PositionCorrection(manifold.bodyA, manifold.bodyB, manifold.penetration, collisionNormal);
	}

	void PositionCorrection(NewRigidBody a, NewRigidBody b, float penetrationDepth, Vector3 collisionNormal)
	{
		// reference on how to correct position once an impulse is applied
		// https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331

		const float percent = 0.8f;
		const float thresholdToPreventJitter = 0.01f;
		Vector3 correction = Mathf.Max(penetrationDepth - thresholdToPreventJitter, 0.0f) / (a._inverseMass + b._inverseMass) * percent * collisionNormal;

		// Apply
		if (a._isKinematic == false)
			a.position -= a._inverseMass * correction;

		if (b._isKinematic == false)
			b.position += b._inverseMass * correction;
	}

    private void ApplyFriction(Collision.CollisionData manifold, float j, float deltaTime)
    {
		// reference on how to get friction impulse and tangent:
		// https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-friction-scene-and-jump-table--gamedev-7756

        Vector3 relativeVelocity = manifold.bodyB.velocity - manifold.bodyA.velocity;
		Vector3 tangent = Vector3.Normalize(relativeVelocity - Vector3.Dot(relativeVelocity, manifold.normal) * manifold.normal);
  
     
		float jFriction = -Vector3.Dot(relativeVelocity, tangent);

		if (manifold.contacts == null || manifold.contacts.Count == 0)
		{
			jFriction = jFriction / (manifold.bodyA._inverseMass + manifold.bodyB._inverseMass);
		} 
		// with rotation
		else
		{
			float rA = Vector3.Cross ((manifold.contacts [0] - manifold.bodyAobj.transform.position), -manifold.normal).sqrMagnitude;
			float rB = Vector3.Cross ((manifold.contacts [0] - manifold.bodyBobj.transform.position), -manifold.normal).sqrMagnitude;

			float impulseRotA = rA * manifold.bodyA._inverseInertia;
			float impulseRotB = rB * manifold.bodyB._inverseInertia;

			jFriction = jFriction / (manifold.bodyA._inverseMass + manifold.bodyB._inverseMass + impulseRotA + impulseRotB);

			// Apply
			if (manifold.bodyA._isKinematic == false)
				manifold.bodyA.angularVelocity -= manifold.bodyA._inverseInertia * Vector3.Cross (manifold.contacts [0], jFriction * -manifold.normal) * deltaTime;

			if (manifold.bodyB._isKinematic == false)
				manifold.bodyB.angularVelocity += manifold.bodyB._inverseInertia * Vector3.Cross (manifold.contacts [0], jFriction * -manifold.normal) * deltaTime;
		}
   

        float approx = Mathf.Sqrt((manifold.bodyA.staticFriction * manifold.bodyA.staticFriction)
                   + (manifold.bodyB.staticFriction * manifold.bodyB.staticFriction));

  
        Vector3 frictionImpulse;
		if (Mathf.Abs(jFriction) < j * approx)
			frictionImpulse = jFriction * tangent;
        else
        {
            float dynamicFriction = Mathf.Sqrt((manifold.bodyA._dynamicFriction * manifold.bodyA._dynamicFriction)
                + (manifold.bodyB._dynamicFriction * manifold.bodyB._dynamicFriction));
            frictionImpulse = -j * tangent * dynamicFriction;
        }
			
        // Apply friction impulse
        if(manifold.bodyA._isKinematic == false)
			manifold.bodyA.velocity -= manifold.bodyA._inverseMass * frictionImpulse;

        if (manifold.bodyB._isKinematic == false)
            manifold.bodyB.velocity += manifold.bodyB._inverseMass * frictionImpulse;



    }

}
