using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobType Type;
    public Rigidbody Body;

    private bool _dead;

    void OnCollisionEnter(Collision other)
    {
        //TODO: still having issues with mobs killing more than one
        if (_dead) return; 

        var otherMob = other.collider.GetComponentInParent<Mob>();
        if (otherMob != null && Type != otherMob.Type)
        {
            var mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
            mobControl.DespawnMob(this);

            _dead = true;
        }
    }
}