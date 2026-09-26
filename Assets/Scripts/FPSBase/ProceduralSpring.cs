using UnityEngine;

[System.Serializable]
public class ProceduralSpring
{
    public float stiffness = 150f;
    public float damping = 18f;

    private Vector3 currentValue;
    private Vector3 currentVelocity;
    private Vector3 targetValue;

    public ProceduralSpring(float stiffness, float damping)
    {
        this.stiffness = stiffness;
        this.damping = damping;
    }

    public void AddImpulse(Vector3 impulse)
    {
        currentVelocity += impulse;
    }

    public void SetTarget(Vector3 target)
    {
        targetValue = target;
    }

    public Vector3 Evaluate(float deltaTime)
    {
        float dt = Mathf.Min(deltaTime, 0.05f);
        Vector3 force = (targetValue - currentValue) * stiffness - currentVelocity * damping;
        currentVelocity += force * dt;
        currentValue += currentVelocity * dt;
        return currentValue;
    }

    public void Reset()
    {
        currentValue = Vector3.zero;
        currentVelocity = Vector3.zero;
        targetValue = Vector3.zero;
    }
}
