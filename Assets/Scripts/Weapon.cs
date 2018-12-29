using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour {

    public bool isMelee = false;

    

    float damage;

    public virtual void Attack()
    {
        // maybe this should be abstract instead of virtual?
        // if it's abstract, each child needs an implementation
        // if it's virtual, they don't NEED it. This may be the case if meleeWeapon and RangedWeapon want to call it differently?
    }

    public virtual void TryReload()
    {

    }
}
