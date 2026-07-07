using HAVIGAME;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarDatabase", menuName = "Database/AvatarDatabase")]
public class AvatarDatabase : Database<AvatarDatabase, string, Avatar> {
#if UNITY_EDITOR
    protected override bool Installable => true;

    protected override void InstallDatabase() {
        base.InstallDatabase();

        foreach (var item in database) {
            item.Reset();
        }
    }
#endif
}
