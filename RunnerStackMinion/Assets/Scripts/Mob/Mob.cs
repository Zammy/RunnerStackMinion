using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobType Type;
    public Rigidbody Body;

    public bool IsDead { get; set; }
    // public int _id = 0;
    // static int idSeed = 0;

    // void Start()
    // {
    //     _id = idSeed++;
    // }

    void OnCollisionEnter(Collision other)
    {
        if (IsDead)
            return;

        var otherMob = other.collider.GetComponentInParent<Mob>();
        if (otherMob == null || otherMob.IsDead)
            return;

        if (Type != otherMob.Type)
        {
            var mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
            mobControl.DespawnMob(this);
            mobControl.DespawnMob(otherMob);
            IsDead = true;
            otherMob.IsDead = true;
        }
    }
}