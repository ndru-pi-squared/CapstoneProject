//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

interface IShootable //was IShootable<T>?
{
    void Shoot(); //was void Shoot(T obj);
    bool IsReadyToShoot();
    int GetTypeOfGun();//was TypeOfGun();
    void PlayGunShotSound();

}