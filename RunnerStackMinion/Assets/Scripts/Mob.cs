using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobType Type;
    public Rigidbody Body;

    void OnCollisionEnter(Collision other)
    {
        var otherMob = other.collider.GetComponentInParent<Mob>();
        if (otherMob != null && Type != otherMob.Type)
        {
            var mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
            mobControl.DespawnMob(this);
        }
    }
}